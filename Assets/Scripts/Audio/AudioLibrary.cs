using System;
using UnityEngine;
using UnityEngine.Audio;

namespace BulletHell.Audio
{
    /// <summary>
    /// Everything the AudioService needs from assets: which <see cref="SfxData"/> answers each <see cref="SfxId"/>, which tracks play in
    /// each <see cref="MusicContext"/>, the mixer and its groups, and the tuning (crossfade length, voice pool size). On GameConfig.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "BulletHell/Audio/Audio Library")]
    public sealed class AudioLibrary : ScriptableObject
    {
        public const string MasterParameter = "MasterVolume";
        public const string MusicParameter = "MusicVolume";
        public const string SfxParameter = "SfxVolume";

        [Serializable]
        public struct SfxEntry
        {
            public SfxId Id;
            public SfxData Data;
        }

        [Serializable]
        public struct MusicEntry
        {
            public MusicContext Context;
            [Tooltip("Tracks of this context. Combat picks by round (round 1 = first, round 2 = second, then around again).")]
            public AudioClip[] Clips;
            [Range(0f, 1f)] public float Volume;
        }

        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup uiGroup;
        [SerializeField] private AudioMixerGroup announcerGroup;

        [Header("Sounds")]
        [SerializeField] private SfxEntry[] sfx = new SfxEntry[0];
        [SerializeField] private MusicEntry[] music = new MusicEntry[0];

        [Header("Tuning")]
        [Tooltip("Seconds the old track fades out while the new one fades in.")]
        [SerializeField, Min(0.05f)] private float crossfadeSeconds = 1.5f;
        [Tooltip("How many sound effects can play at once in total (audio sources are pooled, never created during play).")]
        [SerializeField, Range(4, 64)] private int voicePool = 24;
        [Tooltip("Music volume multiplier while the game is paused.")]
        [SerializeField, Range(0f, 1f)] private float pausedMusicVolume = 0.35f;

        private SfxData[] byId;

        public AudioMixer Mixer => mixer;
        public float CrossfadeSeconds => crossfadeSeconds;
        public int VoicePool => voicePool;
        public float PausedMusicVolume => pausedMusicVolume;
        public SfxEntry[] Sfx => sfx;
        public MusicEntry[] Music => music;
        public AudioMixerGroup MusicGroup => musicGroup;

        public AudioMixerGroup GroupFor(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Ui: return uiGroup;
                case AudioBus.Announcer: return announcerGroup;
                default: return sfxGroup;
            }
        }

        public SfxData Get(SfxId id)
        {
            if (byId == null)
            {
                byId = new SfxData[Enum.GetValues(typeof(SfxId)).Length];
                foreach (SfxEntry entry in sfx)
                    if ((int)entry.Id < byId.Length)
                        byId[(int)entry.Id] = entry.Data;
            }
            return (int)id < byId.Length ? byId[(int)id] : null;
        }

        /// <summary>The track for a context (null when none). <paramref name="variant"/> picks among several (wraps around).</summary>
        public AudioClip GetMusic(MusicContext context, int variant, out float trackVolume)
        {
            trackVolume = 1f;
            foreach (MusicEntry entry in music)
            {
                if (entry.Context != context || entry.Clips == null || entry.Clips.Length == 0)
                    continue;
                trackVolume = entry.Volume > 0f ? entry.Volume : 1f;
                int index = ((variant % entry.Clips.Length) + entry.Clips.Length) % entry.Clips.Length;
                return entry.Clips[index];
            }
            return null;
        }

        private void OnValidate() => byId = null;

#if UNITY_EDITOR
        public void Configure(AudioMixer newMixer, AudioMixerGroup musicG, AudioMixerGroup sfxG, AudioMixerGroup uiG, AudioMixerGroup announcerG,
                              SfxEntry[] sfxEntries, MusicEntry[] musicEntries)
        {
            mixer = newMixer;
            musicGroup = musicG;
            sfxGroup = sfxG;
            uiGroup = uiG;
            announcerGroup = announcerG;
            sfx = sfxEntries;
            music = musicEntries;
            byId = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
