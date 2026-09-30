using BulletHell.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace BulletHell.Feedback
{
    /// <summary>
    /// Runtime home of the pooled feedback effects: particle presets (a fixed ring of ParticleSystems per preset, the oldest
    /// is recycled, so nothing is allocated or grown during play) and the death ghosts (a fixed ring of sprite copies that
    /// squash, flash and dissolve where an enemy died, so the enemy itself can go back to its pool immediately). Created on
    /// first use inside the current scene. Call FeedbackHub.Play / FeedbackHub.SpawnGhost.
    /// </summary>
    public sealed class FeedbackHub : MonoBehaviour
    {
        private sealed class Ghost
        {
            public Transform Root;
            public Transform Body;
            public SpriteRenderer Renderer;
            public SpriteFx Fx;
            public bool Active;
            public float Time;
            public Vector3 BaseScale;
            public Vector3 BaseLocal;
            public float SpriteHeight;
            public LifeCycleTuning Tuning;
        }

        private static FeedbackHub instance;
        private static bool quitting;

        private FeedbackTuning tuning;
        private ParticleSystem[][] rings;
        private int[] next;
        private Ghost[] ghosts;
        private int nextGhost;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            quitting = false;
            Application.quitting -= OnQuitting;   // Reload Domain is off: never stack a second handler
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting() => quitting = true;

        private static FeedbackHub Instance
        {
            get
            {
                if (instance == null && !quitting && Application.isPlaying)
                {
                    var go = new GameObject("FeedbackHub");
                    instance = go.AddComponent<FeedbackHub>();
                }
                return instance;
            }
        }

        /// <summary>Plays a particle preset at a world position. `count` > 0 emits that many instead of the preset's own burst.</summary>
        public static void Play(VfxKind kind, Vector3 position, int count = 0)
        {
            FeedbackHub hub = Instance;
            if (hub != null)
                hub.PlayInternal(kind, position, count);
        }

        /// <summary>
        /// Leaves a dissolving copy of a sprite where an enemy died. `feet` is the enemy's root (its ground position,
        /// which the ghost sorts by); `body` is the sprite to copy.
        /// </summary>
        public static void SpawnGhost(SpriteRenderer body, Transform feet, LifeCycleTuning lifeCycle)
        {
            FeedbackHub hub = Instance;
            if (hub != null && body != null && lifeCycle != null)
                hub.SpawnGhostInternal(body, feet, lifeCycle);
        }

        private void Awake()
        {
            tuning = GameServices.Ensure().Config.Feedback;
            BuildParticles();
            BuildGhosts();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void BuildParticles()
        {
            int kinds = System.Enum.GetValues(typeof(VfxKind)).Length;
            rings = new ParticleSystem[kinds][];
            next = new int[kinds];
            for (int k = 0; k < kinds; k++)
            {
                ParticleSystem prefab = tuning.Prefab((VfxKind)k);
                if (prefab == null)
                {
                    rings[k] = new ParticleSystem[0];
                    continue;
                }
                rings[k] = new ParticleSystem[tuning.ParticlePoolPerPreset];
                for (int i = 0; i < rings[k].Length; i++)
                {
                    ParticleSystem ps = Instantiate(prefab, transform);
                    ps.gameObject.name = prefab.name;
                    rings[k][i] = ps;
                }
            }
        }

        private void BuildGhosts()
        {
            ghosts = new Ghost[tuning.GhostPoolSize];
            for (int i = 0; i < ghosts.Length; i++)
            {
                var root = new GameObject("DeathGhost");
                root.transform.SetParent(transform, false);
                var group = root.AddComponent<SortingGroup>();
                group.sortingLayerID = SortingLayers.Id(SortingLayers.Characters);

                var bodyGo = new GameObject("Body");
                bodyGo.transform.SetParent(root.transform, false);
                var renderer = bodyGo.AddComponent<SpriteRenderer>();
                renderer.sortingLayerID = SortingLayers.Id(SortingLayers.Characters);
                var fx = root.AddComponent<SpriteFx>();
                fx.SetRenderers(new[] { renderer });
                root.SetActive(false);

                ghosts[i] = new Ghost { Root = root.transform, Body = bodyGo.transform, Renderer = renderer, Fx = fx };
            }
        }

        private void PlayInternal(VfxKind kind, Vector3 position, int count)
        {
            ParticleSystem[] ring = rings[(int)kind];
            if (ring.Length == 0)
                return;
            int slot = next[(int)kind];
            next[(int)kind] = (slot + 1) % ring.Length;

            ParticleSystem ps = ring[slot];
            ps.transform.position = position;
            ps.Clear(true);
            if (count > 0)
                ps.Emit(count);
            else
                ps.Play(true);
        }

        private void SpawnGhostInternal(SpriteRenderer source, Transform feet, LifeCycleTuning lifeCycle)
        {
            Ghost g = ghosts[nextGhost];
            nextGhost = (nextGhost + 1) % ghosts.Length;

            g.Tuning = lifeCycle;
            g.Time = 0f;
            g.Active = true;
            g.Root.position = feet.position;
            g.Root.gameObject.SetActive(true);

            g.Body.position = source.transform.position;
            g.Body.rotation = source.transform.rotation;
            g.Body.localScale = source.transform.lossyScale;
            g.BaseScale = g.Body.localScale;
            g.BaseLocal = g.Body.localPosition;
            g.Renderer.sprite = source.sprite;
            g.Renderer.color = source.color;
            g.Renderer.sharedMaterial = source.sharedMaterial;
            g.Renderer.flipX = source.flipX;
            g.SpriteHeight = source.sprite != null ? source.sprite.bounds.size.y * Mathf.Abs(g.BaseScale.y) : 1f;
            g.Fx.ClearAll();
        }

        /// <summary>Particles alive in all pooled systems (the performance overlay and logger; development use).</summary>
        public static int CountLiveParticles()
        {
            FeedbackHub hub = instance;
            if (hub == null || hub.rings == null)
                return 0;
            int total = 0;
            for (int k = 0; k < hub.rings.Length; k++)
                for (int i = 0; i < hub.rings[k].Length; i++)
                    total += hub.rings[k][i].particleCount;
            return total;
        }

        private void Update()
        {
            using var _ = BulletHell.Perf.PerfMarkers.FxHub.Auto();
            if (ghosts == null)
                return;
            float dt = Time.deltaTime;
            for (int i = 0; i < ghosts.Length; i++)
            {
                Ghost g = ghosts[i];
                if (g.Active)
                    StepGhost(g, dt);
            }
        }

        private static void StepGhost(Ghost g, float dt)
        {
            g.Time += dt;
            LifeCycleTuning t = g.Tuning;
            float squash = Mathf.Clamp01(g.Time / t.SquashSeconds);
            float eased = 1f - (1f - squash) * (1f - squash);
            float width = Mathf.Lerp(1f, t.SquashWidth, eased);
            float height = Mathf.Lerp(1f, t.SquashHeight, eased);
            g.Body.localScale = new Vector3(g.BaseScale.x * width, g.BaseScale.y * height, g.BaseScale.z);

            // Squash toward the ground: keep the bottom of the sprite where it was.
            g.Body.localPosition = new Vector3(g.BaseLocal.x, g.BaseLocal.y - g.SpriteHeight * (1f - height) * 0.5f, g.BaseLocal.z);

            float dissolveT = Mathf.Clamp01((g.Time - t.SquashSeconds) / t.DissolveSeconds);
            g.Fx.SetFlash(1f - Mathf.Clamp01((g.Time - t.SquashSeconds * 0.5f) / (t.SquashSeconds * 1.5f)));
            g.Fx.SetDissolve(dissolveT);

            if (g.Time >= t.SquashSeconds + t.DissolveSeconds)
            {
                g.Active = false;
                g.Root.gameObject.SetActive(false);
            }
        }
    }
}
