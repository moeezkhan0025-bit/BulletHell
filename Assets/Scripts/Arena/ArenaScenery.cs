using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>
    /// The layered look of the colosseum, built from an ArenaData: floor with decals, back wall with the gates the
    /// enemies come out of, side walls, the crowd, and a FOREGROUND layer (front railing, drapes, torches, front crowd)
    /// that draws over gameplay. Every piece is its own SpriteRenderer on a sorting layer, never one flattened image.
    /// It is placeholder art (tinted tiles and shapes); real art replaces the tile / shape sprites listed here or the
    /// renderers under the Scenery child without touching code. Nothing here has a collider: the arena's grid and wall
    /// colliders decide what blocks. Also reports the view rectangle the fixed camera frames.
    /// </summary>
    public sealed class ArenaScenery : MonoBehaviour
    {
        [Header("Sprites (placeholders; swap for real art)")]
        [SerializeField] private Sprite squareSprite;
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Sprite ringSprite;
        [Tooltip("Tiled over the floor.")]
        [SerializeField] private Sprite floorTile;
        [Tooltip("Tiled over walls and the railing.")]
        [SerializeField] private Sprite stoneTile;
        [Tooltip("Tiled over the stands.")]
        [SerializeField] private Sprite crowdTile;
        [SerializeField] private Sprite checkerTile;

        [Header("Layout (world units)")]
        [SerializeField, Min(0.5f)] private float backWallHeight = 1.9f;
        [SerializeField, Min(0.2f)] private float sideWallWidth = 1f;
        [Tooltip("The front railing, from the floor's front edge down. Its top edge overlaps the floor a little.")]
        [SerializeField, Min(0.3f)] private float railHeight = 1.2f;
        [SerializeField, Min(0f)] private float railOverlap = 0.25f;
        [Tooltip("Front crowd below the railing (the HUD sits over it).")]
        [SerializeField, Min(0f)] private float frontCrowdHeight = 0.7f;
        [Tooltip("Stands visible beyond the walls at the camera edge.")]
        [SerializeField, Min(0f)] private float standsMargin = 0.7f;

        private static readonly Color CrowdDim = new Color(0.4f, 0.4f, 0.45f);
        private static readonly Color CrowdNear = new Color(0.72f, 0.7f, 0.76f);
        private static readonly Color Dark = new Color(0.1f, 0.07f, 0.08f);
        private static readonly Color Red = new Color(0.7f, 0.15f, 0.15f);
        private static readonly Color Gold = new Color(0.95f, 0.75f, 0.25f);
        private static readonly Color Flame = new Color(1f, 0.55f, 0.1f);
        private static readonly Color Wood = new Color(0.45f, 0.28f, 0.16f);

        private Transform root;

        /// <summary>What the fixed camera frames: the floor, the walls, the railing and a bit of the stands.</summary>
        public Rect ViewRect { get; private set; }

        /// <summary>Total height of the foreground band below the floor (railing plus front crowd, minus the overlap).</summary>
        public float FrontBand => railHeight - railOverlap + frontCrowdHeight;

        public void Rebuild(ArenaData data, Vector2[] gates)
        {
            if (root != null)
                Destroy(root.gameObject);
            root = new GameObject("Scenery").transform;
            root.SetParent(transform, false);

            Rect b = data.Bounds;
            float side = sideWallWidth;
            float top = b.yMax + backWallHeight;
            float railTop = b.yMin + railOverlap;
            float railBottom = railTop - railHeight;
            float crowdBottom = railBottom - frontCrowdHeight;

            ViewRect = Rect.MinMaxRect(b.xMin - side - standsMargin, crowdBottom, b.xMax + side + standsMargin, top + standsMargin);

            // One painted backdrop replaces the placeholder stands, floor, walls, torches and railing (the image has its own).
            // The camera frames the image exactly; obstacles, traps and bullets are untouched.
            ArenaArt art = GameServices.Ensure().Config.ArenaArt;
            if (art != null && art.UseBackdrop)
            {
                Vector2 size = art.Backdrop.bounds.size;
                ViewRect = new Rect(art.BackdropPosition - size * 0.5f, size);
                Add(Group("Backdrop"), "Backdrop", art.Backdrop, ViewRect, Color.white, SortingLayers.Background, art.BackdropSortingOrder, false);
                return;
            }

            BuildStands(b, side, top, crowdBottom);
            BuildFloor(data, b);
            BuildWalls(data, gates, b, side, top);
            BuildTorches(b, side, top);
            BuildForeground(b, side, railTop, railBottom, crowdBottom);
        }

        // ---- Background layers

        private void BuildStands(Rect b, float side, float top, float crowdBottom)
        {
            Transform group = Group("Stands");
            Rect backdrop = Rect.MinMaxRect(b.xMin - 40f, b.yMin - 30f, b.xMax + 40f, top + 30f);
            Add(group, "CrowdBackdrop", crowdTile, backdrop, CrowdDim, SortingLayers.Background, -100, true);
            Add(group, "CrowdTop", crowdTile, Rect.MinMaxRect(b.xMin - side - 4f, top, b.xMax + side + 4f, top + 5f), CrowdNear, SortingLayers.Background, -50, true);
            Add(group, "CrowdLeft", crowdTile, Rect.MinMaxRect(b.xMin - side - 5f, crowdBottom, b.xMin - side, top), CrowdNear, SortingLayers.Background, -50, true);
            Add(group, "CrowdRight", crowdTile, Rect.MinMaxRect(b.xMax + side, crowdBottom, b.xMax + side + 5f, top), CrowdNear, SortingLayers.Background, -50, true);
        }

        private void BuildFloor(ArenaData data, Rect b)
        {
            Transform group = Group("Floor");
            Add(group, "Floor", floorTile, b, data.FloorColor, SortingLayers.Background, 0, true);

            // Low-contrast decals: interactive things must always read stronger than these.
            Vector2 c = b.center;
            var decal = new Color(0.72f, 0.3f, 0.25f, 0.4f);
            AddShape(group, "CrestRing", ringSprite, c, new Vector2(6.6f, 4.4f), decal, 1);
            AddShape(group, "CrestFill", circleSprite, c, new Vector2(5.4f, 3.5f), new Color(0.8f, 0.55f, 0.3f, 0.12f), 1);

            var strip = new Color(0.65f, 0.2f, 0.18f, 0.4f);
            Add(group, "StripTop", squareSprite, new Rect(c.x - 0.55f, b.yMax - 1.3f, 1.1f, 1.3f), strip, SortingLayers.Background, 1, false);
            Add(group, "StripBottom", squareSprite, new Rect(c.x - 0.55f, b.yMin, 1.1f, 1.3f), strip, SortingLayers.Background, 1, false);
            foreach (float sign in new[] { -1f, 1f })
            {
                float x = c.x + sign * (b.width * 0.5f - 1.3f);
                Add(group, "StripUpper", squareSprite, new Rect(x - 0.45f, b.yMax - 2.6f, 0.9f, 2.3f), strip, SortingLayers.Background, 1, false);
                Add(group, "StripLower", squareSprite, new Rect(x - 0.45f, b.yMin + 0.3f, 0.9f, 2.3f), strip, SortingLayers.Background, 1, false);
                float checkerX = c.x + sign * (b.width * 0.5f - 2.4f);
                Add(group, "Checker", checkerTile, new Rect(checkerX - 0.25f, b.yMin + 0.1f, 0.5f, b.height - 0.2f), new Color(0.3f, 0.2f, 0.15f, 0.5f), SortingLayers.Background, 1, true);
            }
        }

        private void BuildWalls(ArenaData data, Vector2[] gates, Rect b, float side, float top)
        {
            Transform group = Group("Walls");
            Color stone = data.WallColor;
            var sideStone = new Color(stone.r * 0.85f, stone.g * 0.85f, stone.b * 0.85f, 1f);

            Add(group, "BackWall", stoneTile, Rect.MinMaxRect(b.xMin - side, b.yMax, b.xMax + side, top), stone, SortingLayers.Background, 10, true);
            Add(group, "SideWallLeft", stoneTile, Rect.MinMaxRect(b.xMin - side, b.yMin - 0.1f, b.xMin, b.yMax), sideStone, SortingLayers.Background, 10, true);
            Add(group, "SideWallRight", stoneTile, Rect.MinMaxRect(b.xMax, b.yMin - 0.1f, b.xMax + side, b.yMax), sideStone, SortingLayers.Background, 10, true);
            // Cap along the top of the back wall.
            Add(group, "BackWallCap", squareSprite, Rect.MinMaxRect(b.xMin - side, top - 0.18f, b.xMax + side, top), new Color(stone.r * 0.7f, stone.g * 0.7f, stone.b * 0.7f), SortingLayers.Background, 12, false);

            // Gates: an arch in the back wall (or a door in a side wall) for every spawn gate of the arena.
            foreach (Vector2 gate in gates)
            {
                if (gate.y >= b.yMax - 1.5f)
                    BuildArch(group, gate.x, b.yMax, stone);
                else if (Mathf.Abs(gate.x) >= b.xMax - 1.5f)
                    BuildSideDoor(group, gate.x < 0f ? b.xMin - side * 0.5f : b.xMax + side * 0.5f, gate.y, side);
            }

            // The emperor's box at the middle of the back wall, banners between it and the gates.
            float cx = b.center.x;
            Add(group, "BoxBack", squareSprite, new Rect(cx - 1.7f, b.yMax + 0.1f, 3.4f, 1.3f), Dark, SortingLayers.Background, 13, false);
            AddShape(group, "EmperorHead", circleSprite, new Vector2(cx, b.yMax + 1.05f), new Vector2(0.75f, 0.8f), new Color(0.55f, 0.75f, 0.3f), 14);
            Add(group, "BoxRail", squareSprite, new Rect(cx - 1.75f, b.yMax + 0.05f, 3.5f, 0.55f), Wood, SortingLayers.Background, 15, false);
            Add(group, "BoxDrape", squareSprite, new Rect(cx - 1.35f, b.yMax + 0.1f, 2.7f, 0.42f), Red, SortingLayers.Background, 16, false);
            Add(group, "BoxCrest", squareSprite, new Rect(cx - 0.4f, b.yMax + 0.2f, 0.8f, 0.14f), Gold, SortingLayers.Background, 17, false);
            foreach (float sign in new[] { -1f, 1f })
            {
                float bx = cx + sign * 2.7f;
                Add(group, "Banner", squareSprite, new Rect(bx - 0.4f, b.yMax + 0.35f, 0.8f, 1.25f), Red, SortingLayers.Background, 13, false);
                AddShape(group, "BannerEmblem", circleSprite, new Vector2(bx, b.yMax + 1.05f), new Vector2(0.4f, 0.4f), new Color(0.95f, 0.9f, 0.85f), 14);
                float cornerX = cx + sign * (b.width * 0.5f - 0.2f);
                Add(group, "CornerBanner", squareSprite, new Rect(cornerX - 0.45f, b.yMax + 0.4f, 0.9f, 1.2f), sign < 0f ? Red : new Color(0.15f, 0.5f, 0.3f), SortingLayers.Background, 13, false);
            }
        }

        private void BuildArch(Transform group, float x, float wallBase, Color stone)
        {
            const float width = 1.9f, height = 1.5f;
            var frame = new Color(stone.r * 0.75f, stone.g * 0.75f, stone.b * 0.75f);
            Add(group, "ArchFrame", squareSprite, new Rect(x - width * 0.5f - 0.18f, wallBase, width + 0.36f, height - 0.1f), frame, SortingLayers.Background, 11, false);
            AddShape(group, "ArchFrameTop", circleSprite, new Vector2(x, wallBase + height - 0.1f), new Vector2(width + 0.36f, 1.1f), frame, 11);
            Add(group, "Gate", squareSprite, new Rect(x - width * 0.5f, wallBase, width, height - 0.1f), Dark, SortingLayers.Background, 12, false);
            AddShape(group, "GateTop", circleSprite, new Vector2(x, wallBase + height - 0.1f), new Vector2(width, 0.95f), Dark, 12);
            for (int i = -2; i <= 2; i++)
                Add(group, "Bar", squareSprite, new Rect(x + i * 0.34f - 0.03f, wallBase + 0.05f, 0.06f, height + 0.25f), new Color(0.42f, 0.4f, 0.42f), SortingLayers.Background, 13, false);
        }

        private void BuildSideDoor(Transform group, float x, float y, float wallWidth)
        {
            Add(group, "SideDoorFrame", squareSprite, new Rect(x - wallWidth * 0.5f, y - 0.75f, wallWidth, 1.5f), new Color(0.3f, 0.24f, 0.2f), SortingLayers.Background, 11, false);
            Add(group, "SideDoor", squareSprite, new Rect(x - wallWidth * 0.5f + 0.1f, y - 0.65f, wallWidth - 0.2f, 1.3f), Dark, SortingLayers.Background, 12, false);
        }

        private void BuildTorches(Rect b, float side, float top)
        {
            Transform group = Group("Torches");
            foreach (float sign in new[] { -1f, 1f })
            {
                float x = sign * (b.width * 0.5f + side * 0.5f);
                foreach (float y in new[] { b.yMax - 0.3f, b.center.y })
                    Torch(group, new Vector2(x, y), SortingLayers.Background, 14);
                Torch(group, new Vector2(sign * (b.width * 0.5f - 0.1f), b.yMax + 0.6f), SortingLayers.Background, 14);
            }
        }

        // ---- Foreground layer: draws over gameplay

        private void BuildForeground(Rect b, float side, float railTop, float railBottom, float crowdBottom)
        {
            Transform group = Group("Foreground");
            float left = b.xMin - side, right = b.xMax + side;
            string layer = SortingLayers.Foreground;

            Add(group, "FrontCrowd", crowdTile, Rect.MinMaxRect(left - 5f, crowdBottom, right + 5f, railBottom), CrowdNear, layer, 0, true);
            Add(group, "Railing", stoneTile, Rect.MinMaxRect(left, railBottom, right, railTop), new Color(0.62f, 0.5f, 0.4f), layer, 1, true);
            Add(group, "RailingCap", squareSprite, Rect.MinMaxRect(left, railTop - 0.12f, right, railTop), new Color(0.45f, 0.35f, 0.28f), layer, 2, false);

            // Drapes between the posts, a purple banner at the middle.
            float[] posts = { left + 0.25f, b.center.x - b.width * 0.25f, b.center.x, b.center.x + b.width * 0.25f, right - 0.25f };
            for (int i = 0; i < posts.Length - 1; i++)
            {
                Add(group, "Drape", squareSprite, Rect.MinMaxRect(posts[i] + 0.3f, railBottom + 0.15f, posts[i + 1] - 0.3f, railTop - 0.35f), Red, layer, 3, false);
            }
            Add(group, "CenterBanner", squareSprite, new Rect(b.center.x - 0.8f, railBottom + 0.05f, 1.6f, railHeight - 0.35f), new Color(0.45f, 0.2f, 0.6f), layer, 4, false);
            AddShape(group, "CenterEmblem", circleSprite, new Vector2(b.center.x, railBottom + (railHeight - 0.35f) * 0.55f), new Vector2(0.45f, 0.45f), new Color(0.9f, 0.9f, 0.95f), 5, layer);

            foreach (float x in posts)
            {
                Add(group, "Post", squareSprite, new Rect(x - 0.28f, railBottom, 0.56f, railHeight + 0.2f), new Color(0.7f, 0.58f, 0.45f), layer, 6, false);
                Torch(group, new Vector2(x, railTop + 0.45f), layer, 7);
            }
        }

        // ---- helpers

        private Transform Group(string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(root, false);
            return group;
        }

        private void Torch(Transform parent, Vector2 position, string layer, int order)
        {
            Add(parent, "TorchPost", squareSprite, new Rect(position.x - 0.16f, position.y - 0.5f, 0.32f, 0.6f), new Color(0.6f, 0.5f, 0.4f), layer, order, false);
            AddShape(parent, "TorchBowl", circleSprite, position + new Vector2(0f, 0.05f), new Vector2(0.44f, 0.2f), Gold, order + 1, layer);
            AddShape(parent, "Flame", circleSprite, position + new Vector2(0f, 0.28f), new Vector2(0.34f, 0.5f), Flame, order + 2, layer);
            AddShape(parent, "FlameCore", circleSprite, position + new Vector2(0f, 0.22f), new Vector2(0.16f, 0.26f), new Color(1f, 0.9f, 0.4f), order + 3, layer);
        }

        private SpriteRenderer AddShape(Transform parent, string name, Sprite sprite, Vector2 center, Vector2 size, Color color, int order, string layer = SortingLayers.Background)
            => Add(parent, name, sprite, new Rect(center - size * 0.5f, size), color, layer, order, false);

        private SpriteRenderer Add(Transform parent, string name, Sprite sprite, Rect rect, Color color, string layer, int order, bool tiled)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sortingLayerID = SortingLayers.Id(layer);
            renderer.sortingOrder = order;
            renderer.color = color;

            if (sprite == null)
                sprite = squareSprite;
            renderer.sprite = sprite;
            go.transform.position = rect.center;
            if (tiled && sprite != squareSprite)
            {
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = rect.size;
            }
            else
            {
                Vector2 native = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
                go.transform.localScale = new Vector3(rect.width / native.x, rect.height / native.y, 1f);
            }
            return renderer;
        }
    }
}
