using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BulletHell.Core;
using BulletHell.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// D1 capture run: in Play mode (Game scene), puts each in-scope screen (HUD, banners, boss bar, Round Results, Pause, Game
    /// Over, Settings) into a stress state (long names, 5-digit numbers) and renders it with <see cref="VoxShots"/> at 1920x1080,
    /// 2560x1440 and a landscape phone size into Captures/&lt;tag&gt;/. Driven frame by frame from EditorApplication.update so it
    /// works while the Editor window is not focused. Start with <c>VoxD1Shots.Start("before")</c> from an eval.
    /// </summary>
    public static class VoxD1Shots
    {
        private static readonly (string name, int w, int h)[] Sizes = { ("1080p", 1920, 1080), ("1440p", 2560, 1440), ("phone", 2340, 1080) };

        private sealed class Case
        {
            public string Name;
            public Action Setup;
            public int Frames = 14;
            public Action BeforeShot;
        }

        private static readonly List<Case> Queue = new List<Case>();
        private static int index;
        private static int wait;
        private static string tag;
        private static string outDir;
        private static bool setupDone;

        public static string Start(string runTag)
        {
            if (!Application.isPlaying)
                return "enter Play mode (Game scene) first";
            tag = runTag;
            outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Captures", tag));
            Directory.CreateDirectory(outDir);
            BuildCases();
            index = 0;
            wait = 6;
            setupDone = false;
            EditorApplication.isPaused = true;
            Time.captureDeltaTime = 1f / 60f;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            return "started " + Queue.Count + " cases -> " + outDir;
        }

        private static void Tick()
        {
            if (!Application.isPlaying)
            {
                EditorApplication.update -= Tick;
                return;
            }
            Time.timeScale = 0f;
            if (wait > 0)
            {
                wait--;
                EditorApplication.Step();
                return;
            }
            if (index >= Queue.Count)
            {
                EditorApplication.update -= Tick;
                Time.captureDeltaTime = 0f;
                Time.timeScale = 1f;
                Debug.Log("VoxD1Shots: done (" + tag + ")");
                return;
            }
            Case c = Queue[index];
            if (!setupDone)
            {
                setupDone = true;
                try { c.Setup?.Invoke(); }
                catch (Exception e) { Debug.LogError("VoxD1Shots setup " + c.Name + ": " + e); }
                wait = c.Frames;
                return;
            }
            foreach (var size in Sizes)
            {
                c.BeforeShot?.Invoke();
                string r = VoxShots.Capture(Path.Combine(outDir, c.Name + "_" + size.name + ".png"), size.w, size.h);
                if (!r.StartsWith("ok"))
                    Debug.LogError("VoxD1Shots " + c.Name + ": " + r);
            }
            index++;
            setupDone = false;
            wait = 1;
        }

        // ------------------------------------------------------------------ cases

        private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

        private static void SetField(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            f.SetValue(target, value);
        }

        private static T GetField<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(target);

        private static void HideFlow()
        {
            foreach (var panel in UnityEngine.Object.FindObjectsByType<FlowPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                panel.Hide();
            var settings = Find<SettingsScreen>();
            if (settings != null && settings.IsOpen)
                settings.Close();
            var introLabel = GetField<TMP_Text>(Find<RoundIntroBanner>(), "label");
            introLabel.gameObject.SetActive(false);
            var boss = Find<BossHealthBar>();
            if (boss != null)
            {
                SetField(boss, "targetAlpha", 0f);
                GetField<CanvasGroup>(boss, "group").alpha = 0f;
            }
        }

        private static FlowPanel FlowByField(string field) => GetField<FlowPanel>(Find<GameFlowUI>(), field);

        private static void BuildCases()
        {
            Queue.Clear();
            var services = GameServices.Ensure();
            RunManager run = services.Run;

            Queue.Add(new Case
            {
                Name = "hud_normal",
                Setup = () =>
                {
                    HideFlow();
                    run.State.Currency = 500;
                },
                BeforeShot = () =>
                {
                    Find<HudHeatBar>().Refresh(0.45f, false, false, Color.white);
                }
            });
            Queue.Add(new Case
            {
                Name = "hud_stress",
                Setup = () =>
                {
                    HideFlow();
                    run.State.Round = 12;
                    run.State.Currency = 99999;
                    run.AddEarnings(12345);
                    var hud = Find<RunHud>();
                    SetField(hud, "shownRound", -1);
                },
                BeforeShot = () =>
                {
                    Find<HudHeatBar>().Refresh(0.97f, true, false, Color.white);
                }
            });
            Queue.Add(new Case
            {
                Name = "hud_holdring",
                Setup = () => HideFlow(),
                BeforeShot = () =>
                {
                    var slot = GetField<HudAmmoSlot[]>(Find<CombatHud>(), "slots")[0];
                    slot.Refresh(GetField<BulletHell.Weapons.AmmoTypeData>(slot, "shownAmmo"), true, true, 0.6f);
                    Find<HudHeatBar>().Refresh(0.45f, false, false, Color.white);
                }
            });
            Queue.Add(new Case
            {
                Name = "banner_round",
                Setup = () =>
                {
                    HideFlow();
                    var b = Find<RoundIntroBanner>();
                    var label = GetField<TMP_Text>(b, "label");
                    label.text = "ROUND 12\n<size=56>Get ready, gladiator!</size>";
                    label.gameObject.SetActive(true);
                    label.transform.localScale = Vector3.one;
                    label.color = Color.white;
                }
            });
            Queue.Add(new Case
            {
                Name = "banner_boss_long",
                Setup = () =>
                {
                    HideFlow();
                    var b = Find<RoundIntroBanner>();
                    var label = GetField<TMP_Text>(b, "label");
                    label.text = "<color=#ff5a3c>BOSS ROUND 3</color>\n<size=56>THE MAGNIFICENT SUPREME PUMPKING OF THE EASTERN COLOSSEUM enters the colosseum!</size>";
                    label.gameObject.SetActive(true);
                    label.transform.localScale = Vector3.one;
                    label.color = Color.white;
                }
            });
            Queue.Add(new Case
            {
                Name = "banner_wave",
                Setup = () =>
                {
                    var rb = GetField<TMP_Text>(Find<RoundIntroBanner>(), "label");
                    rb.gameObject.SetActive(false);
                    var w = Find<WaveBanner>();
                    w.Show("Wave 10/12", "Round 12", 600f);
                    GetField<TMP_Text>(w, "label").transform.localScale = Vector3.one;
                }
            });
            Queue.Add(new Case
            {
                Name = "boss_bar",
                Setup = () =>
                {
                    Find<WaveBanner>().Hide();
                    var bar = Find<BossHealthBar>();
                    GetField<TMP_Text>(bar, "namePlate").text = "PUMPKING";
                    GetField<Image>(bar, "fill").fillAmount = 0.62f;
                    GetField<Image>(bar, "trail").fillAmount = 0.75f;
                    SetField(bar, "targetAlpha", 1f);
                    SetField(bar, "trailSpeed", 0.0001f);
                    GetField<CanvasGroup>(bar, "group").alpha = 1f;
                }
            });
            Queue.Add(new Case
            {
                Name = "boss_bar_long",
                Setup = () =>
                {
                    var bar = Find<BossHealthBar>();
                    GetField<TMP_Text>(bar, "namePlate").text = "THE MAGNIFICENT SUPREME PUMPKING OF THE EASTERN VEGETABLE COLOSSEUM";
                    GetField<Image>(bar, "fill").fillAmount = 0.2f;
                    GetField<Image>(bar, "trail").fillAmount = 0.3f;
                }
            });
            Queue.Add(new Case
            {
                Name = "pause_real",
                Setup = () =>
                {
                    HideFlow();
                    Find<WaveBanner>().Hide();
                    run.BeginCombat();
                    run.SetPaused(true);
                }
            });
            Queue.Add(new Case
            {
                Name = "settings_from_pause",
                Setup = () =>
                {
                    foreach (var panel in UnityEngine.Object.FindObjectsByType<FlowPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        panel.Hide();
                    Find<SettingsScreen>().Open(null);
                }
            });
            Queue.Add(new Case
            {
                Name = "results",
                Setup = () =>
                {
                    HideFlow();
                    Find<WaveBanner>().Hide();
                    FlowByField("roundResults").Show("Round 12 cleared", "Currency collected: +99,999\nTotal currency: 123,456");
                }
            });
            Queue.Add(new Case
            {
                Name = "results_focus_menu",
                Setup = () =>
                {
                    HideFlow();
                    var panel = FlowByField("roundResults");
                    panel.Show("Round 3 cleared", "Currency collected: +120\nTotal currency: 560");
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(GetField<Button>(panel, "menuButton").gameObject);
                }
            });
            Queue.Add(new Case
            {
                Name = "pause",
                Setup = () =>
                {
                    HideFlow();
                    FlowByField("pause").Show("Paused", "Round 12", true, "Resume");
                }
            });
            Queue.Add(new Case
            {
                Name = "gameover",
                Setup = () =>
                {
                    HideFlow();
                    FlowByField("gameOver").Show("GAME OVER", "You reached round 12.\nThe run has ended and its save was deleted.", false);
                }
            });
            Queue.Add(new Case
            {
                Name = "settings_audio",
                Setup = () =>
                {
                    HideFlow();
                    Find<SettingsScreen>().Open(null);
                }
            });
            Queue.Add(new Case
            {
                Name = "settings_controls",
                Setup = () =>
                {
                    typeof(SettingsScreen).GetMethod("ShowTab", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Find<SettingsScreen>(), new object[] { 2 });
                }
            });
        }
    }
}
