using BulletHell.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace BulletHell.Enemies
{
    /// <summary>Pool for every enemy. Warns when it has to grow past its prewarmed size.</summary>
    public sealed class EnemyPool : MonoBehaviour
    {
        [SerializeField] private Enemy prefab;
        [SerializeField] private CombatTuning tuning;
        [Tooltip("On: the small pool for the Boss prefab (its own prewarm and max in CombatTuning).")]
        [SerializeField] private bool bossPool;

        private ObjectPool<Enemy> pool;

        public int CountActive => pool.CountActive;
        public int TotalCreated { get; private set; }
        private int Prewarm => bossPool ? tuning.BossPoolPrewarm : tuning.EnemyPoolPrewarm;

        private void Awake()
        {
            pool = new ObjectPool<Enemy>(Create, e => e.gameObject.SetActive(true), e => e.gameObject.SetActive(false),
                                         Destroy, true, Prewarm, bossPool ? tuning.BossPoolMax : tuning.EnemyPoolMax);

            var warm = new Enemy[Prewarm];
            for (int i = 0; i < warm.Length; i++)
                warm[i] = pool.Get();
            for (int i = 0; i < warm.Length; i++)
                pool.Release(warm[i]);
        }

        public Enemy Get() => pool.Get();

        private bool prewarming;

        /// <summary>
        /// Development tools only (the stress test): makes sure at least `total` enemies exist before measuring, so creating
        /// them does not show up as hitches. Silent (no growth warning).
        /// </summary>
        public void DebugPrewarm(int total)
        {
            prewarming = true;
            var warm = new Enemy[Mathf.Min(total, tuning.EnemyPoolMax)];
            for (int i = 0; i < warm.Length; i++)
                warm[i] = pool.Get();
            for (int i = 0; i < warm.Length; i++)
                pool.Release(warm[i]);
            prewarming = false;
        }

        public void Release(Enemy enemy) => pool.Release(enemy);

        private Enemy Create()
        {
            TotalCreated++;
            if (TotalCreated > Prewarm && !prewarming)
                Debug.LogWarning($"{name} grew past its prewarm size ({Prewarm}): {TotalCreated} created. Raise the prewarm or check for leaks.", this);
            return Instantiate(prefab, transform);
        }
    }
}
