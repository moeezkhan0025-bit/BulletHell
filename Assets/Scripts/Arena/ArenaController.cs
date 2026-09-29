using System;
using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Player;
using UnityEngine;
using UnityEngine.Pool;

namespace BulletHell.Arena
{
    /// <summary>
    /// Builds and runs the colosseum of the current round: the layered scenery, obstacles, traps, the collision grids,
    /// the fixed camera and the player's starting spot. Each round's intro (re)builds it from the round's ArenaData
    /// (breakables restored, traps idle, debris gone); traps only start when combat begins. Obstacles and traps are
    /// pooled and kept between rounds when the layout is the same.
    /// Two grids describe what blocks: <see cref="Grid"/> holds the flat footprints (walking, spawning), and
    /// <see cref="BulletGrid"/> the same footprints plus the reach above them, on a playfield that extends up the back
    /// wall by the bullet headroom (bullets fly at body height, not at the feet). Everything that needs to know what
    /// blocks movement or bullets asks this, and later systems (M8 navigation) listen to ObstacleBroken.
    /// </summary>
    public sealed class ArenaController : MonoBehaviour
    {
        private const string ObstacleLayerName = "Obstacle";

        [SerializeField] private Camera viewCamera;
        [SerializeField] private PlayerHealth player;
        [SerializeField] private PlayerData playerData;
        [Header("Prefabs and art")]
        [SerializeField] private Obstacle obstaclePrefab;
        [SerializeField] private Trap trapPrefab;
        [SerializeField] private Sprite squareSprite;
        [SerializeField] private Sprite circleSprite;
        [Header("Scene objects")]
        [Tooltip("Builds the layered floor / walls / crowd / foreground.")]
        [SerializeField] private ArenaScenery scenery;
        [Tooltip("One collider per wall (top, bottom, left, right), on the Obstacle layer: they stop player bullets and beams.")]
        [SerializeField] private BoxCollider2D[] wallColliders = new BoxCollider2D[4];
        [Header("Tuning")]
        [SerializeField, Min(0.05f)] private float cellSize = 0.25f;
        [SerializeField, Min(0.1f)] private float wallThickness = 0.5f;

        private readonly List<Obstacle> obstacles = new List<Obstacle>();
        private readonly List<Trap> traps = new List<Trap>();
        private ObjectPool<Obstacle> obstaclePool;
        private ObjectPool<Trap> trapPool;
        private RunManager run;
        private GameConfig config;
        private ArenaData current;
        private float lastAspect;

        /// <summary>Flat footprints: what blocks walking and spawning.</summary>
        public ArenaGrid Grid { get; private set; }
        /// <summary>Footprints plus their reach upwards, on a field extended up the back wall: what blocks enemy bullets.</summary>
        public ArenaGrid BulletGrid { get; private set; }
        public bool IsBuilt => Grid != null;
        public Rect Bounds => current != null ? current.Bounds : new Rect(-8f, -4.5f, 16f, 9f);
        public Vector2 PlayerSpawn => current != null ? current.PlayerSpawn : Vector2.zero;
        public IReadOnlyList<Vector2> Gates => current != null ? current.SpawnGates : Array.Empty<Vector2>();
        public IReadOnlyList<Obstacle> Obstacles => obstacles;
        public IReadOnlyList<Trap> Traps => traps;
        public ArenaData Current => current;
        /// <summary>What the fixed camera frames (floor, walls, railing, a bit of the stands).</summary>
        public Rect ViewRect => scenery != null ? scenery.ViewRect : new Rect(-9f, -5.5f, 18f, 11f);

        /// <summary>Raised when a breakable breaks. Navigation (M8) rebuilds its flow field from this.</summary>
        public event Action<Obstacle> ObstacleBroken;

        private PerspectiveTuning Perspective => config != null ? config.Perspective : PerspectiveTuning.Fallback;

        private void Awake()
        {
            GameServices services = GameServices.Ensure();
            run = services.Run;
            config = services.Config;
            obstaclePool = new ObjectPool<Obstacle>(CreateObstacle, o => o.gameObject.SetActive(true), o => o.gameObject.SetActive(false), Destroy, true, 16, 64);
            trapPool = new ObjectPool<Trap>(CreateTrap, t => t.gameObject.SetActive(true), t => t.gameObject.SetActive(false), Destroy, true, 8, 32);
        }

        private void OnEnable()
        {
            run.RoundIntroStarted += OnRoundIntroStarted;
            run.RoundStarted += OnRoundStarted;
            run.Machine.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            run.RoundIntroStarted -= OnRoundIntroStarted;
            run.RoundStarted -= OnRoundStarted;
            run.Machine.StateChanged -= OnStateChanged;
        }

        // So the colosseum is already there behind the Shop / Armory screens of a continued run.
        private void Start()
        {
            if (!IsBuilt && run.State != null)
                Build(config.GetArena(run.State.Round));
        }

        private void Update()
        {
            if (!Mathf.Approximately(viewCamera.aspect, lastAspect))
                FitCamera();
        }

        // ---- run flow

        private void OnRoundIntroStarted(int round)
        {
            ArenaData data = config.GetArena(round);
            if (data != current || !IsBuilt)
                Build(data);
            else
                ResetRound();
            PlacePlayer();
        }

        private void OnRoundStarted(int round)
        {
            foreach (Trap trap in traps)
                trap.Begin();
        }

        // The round is over (cleared or lost): traps go quiet.
        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.RoundResults || to == GameState.GameOver)
                foreach (Trap trap in traps)
                    trap.ResetTrap();
        }

        // ---- queries used by bullets and movement

        /// <summary>Sweeps a bullet's circle along its step. Owner is the obstacle id (or ArenaGrid.Border for the walls).</summary>
        public bool SegmentBlocked(Vector2 from, Vector2 to, float radius, out int owner)
        {
            if (BulletGrid == null)
            {
                owner = ArenaGrid.Free;
                return false;
            }
            return BulletGrid.SegmentBlocked(from, to, radius, out owner);
        }

        /// <summary>A bullet hit an obstacle's cells: solids ignore it, breakables take the damage.</summary>
        public void DamageObstacle(int owner, float damage)
        {
            if (owner >= 0 && owner < obstacles.Count)
                obstacles[owner].TakeDamage(damage);
        }

        // ---- building

        private void Build(ArenaData data)
        {
            Teardown();
            current = data;
            if (data == null)
                return;

            PerspectiveTuning perspective = Perspective;
            Rect bounds = data.Bounds;
            Grid = new ArenaGrid(bounds, cellSize);
            BulletGrid = new ArenaGrid(BulletBounds(bounds), cellSize);
            LayoutStatics(data);

            ObstaclePlacement[] placed = data.Obstacles;
            for (int i = 0; i < placed.Length; i++)
            {
                if (placed[i].Data == null)
                    continue;
                Obstacle obstacle = obstaclePool.Get();
                obstacle.Setup(placed[i].Data, placed[i].Position, placed[i].Size, obstacles.Count, squareSprite, circleSprite,
                               perspective.ObstacleBulletAllowance);
                obstacle.Register(Grid, BulletGrid);
                obstacle.Broken += OnObstacleBroken;
                obstacles.Add(obstacle);
            }

            foreach (TrapPlacement t in data.Traps)
            {
                if (t.Data == null)
                    continue;
                Trap trap = trapPool.Get();
                trap.Setup(t.Data, t.Position, t.Rotation, t.ExtraStartDelay, run, player, circleSprite, squareSprite);
                traps.Add(trap);
            }

            FitCamera();
        }

        private Rect BulletBounds(Rect floor) => new Rect(floor.xMin, floor.yMin, floor.width, floor.height + Perspective.BulletHeadroom);

        private void Teardown()
        {
            foreach (Obstacle obstacle in obstacles)
            {
                obstacle.Broken -= OnObstacleBroken;
                obstaclePool.Release(obstacle);
            }
            obstacles.Clear();
            foreach (Trap trap in traps)
                trapPool.Release(trap);
            traps.Clear();
            Grid = null;
            BulletGrid = null;
        }

        // Same layout, next round: breakables come back, debris goes, traps idle.
        private void ResetRound()
        {
            Grid.Clear();
            BulletGrid.Clear();
            foreach (Obstacle obstacle in obstacles)
            {
                obstacle.Restore();
                obstacle.Register(Grid, BulletGrid);
            }
            foreach (Trap trap in traps)
                trap.ResetTrap();
        }

        private void OnObstacleBroken(Obstacle obstacle) => ObstacleBroken?.Invoke(obstacle);

        private void PlacePlayer()
        {
            if (player == null || current == null)
                return;
            Vector2 spawn = Grid.NearestFree(current.PlayerSpawn, playerData != null ? playerData.BodyRadius : 0.35f);
            player.transform.position = spawn;
        }

        // ---- look and camera

        private void LayoutStatics(ArenaData data)
        {
            if (scenery != null)
                scenery.Rebuild(data);

            // Player bullets and beams stop at these. They enclose the same field as the bullet grid.
            Rect bounds = BulletBounds(data.Bounds);
            float t = wallThickness;
            var rects = new[]
            {
                new Rect(bounds.xMin - t, bounds.yMax, bounds.width + 2f * t, t),      // top
                new Rect(bounds.xMin - t, bounds.yMin - t, bounds.width + 2f * t, t),  // bottom
                new Rect(bounds.xMin - t, bounds.yMin, t, bounds.height),              // left
                new Rect(bounds.xMax, bounds.yMin, t, bounds.height),                  // right
            };
            for (int i = 0; i < 4 && i < wallColliders.Length; i++)
            {
                // The colliders are thicker than the visible walls so a fast bullet can't slip through.
                Rect thick = rects[i];
                float extra = 2f;
                if (i < 2)
                    thick = new Rect(thick.xMin, i == 0 ? thick.yMin : thick.yMin - extra, thick.width, thick.height + extra);
                else
                    thick = new Rect(i == 2 ? thick.xMin - extra : thick.xMin, thick.yMin, thick.width + extra, thick.height);
                wallColliders[i].offset = thick.center - (Vector2)wallColliders[i].transform.position;
                wallColliders[i].size = thick.size;
            }

            viewCamera.backgroundColor = data.StandsColor;
        }

        /// <summary>Frames the whole view rectangle (walls, railing, a bit of stands) at any aspect ratio; extra width or height shows more of the stands.</summary>
        private void FitCamera()
        {
            lastAspect = viewCamera.aspect;
            if (current == null)
                return;

            Rect view = ViewRect;
            viewCamera.orthographicSize = Mathf.Max(view.height * 0.5f, view.width * 0.5f / Mathf.Max(0.1f, viewCamera.aspect));
            Vector3 position = viewCamera.transform.position;
            viewCamera.transform.position = new Vector3(view.center.x, view.center.y, position.z);
        }

        // ---- pools

        private Obstacle CreateObstacle()
        {
            Obstacle obstacle = Instantiate(obstaclePrefab, transform);
            obstacle.gameObject.layer = ObstacleLayer();
            return obstacle;
        }

        private Trap CreateTrap() => Instantiate(trapPrefab, transform);

        /// <summary>The physics layer obstacles use, or Default (0) with a warning when the layer is missing.</summary>
        public static int ObstacleLayer()
        {
            int layer = LayerMask.NameToLayer(ObstacleLayerName);
            if (layer < 0)
            {
                Debug.LogWarning($"Physics layer '{ObstacleLayerName}' is missing: bullets will not be blocked by obstacles. Run BulletHell/M7.5/Setup Everything.");
                return 0;
            }
            return layer;
        }
    }
}
