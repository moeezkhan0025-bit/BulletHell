using System.Collections.Generic;
using BulletHell.AI;
using BulletHell.Bosses;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Perf
{
    /// <summary>
    /// A fixed, seeded stress script for comparing builds: from the start of Combat it runs the player on a scripted
    /// figure-8 with all eight arms firing at once (Homing, Ricochet and Pierce equipped as far as the slots allow), keeps a
    /// swarm of ordinary enemies alive up to a target, and from 20 s on holds the Pumpking in phase 2 with its attack cooldowns
    /// cut to a fraction so the heaviest patterns come back to back. The player cannot die. The PerfLogger records the
    /// whole run; at the end everything is put back and (when started from the command line) the application quits.
    /// Development builds and the editor only.
    /// </summary>
    public sealed class StressTest : MonoBehaviour
    {
        private const float BossPhase2Seconds = 20f;
        private const float SpawnInterval = 0.25f;
        private const int SpawnBatch = 4;

        private enum Stage { WaitingForCombat, Running, Finished }

        private PerfHost host;
        private RunManager run;
        private GameplayInputReader input;
        private PlayerHealth player;
        private ArmFireController fire;
        private WaveSpawner spawner;
        private readonly List<EnemyData> swarmTypes = new List<EnemyData>();
        private EnemyData bossType;
        private System.Random random;
        private Stage stage = Stage.Finished;
        private float startTime;
        private float duration;
        private int enemyTarget;
        private float nextSpawn;
        private float nextBossCheck;
        private bool bossForced;
        private bool quitWhenDone;
        private int previousVSync;
        private int previousFrameRate;
        private string label;

        public bool Running => stage != Stage.Finished;

        /// <summary>Sets everything up. False (with a warning) when this is not the Game scene.</summary>
        public bool Begin(PerfHost owner, string runLabel, float seconds, int enemies, string vsyncMode, bool quitAtEnd)
        {
            host = owner;
            spawner = FindFirstObjectByType<WaveSpawner>();
            input = FindFirstObjectByType<GameplayInputReader>();
            player = FindFirstObjectByType<PlayerHealth>();
            fire = FindFirstObjectByType<ArmFireController>();
            var arms = FindFirstObjectByType<ArmSelectionController>();
            if (spawner == null || input == null || player == null || fire == null || arms == null)
            {
                Debug.LogWarning("StressTest: start it from the Game scene.");
                return false;
            }

            run = GameServices.Ensure().Run;
            GameConfig config = GameServices.Ensure().Config;
            CollectEnemies(config);
            foreach (EnemyPool pool in FindObjectsByType<EnemyPool>(FindObjectsSortMode.None))
                if (pool.name != "BossPool")
                    pool.DebugPrewarm(enemies + 8);
            EquipAllArms(run.State, config, arms);

            label = runLabel;
            duration = seconds;
            enemyTarget = enemies;
            quitWhenDone = quitAtEnd;
            random = new System.Random(1234);
            bossForced = false;
            nextSpawn = 0f;
            nextBossCheck = 0f;

            previousVSync = QualitySettings.vSyncCount;
            previousFrameRate = Application.targetFrameRate;
            if (vsyncMode != "default")
            {
                QualitySettings.vSyncCount = vsyncMode == "1" ? 1 : 0;
                Application.targetFrameRate = -1;
            }

            player.DebugGodMode = true;
            fire.DebugFireAll = true;
            input.DebugOverride = true;
            stage = Stage.WaitingForCombat;
            Debug.Log($"StressTest '{label}': {duration:0}s, swarm {enemyTarget}, {swarmTypes.Count} enemy types, boss {(bossType != null ? bossType.DisplayName : "none")}, vSync {QualitySettings.vSyncCount}.");
            return true;
        }

        // The enemy types of the authored rounds: ordinary ones for the swarm, the first boss for the Pumpking.
        private void CollectEnemies(GameConfig config)
        {
            swarmTypes.Clear();
            bossType = null;
            for (int r = 1; r <= config.RoundCount; r++)
            {
                RoundData round = config.GetRound(r);
                if (round == null)
                    continue;
                foreach (WaveData wave in round.Waves)
                {
                    if (wave == null)
                        continue;
                    foreach (SpawnGroup group in wave.Groups)
                    {
                        if (group.Enemy == null)
                            continue;
                        if (group.Enemy.Boss != null)
                        {
                            if (bossType == null)
                                bossType = group.Enemy;
                        }
                        else if (!swarmTypes.Contains(group.Enemy))
                            swarmTypes.Add(group.Enemy);
                    }
                }
            }
        }

        // Eight arms (the registry's arm types, cycled), each with Homing, Ricochet and Pierce as far as its slots go.
        private static void EquipAllArms(RunState state, GameConfig config, ArmSelectionController arms)
        {
            IReadOnlyList<WeaponArmData> armTypes = config.Registry.Arms;
            IReadOnlyList<ArmamentData> catalog = config.Registry.Armaments;
            ArmamentData homing = Find(catalog, "Homing");
            ArmamentData ricochet = Find(catalog, "Ricochet");
            ArmamentData pierce = Find(catalog, "Pierce");
            var wanted = new[] { homing, ricochet, pierce };

            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                if (armTypes.Count == 0)
                    break;
                var instance = new ArmInstance(armTypes[i % armTypes.Count]);
                state.Loadout[i] = instance;
                for (int s = 0; s < instance.SlotCount; s++)
                {
                    ArmamentData armament = wanted[(s + i) % wanted.Length];
                    if (armament == null)
                        continue;
                    state.Armaments.Add(armament);
                    instance.TryEquip(armament, state.Armaments);
                }
            }
            arms.Rebuild();
        }

        private static ArmamentData Find(IReadOnlyList<ArmamentData> catalog, string key)
        {
            foreach (ArmamentData armament in catalog)
                if (armament != null && armament.name.Contains(key))
                    return armament;
            Debug.LogWarning($"StressTest: no '{key}' armament in the registry.");
            return null;
        }

        private void Update()
        {
            if (stage == Stage.WaitingForCombat)
            {
                if (run.Machine.Current == GameState.Combat)
                {
                    startTime = Time.unscaledTime;
                    host.Logger.Start(label, $"stress seconds={duration} swarm={enemyTarget} vsync={QualitySettings.vSyncCount} seed=1234");
                    host.Stats.ResetMax();
                    stage = Stage.Running;
                }
                return;
            }
            if (stage != Stage.Running)
                return;

            float elapsed = Time.unscaledTime - startTime;
            if (elapsed >= duration || run.Machine.Current != GameState.Combat && run.Machine.Current != GameState.Pause)
            {
                Finish();
                return;
            }

            // Scripted figure-8; the mover clamps to unit length.
            input.DebugMove = new Vector2(Mathf.Cos(elapsed * 0.9f), Mathf.Sin(elapsed * 1.35f));

            if (elapsed >= nextSpawn)
            {
                nextSpawn = elapsed + SpawnInterval;
                KeepSwarmAlive();
            }
            if (elapsed >= nextBossCheck)
            {
                nextBossCheck = elapsed + 0.5f;
                DriveBoss(elapsed);
            }
        }

        private void KeepSwarmAlive()
        {
            if (swarmTypes.Count == 0)
                return;
            int ordinary = spawner.EnemiesAlive - (BossEvents.Active != null ? 1 : 0);
            int missing = enemyTarget - ordinary;
            if (missing <= 0)
                return;
            EnemyData type = swarmTypes[random.Next(swarmTypes.Count)];
            spawner.DebugSpawn(type, Mathf.Min(SpawnBatch, missing));
        }

        // The boss comes from round 3's wave (or is spawned here); at 20 s it is pushed into phase 2 and kept there alive.
        private void DriveBoss(float elapsed)
        {
            BossController boss = BossEvents.Active;
            if (boss == null)
            {
                if (elapsed >= 1f && bossType != null && !bossForced)
                {
                    bossForced = true;
                    spawner.DebugSpawn(bossType, 1);
                }
                return;
            }

            if (elapsed >= BossPhase2Seconds)
            {
                BossBehavior.DebugStress = true;
                if (!bossPushed)
                {
                    bossPushed = true;
                    boss.DebugSetHealth01(0.45f);
                }
                else if (boss.Health.Fraction < 0.3f)
                    boss.DebugSetHealth01(0.45f);
            }
            else if (boss.Health.Fraction < 0.6f)
                boss.DebugSetHealth01(1f);   // before 20 s the boss must not drift into phase 2 by itself
        }

        private bool bossPushed;

        private void Finish()
        {
            stage = Stage.Finished;
            string path = host.Logger.Stop();
            Restore();
            Debug.Log($"StressTest '{label}' finished: {path}");
            if (quitWhenDone)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        private void Restore()
        {
            bossPushed = false;
            BossBehavior.DebugStress = false;
            if (player != null)
                player.DebugGodMode = false;
            if (fire != null)
                fire.DebugFireAll = false;
            if (input != null)
            {
                input.DebugOverride = false;
                input.DebugMove = Vector2.zero;
            }
            QualitySettings.vSyncCount = previousVSync;
            Application.targetFrameRate = previousFrameRate;
        }

        private void OnDisable()
        {
            if (stage != Stage.Finished)
            {
                host?.Logger.Stop();
                stage = Stage.Finished;
            }
            Restore();
        }
    }
}
