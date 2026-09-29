using System.Collections.Generic;
using BulletHell.Arena;
using BulletHell.Player;
using UnityEngine;
using UnityEngine.Pool;

namespace BulletHell.Projectiles
{
    /// <summary>
    /// Pool for every projectile (player and, later, enemy). Also owns the on-screen bounds projectiles despawn at
    /// and the hit counter. Warns when it has to grow past the prewarmed size.
    /// </summary>
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private Projectile prefab;
        [SerializeField] private Camera viewCamera;
        [Tooltip("The player: enemy bullets hurt only this.")]
        [SerializeField] private PlayerHealth playerTarget;
        [Tooltip("The arena: enemy bullets are stopped by its obstacles and walls. Optional.")]
        [SerializeField] private ArenaController arena;
        [Tooltip("Physics layers a projectile can hit.")]
        [SerializeField] private LayerMask hitMask;
        [SerializeField, Min(1)] private int prewarm = 128;
        [SerializeField, Min(1)] private int maxSize = 1024;
        [Tooltip("World units past the screen edge before a projectile is released.")]
        [SerializeField, Min(0f)] private float despawnMargin = 1f;

        private ObjectPool<Projectile> pool;
        private ContactFilter2D hitFilter;
        private readonly List<Projectile> active = new List<Projectile>();

        public int CountActive => pool.CountActive;
        public int CountInactive => pool.CountInactive;
        public int TotalCreated { get; private set; }
        public int TotalHits { get; private set; }
        public Rect ViewBounds { get; private set; }
        public ContactFilter2D HitFilter => hitFilter;
        public PlayerHealth PlayerTarget => playerTarget;
        public ArenaController Arena => arena;
        /// <summary>Physics layer of arena obstacles (-1 if the layer does not exist). Ricochets never pick them as targets.</summary>
        public int ObstacleLayer { get; private set; } = -1;

        private void Awake()
        {
            hitFilter = new ContactFilter2D { useLayerMask = true, layerMask = hitMask, useTriggers = true };
            ObstacleLayer = LayerMask.NameToLayer("Obstacle");
            RefreshBounds();

            pool = new ObjectPool<Projectile>(Create, OnGet, OnRelease, OnDestroyItem, true, prewarm, maxSize);

            var warm = new Projectile[prewarm];
            for (int i = 0; i < prewarm; i++)
                warm[i] = pool.Get();
            for (int i = 0; i < prewarm; i++)
                pool.Release(warm[i]);
        }

        private void Update() => RefreshBounds();

        public Projectile Get()
        {
            Projectile projectile = pool.Get();
            projectile.PoolIndex = active.Count;
            active.Add(projectile);
            return projectile;
        }

        public void Release(Projectile projectile)
        {
            int index = projectile.PoolIndex;
            if (index >= 0 && index < active.Count && active[index] == projectile)
            {
                Projectile last = active[active.Count - 1];
                active[index] = last;
                last.PoolIndex = index;
                active.RemoveAt(active.Count - 1);
            }
            projectile.PoolIndex = -1;
            pool.Release(projectile);
        }

        /// <summary>Returns every bullet in flight to the pool (start of a round, so nothing carries over).</summary>
        public void ReleaseAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Projectile projectile = active[i];
                projectile.PoolIndex = -1;
                pool.Release(projectile);
            }
            active.Clear();
        }

        public void RegisterHit() => TotalHits++;

        private void RefreshBounds()
        {
            float halfHeight = viewCamera.orthographicSize + despawnMargin;
            float halfWidth = viewCamera.orthographicSize * viewCamera.aspect + despawnMargin;
            Vector3 center = viewCamera.transform.position;
            ViewBounds = new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
        }

        private Projectile Create()
        {
            TotalCreated++;
            if (TotalCreated > prewarm)
                Debug.LogWarning($"ProjectilePool grew past its prewarm size ({prewarm}): {TotalCreated} created. Raise Prewarm or check for leaks.", this);

            Projectile projectile = Instantiate(prefab, transform);
            projectile.Bind(this);
            return projectile;
        }

        private static void OnGet(Projectile projectile) => projectile.gameObject.SetActive(true);

        private static void OnRelease(Projectile projectile) => projectile.gameObject.SetActive(false);

        // On leaving Play mode the scene objects are already gone when Unity clears the pool.
        private static void OnDestroyItem(Projectile projectile)
        {
            if (projectile != null)
                Destroy(projectile.gameObject);
        }
    }
}
