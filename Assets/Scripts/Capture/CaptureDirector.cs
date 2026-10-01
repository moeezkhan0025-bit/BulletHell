using System.Collections;
using System.Collections.Generic;
using System.IO;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Projectiles;
using BulletHell.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BulletHell.Capture
{
    /// <summary>
    /// R1 portfolio capture kit - runs a capture scenario (<see cref="CaptureScenarioData"/>) through the normal game services and
    /// owns the F12 screenshot. One persistent object in the Editor and development builds (created by a runtime-init hook, like
    /// the PerfHost); release builds never create it.
    ///
    /// Starting a scenario (Capture menu, or <see cref="StartScenario"/>):
    ///   1. the previous run, if any, is abandoned; a NEW run starts (RunManager.StartNewRun) and is flagged as a debug run;
    ///   2. the scenario's round, loadout (fresh ArmInstances with their armaments, no asset is modified) and ammo are put into its RunState;
    ///   3. the onboarding is skipped for this session only (ProfileService.Data.tutorialDone is set in memory, never saved, and put back);
    ///   4. the Game scene is (re)loaded, so the player, arms and HUD are built from that run state: Boot flow, same as Continue;
    ///   5. when the scene is up: Clean capture hides the debug overlays, Invincible turns on the player's debug god mode, the autopilot is bound;
    ///   6. when the round's combat begins: the autopilot starts, extra enemies are topped up for ExtraSpawnSeconds (WaveSpawner.DebugSpawn),
    ///      and CaptureEvents.CombatBegan fires; after CaptureSeconds of combat CaptureEvents.WindowElapsed fires (auto-record stops there).
    /// The scenario ends when the round is over (results, game over), the Game scene unloads, or another scenario starts.
    ///
    /// Keys: F12 screenshot (this class, via the Editor service when in the Editor); F11 recording is handled by the Editor.
    /// </summary>
    public sealed class CaptureDirector : MonoBehaviour
    {
        public const int ScreenshotWidth = 1920;
        public const int ScreenshotHeight = 1080;

#if UNITY_EDITOR
        /// <summary>SessionState key the Editor menu sets (asset path) when it starts Play mode for a scenario.</summary>
        public const string PendingScenarioKey = "BulletHell.Capture.PendingScenario";
#endif

        private enum Stage { Idle, Loading, Armed, Running }

        private static CaptureDirector instance;

        private readonly CaptureCleaner cleaner = new CaptureCleaner();
        private readonly List<EnemyPool> prewarmed = new List<EnemyPool>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private CaptureAutopilot autopilot;
#endif
        private CaptureScenarioData scenario;
        private Stage stage = Stage.Idle;
        private RunManager run;
        private WaveSpawner spawner;
        private PlayerHealth player;
        private ProjectilePool pool;
        private float combatTime;
        private float nextTopUp;
        private bool windowRaised;
        private bool tutorialOverridden;
        private bool tutorialBefore;

        public static CaptureDirector Instance => instance;

        /// <summary>The scenario being run, or null.</summary>
        public CaptureScenarioData Scenario => stage == Stage.Idle ? null : scenario;

        /// <summary>The file name prefix for the next capture: the running scenario's, else "gameplay".</summary>
        public static string CurrentPrefix =>
            instance != null && instance.stage != Stage.Idle && instance.scenario != null ? instance.scenario.FilePrefix : CaptureNaming.DefaultPrefix;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Debug.isDebugBuild)
                return;
            Ensure();
#if UNITY_EDITOR
            // The Capture menu started Play mode for a scenario: pick it up now that the first scene is loaded.
            string path = UnityEditor.SessionState.GetString(PendingScenarioKey, "");
            if (!string.IsNullOrEmpty(path))
            {
                UnityEditor.SessionState.EraseString(PendingScenarioKey);
                var pending = UnityEditor.AssetDatabase.LoadAssetAtPath<CaptureScenarioData>(path);
                if (pending != null)
                    instance.Begin(pending);
                else
                    Debug.LogWarning($"Capture: pending scenario '{path}' could not be loaded.");
            }
#endif
        }

        private static CaptureDirector Ensure()
        {
            if (instance != null)
                return instance;
            var go = new GameObject("CaptureTools");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<CaptureDirector>();
            return instance;
        }

        /// <summary>Starts a scenario in the running game (Play mode). Development builds and the editor only.</summary>
        public static bool StartScenario(CaptureScenarioData data)
        {
            if (!Debug.isDebugBuild || data == null || !Application.isPlaying)
                return false;
            Ensure().Begin(data);
            return true;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
#if !UNITY_EDITOR
            CaptureHotkeys.ScreenshotRequested += OnScreenshotRequested;   // in the Editor the Recorder service sizes the Game view first
#endif
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
#if !UNITY_EDITOR
            CaptureHotkeys.ScreenshotRequested -= OnScreenshotRequested;
#endif
        }

        private void OnDestroy()
        {
            StopScenario();
            if (instance == this)
                instance = null;
        }

        // ------------------------------------------------------------------ scenario start

        private void Begin(CaptureScenarioData data)
        {
            StopScenario();
            scenario = data;
            List<string> problems = data.Validate();
            if (problems.Count > 0)
                Debug.LogWarning($"Capture scenario '{data.name}' has problems: {string.Join("; ", problems)}");

            GameServices services = GameServices.Ensure();
            run = services.Run;

            // Onboarding off for this session only (the profile file is not written; the old value is put back when the scenario ends).
            tutorialBefore = services.Profile.Data.tutorialDone;
            services.Profile.Data.tutorialDone = true;
            tutorialOverridden = true;

            if (run.HasRun)
                run.AbandonRun();
            run.StartNewRun();
            run.MarkDebug();   // telemetry leaves capture runs out of the averages
            ApplyToRun(data, run.State);

            stage = Stage.Loading;
            combatTime = 0f;
            windowRaised = false;
            CaptureEvents.RaiseStarted(data);
            Debug.Log($"Capture: scenario '{data.DisplayName}' (round {data.Round}) - reloading the Game scene for a fresh run.");
            services.Scenes.Load(SceneLoader.Game);
        }

        /// <summary>Puts a scenario's round, loadout and ammo into a run state. Fresh ArmInstances; no asset is touched.</summary>
        public static void ApplyToRun(CaptureScenarioData data, RunState state)
        {
            state.Round = Mathf.Max(1, data.Round);

            if (data.CustomLoadout)
            {
                for (int i = 0; i < state.Loadout.Length; i++)
                    state.Loadout[i] = null;
                for (int i = 0; i < data.Arms.Count; i++)
                {
                    CaptureArmSetup setup = data.Arms[i];
                    if (setup.arm == null || setup.slot < 0 || setup.slot >= state.Loadout.Length)
                        continue;
                    var instance = new ArmInstance(setup.arm);
                    if (setup.armaments != null)
                        foreach (ArmamentData armament in setup.armaments)
                            instance.TryAdd(armament);
                    state.Loadout[setup.slot] = instance;
                }
            }

            if (data.CustomAmmo)
            {
                for (int i = 0; i < AmmoSlotSet.Count; i++)
                    state.Ammo.Replace(i, i < data.Ammo.Count ? data.Ammo[i] : null);
                if (state.Ammo.Active == null)
                    for (int i = 0; i < AmmoSlotSet.Count; i++)
                        if (state.Ammo.TrySelect(i))
                            break;
            }
        }

        // ------------------------------------------------------------------ scene hooks

        private void OnSceneLoaded(Scene loaded, LoadSceneMode mode)
        {
            if (stage != Stage.Loading || loaded.name != SceneLoader.Game)
                return;
            if (!Bind())
            {
                Debug.LogWarning("Capture: the Game scene is missing objects the scenario needs. Scenario cancelled.");
                StopScenario();
                return;
            }
            stage = Stage.Armed;
        }

        private void OnSceneUnloaded(Scene unloaded)
        {
            // The old Game scene unloads while a new scenario reloads it (Stage.Loading): that is not an end.
            if ((stage == Stage.Armed || stage == Stage.Running) && unloaded.name == SceneLoader.Game)
                StopScenario();
        }

        private bool Bind()
        {
            spawner = FindFirstObjectByType<WaveSpawner>();
            player = FindFirstObjectByType<PlayerHealth>();
            pool = FindFirstObjectByType<ProjectilePool>();
            if (spawner == null || player == null || pool == null)
                return false;

            // Enough pooled enemies for the top-ups, so the pool never grows (and warns) mid-capture.
            int extras = 16;
            foreach (CaptureExtraSpawn extra in scenario.ExtraSpawns)
                extras += extra.keepAlive;
            prewarmed.Clear();
            foreach (EnemyPool enemyPool in FindObjectsByType<EnemyPool>(FindObjectsSortMode.None))
                if (enemyPool.name != "BossPool")
                {
                    enemyPool.DebugPrewarm(extras);
                    prewarmed.Add(enemyPool);
                }

            if (CaptureFlags.Invincible)
                player.DebugImmune = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (scenario.Autopilot)
            {
                if (autopilot == null && !TryGetComponent(out autopilot))
                    autopilot = gameObject.AddComponent<CaptureAutopilot>();
                bool ok = autopilot.Bind(scenario.Tuning,
                                         FindFirstObjectByType<GameplayInputReader>(), player,
                                         FindFirstObjectByType<ArmSelectionController>(), FindFirstObjectByType<ArmFireController>(),
                                         FindFirstObjectByType<AmmoSlots>(), FindFirstObjectByType<JumpController>(), pool);
                if (!ok)
                    return false;
            }
#endif

            if (CaptureFlags.CleanCapture)
                cleaner.Apply();

            run.RoundStarted += OnRoundStarted;
            run.Machine.StateChanged += OnStateChanged;
            return true;
        }

        private void OnRoundStarted(int round)
        {
            if (stage != Stage.Armed)
                return;
            stage = Stage.Running;
            combatTime = 0f;
            nextTopUp = 0f;
            windowRaised = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (scenario.Autopilot && autopilot != null)
                autopilot.Begin();
#endif
            CaptureEvents.RaiseCombatBegan(scenario);
        }

        // The round is over (cleared, died, left): the scenario ends.
        private void OnStateChanged(GameState from, GameState to)
        {
            if (stage == Stage.Running && (to == GameState.RoundResults || to == GameState.GameOver))
                StopScenario();
        }

        // ------------------------------------------------------------------ running

        private void Update()
        {
            if (stage != Stage.Running || run.Machine.Current != GameState.Combat)
                return;

            combatTime += Time.deltaTime;
            if (combatTime <= scenario.ExtraSpawnSeconds && combatTime >= nextTopUp)
            {
                nextTopUp = combatTime + scenario.TopUpInterval;
                TopUp();
            }

            if (!windowRaised && combatTime >= scenario.CaptureSeconds)
            {
                windowRaised = true;
                CaptureEvents.RaiseWindowElapsed(scenario);
            }
        }

        private void LateUpdate()
        {
            if (stage == Stage.Armed || stage == Stage.Running)
                cleaner.Tick();
        }

        // Keeps each extra enemy type at its target count (spawns through the wave spawner's debug path, so they count as alive).
        private void TopUp()
        {
            IReadOnlyList<Enemy> alive = pool.Enemies;
            if (alive == null)
                return;
            IReadOnlyList<CaptureExtraSpawn> extras = scenario.ExtraSpawns;
            for (int e = 0; e < extras.Count; e++)
            {
                CaptureExtraSpawn extra = extras[e];
                if (extra.enemy == null)
                    continue;
                int count = 0;
                for (int i = 0; i < alive.Count; i++)
                    if (alive[i] != null && alive[i].IsAlive && alive[i].Data == extra.enemy)
                        count++;
                int missing = extra.keepAlive - count;
                if (missing > 0)
                    spawner.DebugSpawn(extra.enemy, Mathf.Min(Mathf.Max(1, extra.batch), missing));
            }
        }

        /// <summary>Ends the scenario: autopilot off, overlays and cursor back, onboarding flag back. Safe to call any time.</summary>
        public void StopScenario()
        {
            if (stage == Stage.Idle)
                return;

            if (run != null)
            {
                run.RoundStarted -= OnRoundStarted;
                run.Machine.StateChanged -= OnStateChanged;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (autopilot != null)
                autopilot.End();
#endif
            cleaner.Restore();
            if (tutorialOverridden && GameServices.HasInstance)
                GameServices.Ensure().Profile.Data.tutorialDone = tutorialBefore;
            tutorialOverridden = false;

            CaptureScenarioData ended = scenario;
            stage = Stage.Idle;
            if (ended != null)
                CaptureEvents.RaiseEnded(ended);
        }

        // ------------------------------------------------------------------ screenshot

        private void OnScreenshotRequested() => TakeScreenshot();

        /// <summary>
        /// Saves the Game view as a 1920x1080 PNG in Captures/Gameplay named after the running scenario (or "gameplay").
        /// Taken at the end of the frame, so the HUD is in it; a Game view of another size is resampled. Returns immediately.
        /// </summary>
        public static void TakeScreenshot()
        {
            if (Debug.isDebugBuild && instance != null)
                instance.StartCoroutine(instance.ScreenshotRoutine(CurrentPrefix));
        }

        private IEnumerator ScreenshotRoutine(string prefix)
        {
            yield return new WaitForEndOfFrame();
            Texture2D source = ScreenCapture.CaptureScreenshotAsTexture();
            if (source == null)
            {
                Debug.LogWarning("Capture: the screenshot came back empty.");
                yield break;
            }

            Texture2D result = source;
            if (source.width != ScreenshotWidth || source.height != ScreenshotHeight)
            {
                Debug.LogWarning($"Capture: the Game view is {source.width}x{source.height}, resampling the screenshot to {ScreenshotWidth}x{ScreenshotHeight}.");
                RenderTexture target = RenderTexture.GetTemporary(ScreenshotWidth, ScreenshotHeight, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, target);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                result = new Texture2D(ScreenshotWidth, ScreenshotHeight, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, ScreenshotWidth, ScreenshotHeight), 0, 0);
                result.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Destroy(source);
            }

            string path;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Annotated shots (FINAL ART / PLACEHOLDER labels on) go to Captures/State.
            path = PlaceholderAnnotation.Enabled
                ? CaptureNaming.NewPath(prefix + "-annotated", "png", CaptureNaming.StateFolder)
                : CaptureNaming.NewPath(prefix, "png");
#else
            path = CaptureNaming.NewPath(prefix, "png");
#endif
            File.WriteAllBytes(path, result.EncodeToPNG());
            Destroy(result);
            Debug.Log($"Capture: screenshot saved to {path}");
            CaptureHotkeys.RaiseScreenshotSaved(path);
        }
    }
}
