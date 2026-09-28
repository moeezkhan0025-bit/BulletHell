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
        [Tooltip("Physics layers a projectile can hit.")]
        [SerializeField] private LayerMask hitMask;
        [SerializeField, Min(1)] private int prewarm = 128;
        [SerializeField, Min(1)] private int maxSize = 1024;
        [Tooltip("World units past the screen edge before a projectile is released.")]
        [SerializeField, Min(0f)] private float despawnMargin = 1f;

        private ObjectPool<Projectile> pool;
        private ContactFilter2D hitFilter;

        public int CountActive => pool.CountActive;
        public int CountInactive => pool.CountInactive;
        public int TotalCreated { get; private set; }
        public int TotalHits { get; private set; }
        public Rect ViewBounds { get; private set; }
        public ContactFilter2D HitFilter => hitFilter;

        private void Awake()
        {
            hitFilter = new ContactFilter2D { useLayerMask = true, layerMask = hitMask, useTriggers = true };
            RefreshBounds();

            pool = new ObjectPool<Projectile>(Create, OnGet, OnRelease, OnDestroyItem, true, prewarm, maxSize);

            var warm = new Projectile[prewarm];
            for (int i = 0; i < prewarm; i++)
                warm[i] = pool.Get();
            for (int i = 0; i < prewarm; i++)
                pool.Release(warm[i]);
        }

        private void Update() => RefreshBounds();

        public Projectile Get() => pool.Get();

        public void Release(Projectile projectile) => pool.Release(projectile);

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
