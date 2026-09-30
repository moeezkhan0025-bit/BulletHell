using System;
using System.IO;
using System.Reflection;
using BulletHell.Audio;
using BulletHell.Core;
using BulletHell.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// D4 setup: import settings for the audio clips, the AudioMixer (Master > Music, Sfx > Ui, Announcer, with exposed MasterVolume /
    /// MusicVolume / SfxVolume), the SfxData assets with their voice limits, the AudioLibrary on GameConfig, and the fire sound of each
    /// ammo type. Safe to run again: existing assets are updated, not duplicated.
    /// </summary>
    public static class AudioSetup
    {
        private const string Root = "Assets/Audio";
        private const string DataFolder = "Assets/Data/Audio";
        private const string MixerPath = "Assets/Audio/VoxMixer.mixer";
        private const string LibraryPath = "Assets/Data/Audio/AudioLibrary.asset";
        private const string ConfigPath = "Assets/Resources/GameConfig.asset";

        [MenuItem("BulletHell/D4/Build Audio")]
        public static void Build()
        {
            ImportSettings();
            AudioMixer mixer = BuildMixer(out AudioMixerGroup music, out AudioMixerGroup sfx, out AudioMixerGroup ui, out AudioMixerGroup announcer);
            BuildLibrary(mixer, music, sfx, ui, announcer);
            Debug.Log("D4: audio imported, mixer, sound data and library built.");
        }

        // ---------------------------------------------------------------- clip import

        private static void ImportSettings()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                bool music = path.Contains("/Music/");
                var settings = new AudioImporterSampleSettings
                {
                    // Music streams from disk; short effects are decoded once at load so playing one costs nothing.
                    loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad,
                    compressionFormat = AudioCompressionFormat.Vorbis,
                    quality = music ? 0.5f : 0.6f,
                    sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                    preloadAudioData = !music,
                };
                importer.defaultSampleSettings = settings;
                importer.forceToMono = !music;        // effects are mono: the game is 2D and mono halves their memory
                importer.loadInBackground = music;
                importer.SaveAndReimport();
            }
        }

        // ---------------------------------------------------------------- mixer

        private static AudioMixer BuildMixer(out AudioMixerGroup music, out AudioMixerGroup sfx, out AudioMixerGroup ui, out AudioMixerGroup announcer)
        {
            AudioMixer existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(MixerPath);   // rebuilt from scratch so the groups and exposed parameters are exactly these

            Assembly editor = typeof(Editor).Assembly;
            Type controllerType = editor.GetType("UnityEditor.Audio.AudioMixerController");
            Type groupType = editor.GetType("UnityEditor.Audio.AudioMixerGroupController");
            Type pathType = editor.GetType("UnityEditor.Audio.AudioGroupParameterPath");
            const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            object controller = controllerType.GetMethod("CreateMixerControllerAtPath", All).Invoke(null, new object[] { MixerPath });
            object master = controllerType.GetProperty("masterGroup", All).GetValue(controller);
            MethodInfo createGroup = controllerType.GetMethod("CreateNewGroup", All, null, new[] { typeof(string), typeof(bool) }, null);
            MethodInfo addChild = controllerType.GetMethod("AddChildToParent", All);

            object Group(string name, object parent)
            {
                object group = createGroup.Invoke(controller, new object[] { name, false });
                addChild.Invoke(controller, new[] { group, parent });
                return group;
            }

            object musicGroup = Group("Music", master);
            object sfxGroup = Group("Sfx", master);
            object uiGroup = Group("Ui", sfxGroup);
            object announcerGroup = Group("Announcer", sfxGroup);

            // Expose a group's volume under the name the AudioService sets.
            void Expose(object group, string name)
            {
                object guid = groupType.GetMethod("GetGUIDForVolume", All).Invoke(group, null);
                object path = Activator.CreateInstance(pathType, All, null, new[] { group, guid }, null);
                controllerType.GetMethod("AddExposedParameter", All).Invoke(controller, new[] { path });

                PropertyInfo property = controllerType.GetProperty("exposedParameters", All);
                var array = (Array)property.GetValue(controller);
                for (int i = 0; i < array.Length; i++)
                {
                    object item = array.GetValue(i);
                    FieldInfo guidField = item.GetType().GetField("guid", All);
                    if (!guidField.GetValue(item).Equals(guid))
                        continue;
                    item.GetType().GetField("name", All).SetValue(item, name);
                    array.SetValue(item, i);   // a struct would be a copy
                }
                if (property.CanWrite)
                    property.SetValue(controller, array);
            }

            Expose(master, AudioLibrary.MasterParameter);
            Expose(musicGroup, AudioLibrary.MusicParameter);
            Expose(sfxGroup, AudioLibrary.SfxParameter);

            var mixer = (AudioMixer)controller;
            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(MixerPath, ImportAssetOptions.ForceUpdate);

            mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            music = mixer.FindMatchingGroups("Master/Music")[0];
            sfx = mixer.FindMatchingGroups("Master/Sfx")[0];
            ui = mixer.FindMatchingGroups("Master/Sfx/Ui")[0];
            announcer = mixer.FindMatchingGroups("Master/Sfx/Announcer")[0];
            return mixer;
        }

        // ---------------------------------------------------------------- data

        private static AudioClip Clip(string relative) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/" + relative);

        private static AudioClip[] Clips(params string[] relatives)
        {
            var clips = new AudioClip[relatives.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i] = Clip(relatives[i]);
                if (clips[i] == null)
                    Debug.LogWarning("D4: missing clip " + relatives[i]);
            }
            return clips;
        }

        private static SfxData Sfx(string name, AudioClip[] clips, float volume, float pitchMin, float pitchMax, AudioBus bus, int voices,
                                   float interval, int priority, float duck = 1f, float duckSeconds = 1.5f)
        {
            string path = DataFolder + "/Sfx_" + name + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<SfxData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<SfxData>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.Configure(clips, volume, new Vector2(pitchMin, pitchMax), bus, voices, interval, priority, duck, duckSeconds);
            return data;
        }

        private static void BuildLibrary(AudioMixer mixer, AudioMixerGroup music, AudioMixerGroup sfx, AudioMixerGroup ui, AudioMixerGroup announcer)
        {
            Directory.CreateDirectory(DataFolder);
            AssetDatabase.Refresh();

            // Ammo fire sounds (one per ammo type; ArmFireController plays AmmoTypeData.FireSound)
            SfxData basic = Sfx("ShotBasic", Clips("Sfx/shot_basic_1.ogg", "Sfx/shot_basic_2.ogg", "Sfx/shot_basic_3.ogg"), 0.45f, 0.95f, 1.05f, AudioBus.Sfx, 4, 0.05f, 1);
            SfxData shotgun = Sfx("ShotShotgun", Clips("Sfx/shot_shotgun_1.ogg", "Sfx/shot_shotgun_2.ogg"), 0.6f, 0.9f, 1.0f, AudioBus.Sfx, 3, 0.08f, 1);
            SfxData laser = Sfx("ShotLaser", Clips("Sfx/shot_laser_1.ogg", "Sfx/shot_laser_2.ogg"), 0.35f, 1f, 1f, AudioBus.Sfx, 2, 0.14f, 1);
            SfxData gatling = Sfx("ShotGatling", Clips("Sfx/shot_gatling_1.ogg", "Sfx/shot_gatling_2.ogg", "Sfx/shot_gatling_3.ogg"), 0.3f, 0.95f, 1.1f, AudioBus.Sfx, 4, 0.04f, 1);

            var entries = new[]
            {
                E(SfxId.EnemyHit, Sfx("EnemyHit", Clips("Sfx/enemy_hit_1.ogg", "Sfx/enemy_hit_2.ogg", "Sfx/enemy_hit_3.ogg"), 0.5f, 0.9f, 1.15f, AudioBus.Sfx, 3, 0.05f, 0)),
                E(SfxId.EnemyDeath, Sfx("EnemyDeath", Clips("Sfx/enemy_death_1.ogg", "Sfx/enemy_death_2.ogg"), 0.7f, 0.9f, 1.1f, AudioBus.Sfx, 3, 0.05f, 1)),
                E(SfxId.PlayerHit, Sfx("PlayerHit", Clips("Sfx/player_hit_1.ogg", "Sfx/player_hit_2.ogg"), 0.9f, 0.95f, 1.0f, AudioBus.Sfx, 2, 0.2f, 3)),
                E(SfxId.Jump, Sfx("Jump", Clips("Sfx/jump.ogg"), 0.6f, 1f, 1f, AudioBus.Sfx, 1, 0.1f, 2)),
                E(SfxId.Land, Sfx("Land", Clips("Sfx/land.ogg"), 0.6f, 0.95f, 1.05f, AudioBus.Sfx, 1, 0.1f, 2)),
                E(SfxId.PickupAmmo, Sfx("PickupAmmo", Clips("Sfx/pickup_ammo.ogg"), 0.8f, 1f, 1f, AudioBus.Sfx, 2, 0.1f, 2)),
                E(SfxId.PickupCoin, Sfx("PickupCoin", Clips("Sfx/pickup_coin_1.ogg", "Sfx/pickup_coin_2.ogg"), 0.5f, 0.95f, 1.25f, AudioBus.Sfx, 4, 0.04f, 0)),
                E(SfxId.UiFocus, Sfx("UiFocus", Clips("Sfx/ui_focus_1.ogg", "Sfx/ui_focus_2.ogg"), 0.35f, 1f, 1f, AudioBus.Ui, 2, 0.03f, 1)),
                E(SfxId.UiConfirm, Sfx("UiConfirm", Clips("Sfx/ui_confirm.ogg"), 0.6f, 1f, 1f, AudioBus.Ui, 2, 0.05f, 2)),
                E(SfxId.UiBack, Sfx("UiBack", Clips("Sfx/ui_back.ogg"), 0.6f, 1f, 1f, AudioBus.Ui, 2, 0.05f, 2)),
                E(SfxId.UiBuy, Sfx("UiBuy", Clips("Sfx/ui_buy.ogg"), 0.7f, 1f, 1f, AudioBus.Ui, 2, 0.1f, 2)),
                E(SfxId.UiEquip, Sfx("UiEquip", Clips("Sfx/ui_equip.ogg"), 0.7f, 1f, 1f, AudioBus.Ui, 2, 0.1f, 2)),
                E(SfxId.UiError, Sfx("UiError", Clips("Sfx/ui_error.ogg"), 0.6f, 1f, 1f, AudioBus.Ui, 2, 0.1f, 2)),
                E(SfxId.StingerRound, Sfx("StingerRound", Clips("Stingers/stinger_round.ogg"), 0.8f, 1f, 1f, AudioBus.Announcer, 1, 0f, 3, 0.35f, 2.2f)),
                E(SfxId.StingerBoss, Sfx("StingerBoss", Clips("Stingers/stinger_boss.ogg"), 0.85f, 1f, 1f, AudioBus.Announcer, 1, 0f, 3, 0.3f, 3.2f)),
                E(SfxId.StingerClear, Sfx("StingerClear", Clips("Stingers/stinger_clear.ogg"), 0.8f, 1f, 1f, AudioBus.Announcer, 1, 0f, 3, 0.35f, 2.5f)),
                E(SfxId.StingerGameOver, Sfx("StingerGameOver", Clips("Stingers/stinger_gameover.ogg"), 0.8f, 1f, 1f, AudioBus.Announcer, 1, 0f, 3, 0.3f, 3.5f)),
                E(SfxId.Countdown, Sfx("Countdown", Clips("Sfx/countdown.ogg"), 0.6f, 1f, 1f, AudioBus.Announcer, 1, 0.2f, 2)),
                E(SfxId.CountdownGo, Sfx("CountdownGo", Clips("Sfx/countdown_go.ogg"), 0.7f, 1f, 1f, AudioBus.Announcer, 1, 0.2f, 2)),
            };

            var tracks = new[]
            {
                M(MusicContext.Menu, 0.6f, "Music/music_menu.wav"),
                M(MusicContext.Combat, 0.55f, "Music/music_combat_1.ogg", "Music/music_combat_2.ogg"),
                M(MusicContext.Boss, 0.6f, "Music/music_boss.ogg"),
                M(MusicContext.Shop, 0.5f, "Music/music_shop.ogg"),
            };

            var library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            library.Configure(mixer, music, sfx, ui, announcer, entries, tracks);

            // GameConfig gets the library; each ammo type its fire sound.
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var configObject = new SerializedObject(config);
            configObject.FindProperty("audioLibrary").objectReferenceValue = library;
            configObject.ApplyModifiedPropertiesWithoutUndo();

            SetAmmoSound("Ammo_Basic", basic);
            SetAmmoSound("Ammo_Shotgun", shotgun);
            SetAmmoSound("Ammo_Laser", laser);
            SetAmmoSound("Ammo_Gatling", gatling);
            SetAmmoSound("Ammo_TestSpare", basic);

            AssetDatabase.SaveAssets();
        }

        private static AudioLibrary.SfxEntry E(SfxId id, SfxData data) => new AudioLibrary.SfxEntry { Id = id, Data = data };

        private static AudioLibrary.MusicEntry M(MusicContext context, float volume, params string[] clips) =>
            new AudioLibrary.MusicEntry { Context = context, Volume = volume, Clips = Clips(clips) };

        private static void SetAmmoSound(string assetName, SfxData data)
        {
            var ammo = AssetDatabase.LoadAssetAtPath<AmmoTypeData>("Assets/Data/Ammo/" + assetName + ".asset");
            if (ammo == null)
                return;
            var so = new SerializedObject(ammo);
            so.FindProperty("fireSound").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
