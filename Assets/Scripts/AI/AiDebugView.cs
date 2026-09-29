using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Settings;
using UnityEngine;

namespace BulletHell.AI
{
    /// <summary>
    /// Debug view of the enemy AI, toggled by the "Show AI debug" setting (development builds only): the flow field as
    /// small arrows (every few cells) coloured from near the player (warm) to far (cool), cells the enemies can't walk
    /// in as faint red dots, and a line of sight ray from every ranged enemy to the player, green when it is clear and
    /// red when an obstacle is in the way. Two vertex-coloured meshes with preallocated buffers, drawn over the
    /// gameplay on the Foreground layer.
    /// </summary>
    public sealed class AiDebugView : MonoBehaviour
    {
        private const int MaxRays = 64;

        [SerializeField] private NavigationService navigation;
        [Tooltip("Draw the flow field arrow of every Nth cell.")]
        [SerializeField, Range(1, 6)] private int cellStride = 2;
        [Tooltip("Arrow length as a fraction of the stride between arrows.")]
        [SerializeField, Range(0.2f, 1f)] private float arrowSize = 0.6f;
        [SerializeField] private Color nearColor = new Color(1f, 0.9f, 0.2f, 0.75f);
        [SerializeField] private Color farColor = new Color(0.2f, 0.75f, 1f, 0.6f);
        [SerializeField] private Color blockedColor = new Color(1f, 0.2f, 0.2f, 0.3f);
        [SerializeField] private Color lineClear = new Color(0.3f, 1f, 0.3f, 0.85f);
        [SerializeField] private Color lineBlocked = new Color(1f, 0.3f, 0.3f, 0.85f);
        [SerializeField, Min(0.01f)] private float rayWidth = 0.05f;

        private SettingsService settings;
        private MeshRenderer fieldRenderer, rayRenderer;
        private Mesh fieldMesh, rayMesh;
        private FlowField shownField;
        private int shownVersion = -1;
        private Vector3[] fieldVertices = new Vector3[0];
        private Color[] fieldColors = new Color[0];
        private int[] fieldIndices = new int[0];
        private readonly Vector3[] rayVertices = new Vector3[MaxRays * 4];
        private readonly Color[] rayColors = new Color[MaxRays * 4];
        private readonly int[] rayIndices = new int[MaxRays * 6];
        private bool visible;

        private void Awake()
        {
            settings = GameServices.Ensure().Settings;
            var material = new Material(Shader.Find("Sprites/Default"));
            fieldMesh = new Mesh { name = "AiFlowField" };
            rayMesh = new Mesh { name = "AiLineOfSight" };
            fieldRenderer = CreateLayer("FlowField", fieldMesh, material, 200);
            rayRenderer = CreateLayer("LineOfSight", rayMesh, material, 201);
            Apply();
        }

        private MeshRenderer CreateLayer(string objectName, Mesh mesh, Material material, int order)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingLayerID = SortingLayers.Id(SortingLayers.Foreground);
            meshRenderer.sortingOrder = order;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.enabled = false;
            return meshRenderer;
        }

        private void OnEnable()
        {
            settings.Changed += Apply;
            Apply();
        }

        private void OnDisable() => settings.Changed -= Apply;

        private void OnDestroy()
        {
            if (!Application.isPlaying)
                return;   // leaving Play mode: the scene is being torn down anyway
            if (fieldMesh != null) Destroy(fieldMesh);
            if (rayMesh != null) Destroy(rayMesh);
        }

        private void Apply()
        {
            visible = Debug.isDebugBuild && settings.Current.showAiDebug;
            if (fieldRenderer != null) fieldRenderer.enabled = visible;
            if (rayRenderer != null) rayRenderer.enabled = visible;
            if (!visible)
                shownField = null;
        }

        private void LateUpdate()
        {
            if (!visible)
                return;
            DrawField();
            DrawRays();
        }

        private void DrawField()
        {
            FlowField field = navigation.Field;
            if (field == null)
            {
                fieldMesh.Clear();
                shownField = null;
                return;
            }
            if (field == shownField && field.Version == shownVersion)
                return;
            shownField = field;
            shownVersion = field.Version;

            int columns = (field.Columns + cellStride - 1) / cellStride;
            int rows = (field.Rows + cellStride - 1) / cellStride;
            int capacity = columns * rows * 3;
            if (fieldVertices.Length < capacity)
            {
                fieldVertices = new Vector3[capacity];
                fieldColors = new Color[capacity];
                fieldIndices = new int[capacity];
            }

            var grid = field.Grid;
            float maxCost = 1f;
            for (int r = 0; r < field.Rows; r += cellStride)
                for (int c = 0; c < field.Columns; c += cellStride)
                {
                    float cost = field.CostAt(c, r);
                    if (!float.IsPositiveInfinity(cost) && cost > maxCost)
                        maxCost = cost;
                }

            float length = grid.CellSize * cellStride * arrowSize;
            int count = 0;
            for (int r = 0; r < field.Rows; r += cellStride)
            {
                for (int c = 0; c < field.Columns; c += cellStride)
                {
                    Vector2 center = grid.CellCenter(c, r);
                    Vector2 flow = field.FlowAt(c, r);
                    if (!field.IsWalkable(c, r) && flow == Vector2.zero)
                    {
                        AddTriangle(ref count, center, Vector2.up, length * 0.25f, blockedColor);   // a tiny dot: blocked for good
                        continue;
                    }
                    if (flow == Vector2.zero)
                        continue;

                    Color color = field.IsWalkable(c, r)
                        ? Color.Lerp(nearColor, farColor, field.CostAt(c, r) / maxCost)
                        : blockedColor;   // inside an obstacle's margin: it only leads out
                    AddTriangle(ref count, center, flow, length, color);
                }
            }

            fieldMesh.Clear();
            if (count == 0)
                return;
            fieldMesh.SetVertices(fieldVertices, 0, count);
            fieldMesh.SetColors(fieldColors, 0, count);
            fieldMesh.SetIndices(fieldIndices, 0, count, MeshTopology.Triangles, 0, false);
            fieldMesh.bounds = new Bounds(Vector3.zero, new Vector3(200f, 200f, 10f));
        }

        // An arrow: a triangle whose tip points along the flow, centred on the cell.
        private void AddTriangle(ref int count, Vector2 center, Vector2 direction, float length, Color color)
        {
            Vector2 side = new Vector2(-direction.y, direction.x);
            Vector2 tip = center + direction * (length * 0.5f);
            Vector2 left = center - direction * (length * 0.5f) + side * (length * 0.32f);
            Vector2 right = center - direction * (length * 0.5f) - side * (length * 0.32f);
            fieldVertices[count] = tip; fieldColors[count] = color; fieldIndices[count] = count++;
            fieldVertices[count] = right; fieldColors[count] = color; fieldIndices[count] = count++;
            fieldVertices[count] = left; fieldColors[count] = color; fieldIndices[count] = count++;
        }

        private void DrawRays()
        {
            int vertices = 0, indices = 0;
            var enemies = navigation.Enemies;
            Vector2 target = navigation.Player.Position;
            for (int i = 0; i < enemies.Count && vertices + 4 <= rayVertices.Length; i++)
            {
                Enemy enemy = enemies[i];
                EnemyBrain brain = enemy.Brain;
                if (!enemy.IsAlive || brain == null || !brain.IsActive || !UsesLine(enemy.Data.Behavior))
                    continue;

                Vector2 from = enemy.BodyCenter;
                Vector2 along = target - from;
                if (along.sqrMagnitude < 1e-6f)
                    continue;
                Vector2 side = new Vector2(-along.y, along.x).normalized * (rayWidth * 0.5f);
                Color color = brain.Agent.LastLineClear ? lineClear : lineBlocked;

                rayVertices[vertices] = from + side; rayColors[vertices] = color;
                rayVertices[vertices + 1] = from - side; rayColors[vertices + 1] = color;
                rayVertices[vertices + 2] = target - side; rayColors[vertices + 2] = color;
                rayVertices[vertices + 3] = target + side; rayColors[vertices + 3] = color;
                rayIndices[indices++] = vertices; rayIndices[indices++] = vertices + 1; rayIndices[indices++] = vertices + 2;
                rayIndices[indices++] = vertices; rayIndices[indices++] = vertices + 2; rayIndices[indices++] = vertices + 3;
                vertices += 4;
            }

            rayMesh.Clear();
            if (vertices == 0)
                return;
            rayMesh.SetVertices(rayVertices, 0, vertices);
            rayMesh.SetColors(rayColors, 0, vertices);
            rayMesh.SetIndices(rayIndices, 0, indices, MeshTopology.Triangles, 0, false);
            rayMesh.bounds = new Bounds(Vector3.zero, new Vector3(200f, 200f, 10f));
        }

        private static bool UsesLine(EnemyBehavior behavior) =>
            behavior == EnemyBehavior.Skirmisher || behavior == EnemyBehavior.Sentry || behavior == EnemyBehavior.Sniper;
    }
}
