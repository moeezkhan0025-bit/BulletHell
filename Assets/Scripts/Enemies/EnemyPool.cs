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

        private ObjectPool<Enemy> pool;

        public int CountActive => pool.CountActive;
        public int TotalCreated { get; private set; }

        private void Awake()
        {
            pool = new ObjectPool<Enemy>(Create, e => e.gameObject.SetActive(true), e => e.gameObject.SetActive(false),
                                         Destroy, true, tuning.EnemyPoolPrewarm, tuning.EnemyPoolMax);

            var warm = new Enemy[tuning.EnemyPoolPrewarm];
            for (int i = 0; i < warm.Length; i++)
                warm[i] = pool.Get();
            for (int i = 0; i < warm.Length; i++)
                pool.Release(warm[i]);
        }

        public Enemy Get() => pool.Get();

        public void Release(Enemy enemy) => pool.Release(enemy);

        private Enemy Create()
        {
            TotalCreated++;
            if (TotalCreated > tuning.EnemyPoolPrewarm)
                Debug.LogWarning($"EnemyPool grew past its prewarm size ({tuning.EnemyPoolPrewarm}): {TotalCreated} created. Raise the prewarm or check for leaks.", this);
            return Instantiate(prefab, transform);
        }
    }
}
