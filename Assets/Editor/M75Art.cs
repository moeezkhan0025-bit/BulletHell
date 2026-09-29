using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// Draws the M7.5 placeholder art (tiles, obstacles, HUD icons, button glyphs) into PNG files and sets their import
    /// settings. Only files that do not exist yet are written, so real art dropped in under the same name is never
    /// overwritten. Everything here is simple shapes: real art replaces the files or the sprite references.
    /// </summary>
    public static class M75Art
    {
        public const string Folder = "Assets/Art/Placeholder/M75";

        // ------------------------------------------------------------ Pixel canvas

        private sealed class Canvas
        {
            public readonly int W, H;
            private readonly Color[] px;

            public Canvas(int w, int h, Color fill = default)
            {
                W = w;
                H = h;
                px = new Color[w * h];
                for (int i = 0; i < px.Length; i++)
                    px[i] = fill;
            }

            public Color Get(int x, int y) => px[y * W + x];
            public void Put(int x, int y, Color c) => px[y * W + x] = c;

            /// <summary>Composites a colour over the pixel with the given coverage (0..1).</summary>
            public void Blend(int x, int y, Color src, float cover)
            {
                if (x < 0 || y < 0 || x >= W || y >= H || cover <= 0f)
                    return;
                float a = src.a * Mathf.Clamp01(cover);
                Color dst = px[y * W + x];
                float outA = a + dst.a * (1f - a);
                if (outA <= 0f)
                    return;
                px[y * W + x] = new Color(
                    (src.r * a + dst.r * dst.a * (1f - a)) / outA,
                    (src.g * a + dst.g * dst.a * (1f - a)) / outA,
                    (src.b * a + dst.b * dst.a * (1f - a)) / outA, outA);
            }

            /// <summary>Fills where the signed distance is inside (negative), with a one pixel soft edge.</summary>
            public void Fill(Func<float, float, float> signedDistance, Color color)
            {
                for (int y = 0; y < H; y++)
                    for (int x = 0; x < W; x++)
                        Blend(x, y, color, 0.5f - signedDistance(x + 0.5f, y + 0.5f));
            }

            public void Circle(float cx, float cy, float r, Color c) =>
                Fill((x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r, c);

            public void Ring(float cx, float cy, float r, float thickness, Color c) =>
                Fill((x, y) => Mathf.Abs(Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r) - thickness * 0.5f, c);

            public void Ellipse(float cx, float cy, float rx, float ry, Color c) =>
                Fill((x, y) => (Mathf.Sqrt((x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry)) - 1f) * Mathf.Min(rx, ry), c);

            public void RoundRect(float x0, float y0, float x1, float y1, float radius, Color c) =>
                Fill((x, y) => RoundRectDistance(x, y, x0, y0, x1, y1, radius), c);

            public void RoundRectOutline(float x0, float y0, float x1, float y1, float radius, float thickness, Color c) =>
                Fill((x, y) => Mathf.Abs(RoundRectDistance(x, y, x0, y0, x1, y1, radius) + thickness * 0.5f) - thickness * 0.5f, c);

            public void Rect(float x0, float y0, float x1, float y1, Color c) => RoundRect(x0, y0, x1, y1, 0f, c);

            public void Line(float ax, float ay, float bx, float by, float thickness, Color c) =>
                Fill((x, y) =>
                {
                    float dx = bx - ax, dy = by - ay;
                    float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));
                    float px2 = ax + dx * t - x, py2 = ay + dy * t - y;
                    return Mathf.Sqrt(px2 * px2 + py2 * py2) - thickness * 0.5f;
                }, c);

            /// <summary>Paints a border under everything drawn so far (where a transparent pixel touches an opaque one).</summary>
            public void OutlineBehind(Color color, int thickness)
            {
                var alpha = new float[px.Length];
                for (int i = 0; i < px.Length; i++)
                    alpha[i] = px[i].a;
                for (int y = 0; y < H; y++)
                {
                    for (int x = 0; x < W; x++)
                    {
                        if (alpha[y * W + x] >= 0.99f)
                            continue;
                        float near = 0f;
                        for (int dy = -thickness; dy <= thickness; dy++)
                            for (int dx = -thickness; dx <= thickness; dx++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= W || ny >= H || dx * dx + dy * dy > thickness * thickness)
                                    continue;
                                near = Mathf.Max(near, alpha[ny * W + nx]);
                            }
                        if (near <= 0f)
                            continue;
                        Color under = color;
                        under.a = color.a * near;
                        Color top = px[y * W + x];
                        // The outline goes under what is already there.
                        float outA = top.a + under.a * (1f - top.a);
                        px[y * W + x] = outA <= 0f ? top : new Color(
                            (top.r * top.a + under.r * under.a * (1f - top.a)) / outA,
                            (top.g * top.a + under.g * under.a * (1f - top.a)) / outA,
                            (top.b * top.a + under.b * under.a * (1f - top.a)) / outA, outA);
                    }
                }
            }

            public void Save(string path)
            {
                var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
                tex.SetPixels(px);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }

            private static float RoundRectDistance(float x, float y, float x0, float y0, float x1, float y1, float r)
            {
                float cx = (x0 + x1) * 0.5f, cy = (y0 + y1) * 0.5f;
                float hx = (x1 - x0) * 0.5f - r, hy = (y1 - y0) * 0.5f - r;
                float qx = Mathf.Abs(x - cx) - hx, qy = Mathf.Abs(y - cy) - hy;
                float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            }
        }

        private static readonly Color Ink = new Color(0.1f, 0.07f, 0.09f, 1f);

        private struct ImportSpec
        {
            public float Ppu;
            public Vector2 Pivot;
            public bool Tiled;
            public Vector4 Border;
        }

        private static string PathOf(string name) => $"{Folder}/{name}.png";

        // ------------------------------------------------------------ Entry point

        /// <summary>Creates any missing placeholder file and imports it. Returns nothing; sprites are loaded by path.</summary>
        public static void EnsureAll()
        {
            if (!Directory.Exists(Folder))
                Directory.CreateDirectory(Folder);

            Make("FloorTile", 128, 128, DrawFloorTile, new ImportSpec { Ppu = 128f / 1.5f, Pivot = new Vector2(0.5f, 0.5f), Tiled = true });
            Make("StoneTile", 128, 128, DrawStoneTile, new ImportSpec { Ppu = 128f, Pivot = new Vector2(0.5f, 0.5f), Tiled = true });
            Make("CrowdTile", 128, 128, DrawCrowdTile, new ImportSpec { Ppu = 64f, Pivot = new Vector2(0.5f, 0.5f), Tiled = true });
            Make("CheckerTile", 32, 32, DrawCheckerTile, new ImportSpec { Ppu = 64f, Pivot = new Vector2(0.5f, 0.5f), Tiled = true });
            Make("Ring", 256, 256, c => c.Ring(128f, 128f, 118f, 12f, Color.white), Single(256f));

            Make("Pillar", 64, 160, DrawPillar, new ImportSpec { Ppu = 64f, Pivot = new Vector2(0.5f, 0f) });
            Make("LowWall", 64, 64, DrawLowWall, new ImportSpec { Ppu = 64f, Pivot = new Vector2(0.5f, 0f) });
            Make("Crate", 64, 64, DrawCrate, new ImportSpec { Ppu = 64f, Pivot = new Vector2(0.5f, 0f) });
            Make("Pumpkin", 64, 64, DrawPumpkin, new ImportSpec { Ppu = 64f, Pivot = new Vector2(0.5f, 0f) });
            Make("Cabbage", 64, 64, DrawCabbage, new ImportSpec { Ppu = 64f, Pivot = new Vector2(0.5f, 0f) });

            Make("HeartFull", 64, 64, c => DrawHeart(c, true), Single(64f));
            Make("HeartEmpty", 64, 64, c => DrawHeart(c, false), Single(64f));
            Make("UIRoundRect", 64, 64, c => c.RoundRect(1f, 1f, 63f, 63f, 14f, Color.white), new ImportSpec { Ppu = 64f, Pivot = new Vector2(0.5f, 0.5f), Border = new Vector4(18, 18, 18, 18) });
            Make("UIRoundRectOutline", 64, 64, c => c.RoundRectOutline(1f, 1f, 63f, 63f, 14f, 5f, Color.white), new ImportSpec { Ppu = 64f, Pivot = new Vector2(0.5f, 0.5f), Border = new Vector4(18, 18, 18, 18) });

            Make("Icon_Basic", 64, 64, c => { c.Circle(32f, 32f, 15f, Color.white); c.OutlineBehind(Ink, 3); }, Single(64f));
            Make("Icon_Shotgun", 64, 64, DrawShotgunIcon, Single(64f));
            Make("Icon_Laser", 64, 64, c => { c.RoundRect(6f, 27f, 58f, 37f, 5f, Color.white); c.Circle(10f, 32f, 9f, Color.white); c.OutlineBehind(Ink, 3); }, Single(64f));
            Make("Icon_Gatling", 64, 64, DrawGatlingIcon, Single(64f));
            Make("Icon_Spare", 64, 64, c => { c.RoundRect(14f, 14f, 50f, 50f, 4f, Color.white); c.Circle(32f, 32f, 8f, Ink); c.OutlineBehind(Ink, 3); }, Single(64f));

            Make("Glyph_Disc", 64, 64, c => { c.Circle(32f, 32f, 29f, Color.white); c.Ring(32f, 32f, 29f, 4f, new Color(0f, 0f, 0f, 0.55f)); }, Single(64f));
            Make("Glyph_PS_Cross", 64, 64, c => PsGlyph(c, new Color(0.55f, 0.72f, 1f), g => { g.Line(20f, 20f, 44f, 44f, 6f, g.Ink()); g.Line(20f, 44f, 44f, 20f, 6f, g.Ink()); }), Single(64f));
            Make("Glyph_PS_Circle", 64, 64, c => PsGlyph(c, new Color(1f, 0.42f, 0.48f), g => g.Ring(32f, 32f, 13f, 6f, g.Ink())), Single(64f));
            Make("Glyph_PS_Square", 64, 64, c => PsGlyph(c, new Color(0.96f, 0.62f, 0.86f), g => g.RoundRectOutline(19f, 19f, 45f, 45f, 2f, 6f, g.Ink())), Single(64f));
            Make("Glyph_PS_Triangle", 64, 64, c => PsGlyph(c, new Color(0.45f, 0.92f, 0.65f), g =>
            {
                g.Line(32f, 47f, 19f, 22f, 6f, g.Ink());
                g.Line(32f, 47f, 45f, 22f, 6f, g.Ink());
                g.Line(19f, 22f, 45f, 22f, 6f, g.Ink());
            }), Single(64f));
        }

        private static ImportSpec Single(float ppu) => new ImportSpec { Ppu = ppu, Pivot = new Vector2(0.5f, 0.5f) };

        // The PS glyphs are drawn on a dark disc with a coloured shape; a tiny wrapper lets the shape lambdas ask for their ink colour.

        private static void PsGlyph(Canvas c, Color shape, Action<GlyphCanvas> draw)
        {
            c.Circle(32f, 32f, 29f, new Color(0.12f, 0.12f, 0.18f, 1f));
            c.Ring(32f, 32f, 29f, 4f, new Color(0.75f, 0.75f, 0.85f, 1f));
            draw(new GlyphCanvas(c, shape));
        }

        /// <summary>A canvas whose drawing calls with the special colour "Ink()" use the glyph's shape colour.</summary>
        private sealed class GlyphCanvas
        {
            private readonly Canvas canvas;
            private readonly Color colour;
            public GlyphCanvas(Canvas c, Color shape) { canvas = c; colour = shape; }
            public Color Ink() => colour;
            public void Line(float ax, float ay, float bx, float by, float t, Color c) => canvas.Line(ax, ay, bx, by, t, c);
            public void Ring(float cx, float cy, float r, float t, Color c) => canvas.Ring(cx, cy, r, t, c);
            public void RoundRectOutline(float x0, float y0, float x1, float y1, float r, float t, Color c) => canvas.RoundRectOutline(x0, y0, x1, y1, r, t, c);
        }

        // ------------------------------------------------------------ Writing and importing

        private static void Make(string name, int w, int h, Action<Canvas> draw, ImportSpec spec)
        {
            string path = PathOf(name);
            bool existed = File.Exists(path);
            if (!existed)
            {
                var canvas = new Canvas(w, h);
                draw(canvas);
                canvas.Save(path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
            if (!existed || AssetImporter.GetAtPath(path) is TextureImporter { textureType: not TextureImporterType.Sprite })
                Configure(path, spec);
        }

        private static void Configure(string path, ImportSpec spec)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = spec.Ppu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = spec.Tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.spriteBorder = spec.Border;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = spec.Tiled ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = spec.Pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        public static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(PathOf(name));

        // ------------------------------------------------------------ Tiles (all seamless)

        /// <summary>Draws a shape at its position and at the neighbouring tile positions, so it wraps around the edges.</summary>
        private static void Wrapped(Canvas c, Action<float, float> draw)
        {
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                    draw(ox * c.W, oy * c.H);
        }

        private static void DrawFloorTile(Canvas c)
        {
            var rng = new System.Random(11);
            Color baseColor = new Color(1f, 0.97f, 0.9f, 1f);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    float n = 0.93f + (float)rng.NextDouble() * 0.07f;
                    c.Put(x, y, new Color(baseColor.r * n, baseColor.g * n, baseColor.b * n, 1f));
                }
            for (int i = 0; i < 40; i++)   // mottling
                c.Circle((float)rng.NextDouble() * c.W, (float)rng.NextDouble() * c.H, 4f + (float)rng.NextDouble() * 10f, new Color(0.8f, 0.72f, 0.55f, 0.07f));
            var grout = new Color(0.6f, 0.48f, 0.34f, 0.5f);
            c.Rect(0f, 0f, c.W, 3f, grout);
            c.Rect(0f, 0f, 3f, c.H, grout);
            c.Rect(0f, c.H - 2f, c.W, c.H, grout);
            c.Rect(c.W - 2f, 0f, c.W, c.H, grout);
            c.Line(30f, 92f, 52f, 70f, 1.5f, new Color(0.5f, 0.4f, 0.3f, 0.25f));   // a crack
            c.Line(52f, 70f, 60f, 74f, 1.2f, new Color(0.5f, 0.4f, 0.3f, 0.25f));
        }

        private static void DrawStoneTile(Canvas c)
        {
            var rng = new System.Random(23);
            var mortar = new Color(0.32f, 0.27f, 0.24f, 1f);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                    c.Put(x, y, mortar);
            const int rows = 4;
            int rowHeight = c.H / rows;
            for (int row = 0; row < rows; row++)
            {
                int bricks = 2;
                int width = c.W / bricks;
                int shift = row % 2 == 0 ? 0 : width / 2;
                for (int b = -1; b <= bricks; b++)
                {
                    float shade = 0.82f + (float)rng.NextDouble() * 0.18f;
                    float x0 = b * width + shift + 3f, x1 = (b + 1) * width + shift - 3f;
                    float y0 = row * rowHeight + 3f, y1 = (row + 1) * rowHeight - 3f;
                    var brick = new Color(0.86f * shade, 0.8f * shade, 0.72f * shade, 1f);
                    c.RoundRect(x0, y0, x1, y1, 3f, brick);
                    c.Rect(x0 + 2f, y1 - 5f, x1 - 2f, y1 - 2f, new Color(1f, 1f, 1f, 0.12f));   // lit top edge
                }
            }
        }

        private static void DrawCrowdTile(Canvas c)
        {
            var rng = new System.Random(5);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                    c.Put(x, y, new Color(0.16f, 0.12f, 0.16f, 1f));
            Color[] veg =
            {
                new Color(0.85f, 0.2f, 0.2f), new Color(0.95f, 0.55f, 0.15f), new Color(0.45f, 0.75f, 0.3f),
                new Color(0.6f, 0.3f, 0.6f), new Color(0.95f, 0.8f, 0.25f), new Color(0.9f, 0.5f, 0.6f),
            };
            const int cols = 4, rowsCount = 4;
            for (int r = 0; r < rowsCount; r++)
                for (int col = 0; col < cols; col++)
                {
                    float cx = (col + 0.5f) * (c.W / (float)cols) + (r % 2) * 16f + (float)(rng.NextDouble() - 0.5) * 6f;
                    float cy = (r + 0.5f) * (c.H / (float)rowsCount) + (float)(rng.NextDouble() - 0.5) * 4f;
                    Color body = veg[rng.Next(veg.Length)];
                    Wrapped(c, (ox, oy) =>
                    {
                        c.Circle(cx + ox, cy + oy, 12f, body);
                        c.Ring(cx + ox, cy + oy, 12f, 2f, new Color(0.08f, 0.05f, 0.07f, 0.9f));
                        c.Circle(cx + ox - 4f, cy + oy + 2f, 2.2f, Color.white);
                        c.Circle(cx + ox + 4f, cy + oy + 2f, 2.2f, Color.white);
                        c.Circle(cx + ox - 4f, cy + oy + 2f, 1f, Ink);
                        c.Circle(cx + ox + 4f, cy + oy + 2f, 1f, Ink);
                        c.RoundRect(cx + ox - 4f, cy + oy - 6f, cx + ox + 4f, cy + oy - 4f, 1f, new Color(0.15f, 0.05f, 0.05f, 0.85f));
                    });
                }
        }

        private static void DrawCheckerTile(Canvas c)
        {
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                    c.Put(x, y, ((x / 16) + (y / 16)) % 2 == 0 ? new Color(0.25f, 0.18f, 0.14f, 1f) : new Color(0.95f, 0.9f, 0.8f, 1f));
        }

        // ------------------------------------------------------------ Obstacles (pivot at the bottom centre)

        private static void DrawPillar(Canvas c)
        {
            Color stone = new Color(0.78f, 0.74f, 0.66f), light = new Color(0.92f, 0.89f, 0.8f), shade = new Color(0.55f, 0.5f, 0.45f);
            c.Ellipse(32f, 16f, 27f, 14f, shade);                    // base
            c.Rect(8f, 10f, 56f, 22f, shade);
            c.Ellipse(32f, 22f, 27f, 11f, stone);
            c.Rect(14f, 22f, 50f, 128f, stone);                      // shaft
            c.Rect(14f, 22f, 24f, 128f, light);
            c.Rect(42f, 22f, 50f, 128f, shade);
            for (int i = 0; i < 4; i++)
                c.Rect(14f, 40f + i * 24f, 50f, 43f + i * 24f, new Color(0.45f, 0.4f, 0.36f, 0.5f));
            c.Ellipse(32f, 132f, 30f, 12f, stone);                   // capital
            c.Rect(2f, 124f, 62f, 134f, stone);
            c.Ellipse(32f, 134f, 30f, 12f, light);
            c.Ellipse(32f, 140f, 17f, 7f, new Color(0.35f, 0.7f, 0.3f));   // a leaf on top
            c.OutlineBehind(Ink, 2);
        }

        private static void DrawLowWall(Canvas c)
        {
            Color stone = new Color(0.72f, 0.66f, 0.58f);
            c.RoundRect(2f, 4f, 62f, 40f, 4f, stone);
            c.Rect(2f, 32f, 62f, 40f, new Color(0.88f, 0.83f, 0.73f));   // top face
            for (int i = 0; i < 3; i++)
                c.Rect(2f, 12f + i * 8f, 62f, 13f + i * 8f, new Color(0.4f, 0.35f, 0.3f, 0.6f));
            c.Rect(20f, 4f, 21f, 32f, new Color(0.4f, 0.35f, 0.3f, 0.5f));
            c.Rect(44f, 4f, 45f, 32f, new Color(0.4f, 0.35f, 0.3f, 0.5f));
            c.OutlineBehind(Ink, 2);
        }

        private static void DrawCrate(Canvas c)
        {
            Color wood = new Color(0.82f, 0.58f, 0.3f), dark = new Color(0.5f, 0.32f, 0.16f);
            c.RoundRect(4f, 4f, 60f, 56f, 3f, wood);
            c.Rect(4f, 46f, 60f, 56f, new Color(0.92f, 0.7f, 0.42f));    // top face
            c.RoundRectOutline(6f, 6f, 58f, 48f, 2f, 4f, dark);
            c.Line(8f, 8f, 56f, 46f, 4f, dark);
            c.Line(8f, 46f, 56f, 8f, 4f, dark);
            c.OutlineBehind(Ink, 2);
        }

        private static void DrawPumpkin(Canvas c)
        {
            Color orange = new Color(0.98f, 0.55f, 0.12f), dark = new Color(0.75f, 0.35f, 0.05f);
            c.Ellipse(32f, 30f, 29f, 24f, dark);
            c.Ellipse(20f, 30f, 14f, 24f, orange);
            c.Ellipse(44f, 30f, 14f, 24f, orange);
            c.Ellipse(32f, 30f, 12f, 25f, new Color(1f, 0.65f, 0.2f));
            c.Rect(29f, 50f, 36f, 60f, new Color(0.3f, 0.55f, 0.2f));    // stem
            c.Ellipse(38f, 56f, 8f, 3f, new Color(0.35f, 0.65f, 0.25f));
            c.OutlineBehind(Ink, 2);
        }

        private static void DrawCabbage(Canvas c)
        {
            Color outer = new Color(0.4f, 0.7f, 0.3f), mid = new Color(0.55f, 0.82f, 0.4f), core = new Color(0.78f, 0.93f, 0.6f);
            c.Ellipse(32f, 28f, 29f, 24f, outer);
            c.Ellipse(32f, 30f, 23f, 20f, mid);
            c.Ellipse(32f, 33f, 15f, 14f, core);
            c.Line(32f, 20f, 32f, 44f, 2f, new Color(0.35f, 0.6f, 0.3f, 0.8f));
            c.Line(20f, 30f, 44f, 30f, 2f, new Color(0.35f, 0.6f, 0.3f, 0.5f));
            c.OutlineBehind(Ink, 2);
        }

        // ------------------------------------------------------------ HUD

        private static void DrawHeart(Canvas c, bool full)
        {
            // Classic heart curve (x^2 + y^2 - 1)^3 - x^2 y^3 <= 0, 3x3 supersampled.
            Color body = full ? new Color(0.92f, 0.16f, 0.22f) : new Color(0.22f, 0.16f, 0.22f, 0.85f);
            for (int y = 0; y < c.H; y++)
                for (int x = 0; x < c.W; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < 3; sy++)
                        for (int sx = 0; sx < 3; sx++)
                        {
                            float u = (x + (sx + 0.5f) / 3f - 32f) / 25f;
                            float v = (y + (sy + 0.5f) / 3f - 30f) / 25f;
                            float a = u * u + v * v - 1f;
                            if (a * a * a - u * u * v * v * v <= 0f)
                                inside++;
                        }
                    c.Blend(x, y, body, inside / 9f);
                }
            if (full)
                c.Ellipse(20f, 42f, 5f, 3f, new Color(1f, 1f, 1f, 0.55f));    // shine
            c.OutlineBehind(full ? Ink : new Color(0.55f, 0.5f, 0.6f, 0.9f), 2);
        }

        private static void DrawShotgunIcon(Canvas c)
        {
            c.Circle(20f, 44f, 8f, Color.white);
            c.Circle(44f, 44f, 8f, Color.white);
            c.Circle(32f, 24f, 8f, Color.white);
            c.Circle(32f, 46f, 6f, Color.white);
            c.OutlineBehind(Ink, 3);
        }

        private static void DrawGatlingIcon(Canvas c)
        {
            for (int i = 0; i < 4; i++)
                c.RoundRect(8f + i * 13f, 14f, 15f + i * 13f, 50f, 3f, Color.white);
            c.OutlineBehind(Ink, 3);
        }
    }
}
