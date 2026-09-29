using System.Collections.Generic;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Pickups;
using BulletHell.Player;
using BulletHell.Projectiles;
using BulletHell.UI;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>
    /// Runs a round's combat. At the start of each round it looks up the round's waves and the difficulty for that round
    /// number, then plays the waves in order: enemies appear on their schedule, a wave is cleared when all of its enemies
    /// have appeared and died, and after every wave but the last there is a short breather. Clearing the last wave
    /// collects the remaining coins and ends the round.
    /// </summary>
    public sealed class WaveSpawner : MonoBehaviour
    {
        private enum Phase { Idle, Fighting, Breather }

        [SerializeField] private CombatTuning tuning;
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private CoinField coins;
        [SerializeField] private PlayerHealth player;
        [SerializeField] private WaveBanner banner;
        [SerializeField] private Camera viewCamera;
        [Tooltip("The arena: spawn gates, obstacle-free spawn spots, and its bounds. Optional (falls back to the camera view).")]
        [SerializeField] private ArenaController arena;

        private readonly SpawnScheduler scheduler = new SpawnScheduler();
        private readonly List<SpawnRequest> due = new List<SpawnRequest>();
        private readonly List<Enemy> alive = new List<Enemy>();
        private GameServices services;
        private RunManager run;
        private RoundData round;
        private RoundDifficulty difficulty;
        private Phase phase = Phase.Idle;
        private int roundNumber;
        private int waveIndex;
        private float breatherLeft;
        private int gateCounter;

        /// <summary>1-based number of the wave being played (0 between rounds).</summary>
        public int WaveNumber => phase == Phase.Idle ? 0 : waveIndex + 1;
        public int WaveCount => round != null ? round.Waves.Length : 0;
        public int EnemiesAlive => alive.Count;
        public bool IsBreather => phase == Phase.Breather;
        public bool IsBossRound => round != null && round.IsBossRound;

        private void Awake()
        {
            services = GameServices.Ensure();
            run = services.Run;
        }

        private void OnEnable()
        {
            run.RoundIntroStarted += OnRoundIntroStarted;
            run.RoundStarted += OnRoundStarted;
        }

        private void OnDisable()
        {
            run.RoundIntroStarted -= OnRoundIntroStarted;
            run.RoundStarted -= OnRoundStarted;
        }

        // The intro clears the field of the previous round; nothing spawns until combat begins.
        private void OnRoundIntroStarted(int number)
        {
            phase = Phase.Idle;
            projectiles.ReleaseAll();
            coins.Clear();
            ReleaseAllEnemies();
        }

        private void OnRoundStarted(int number)
        {
            roundNumber = number;
            gateCounter = 0;

            round = services.Config.GetRound(number);
            difficulty = services.Config.Difficulty != null ? services.Config.Difficulty.Evaluate(number) : new RoundDifficulty(1f, 1f, 1f, 1f);
            waveIndex = -1;

            if (round == null || round.Waves.Length == 0)
            {
                Debug.LogWarning($"No waves for round {number}: it is cleared immediately.", this);
                phase = Phase.Idle;
                run.CombatCleared();
                return;
            }
            StartNextWave();
        }

        private void StartNextWave()
        {
            waveIndex++;
            scheduler.Begin(round.Waves[waveIndex], difficulty.CountMultiplier);
            phase = Phase.Fighting;

            string sub = waveIndex == 0 ? (round.IsBossRound ? "BOSS ROUND" : $"Round {roundNumber}") : null;
            banner.Show($"Wave {waveIndex + 1}/{round.Waves.Length}", sub, tuning.BannerSeconds);
        }

        private void Update()
        {
            if (phase == Phase.Idle)
                return;

            float dt = Time.deltaTime;
            if (phase == Phase.Breather)
            {
                breatherLeft -= dt;
                if (breatherLeft <= 0f)
                    StartNextWave();
                return;
            }

            due.Clear();
            scheduler.Tick(dt, due);
            for (int i = 0; i < due.Count; i++)
                Spawn(due[i]);

            if (scheduler.Finished && alive.Count == 0)
                OnWaveCleared();
        }

        private void OnWaveCleared()
        {
            if (waveIndex >= round.Waves.Length - 1)
            {
                phase = Phase.Idle;
                banner.Hide();
                coins.CollectAll();
                run.CombatCleared();
                return;
            }

            phase = Phase.Breather;
            breatherLeft = round.WaveBreatherSeconds;
        }

        private void Spawn(in SpawnRequest request)
        {
            bool inArena = arena != null && arena.IsBuilt;
            Vector2 position;
            if (request.Pattern == SpawnPattern.Gates && inArena && arena.Gates.Count > 0)
                position = SpawnPlacement.GatePosition(gateCounter++, arena.Gates, tuning.GateJitter);
            else
                position = SpawnPlacement.Position(request.Pattern, request.Index, request.Count, SpawnRect(), player.Position,
                                                   tuning.MinSpawnDistanceFromPlayer, tuning.RingRadius, tuning.RowWidthFraction);
            if (inArena)
                position = arena.Grid.NearestFree(position, request.Enemy.Size * 0.5f);   // never inside an obstacle or wall

            Enemy enemy = enemyPool.Get();
            enemy.Initialize(request.Enemy, position, difficulty, projectiles, player, inArena ? arena : null);
            enemy.Defeated += OnEnemyDefeated;
            alive.Add(enemy);
        }

        private void OnEnemyDefeated(Enemy enemy)
        {
            enemy.Defeated -= OnEnemyDefeated;
            alive.Remove(enemy);
            coins.Drop(enemy.Position, enemy.Data.CoinValue);
            enemyPool.Release(enemy);
        }

        private void ReleaseAllEnemies()
        {
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                alive[i].Defeated -= OnEnemyDefeated;
                enemyPool.Release(alive[i]);
            }
            alive.Clear();
        }

        /// <summary>The rectangle spawn patterns place enemies in: the arena's bounds (or, without one, the camera view), inset from the edges.</summary>
        private Rect SpawnRect()
        {
            if (arena != null && arena.IsBuilt)
            {
                Rect bounds = arena.Bounds;
                float inset = tuning.SpawnEdgeMargin * 0.5f;
                return new Rect(bounds.xMin + inset, bounds.yMin + inset, bounds.width - 2f * inset, bounds.height - 2f * inset);
            }

            float halfHeight = viewCamera.orthographicSize - tuning.SpawnEdgeMargin;
            float halfWidth = viewCamera.orthographicSize * viewCamera.aspect - tuning.SpawnEdgeMargin;
            Vector2 center = viewCamera.transform.position;
            return new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }
    }
}
