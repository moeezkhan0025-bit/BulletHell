using BulletHell.Arena;
using BulletHell.Platform;
using BulletHell.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BulletHell.Tests
{
    /// <summary>M7.5: footprints and bullet reach, perspective numbers, button glyphs.</summary>
    public class PerspectiveTests
    {
        private static readonly Rect Floor = new Rect(-8f, -4.5f, 16f, 9f);

        // ---- footprints

        [Test]
        public void AnEllipseFootprintIsWiderThanDeep()
        {
            var grid = new ArenaGrid(Floor, 0.1f);
            grid.AddEllipse(Vector2.zero, new Vector2(1f, 0.4f), 0);

            Assert.IsTrue(grid.CircleBlocked(new Vector2(0.8f, 0f), 0.02f));     // inside along the wide axis
            Assert.IsFalse(grid.CircleBlocked(new Vector2(0f, 0.6f), 0.02f));    // same distance the shallow way is outside
            Assert.IsFalse(grid.CircleBlocked(new Vector2(0.8f, 0.35f), 0.02f)); // a corner outside the ellipse
            Assert.IsTrue(grid.CircleBlocked(new Vector2(0f, 0.3f), 0.02f));
        }

        [Test]
        public void ReachingUpExtendsTheEllipseUpwardsOnly()
        {
            var grid = new ArenaGrid(Floor, 0.1f);
            grid.AddEllipseReachingUp(Vector2.zero, new Vector2(1f, 0.4f), 0.8f, 0);

            Assert.IsTrue(grid.CircleBlocked(new Vector2(0f, 1f), 0.02f));       // the body above the footprint
            Assert.IsTrue(grid.CircleBlocked(new Vector2(0.9f, 0.6f), 0.02f));
            Assert.IsFalse(grid.CircleBlocked(new Vector2(0f, -0.6f), 0.02f));   // nothing below the feet
            Assert.IsFalse(grid.CircleBlocked(new Vector2(0f, 1.5f), 0.02f));    // and not beyond the reach
        }

        private static Obstacle NewObstacle(ObstacleData data, Vector2 footprint, float reach, ArenaGrid move, ArenaGrid bullets)
        {
            var go = new GameObject("test obstacle", typeof(BoxCollider2D), typeof(PolygonCollider2D));
            var art = new GameObject("Art", typeof(SpriteRenderer));
            art.transform.SetParent(go.transform, false);
            var obstacle = go.AddComponent<Obstacle>();
            var so = new UnityEditor.SerializedObject(obstacle);
            so.FindProperty("art").objectReferenceValue = art.GetComponent<SpriteRenderer>();
            so.FindProperty("boxCollider").objectReferenceValue = go.GetComponent<BoxCollider2D>();
            so.FindProperty("ellipseCollider").objectReferenceValue = go.GetComponent<PolygonCollider2D>();
            so.ApplyModifiedPropertiesWithoutUndo();
            obstacle.Setup(data, Vector2.zero, footprint, 0, null, null, reach);
            obstacle.Register(move, bullets);
            return obstacle;
        }

        private static ObstacleData Data(ObstacleShape shape, ObstacleKind kind = ObstacleKind.Solid)
        {
            var data = ScriptableObject.CreateInstance<ObstacleData>();
            data.Configure(kind, shape, Vector2.one, Color.white, 10f);
            return data;
        }

        [Test]
        public void WalkersUseTheFootprintAndBulletsAlsoTheBodyAboveIt()
        {
            var move = new ArenaGrid(Floor, 0.1f);
            var bullets = new ArenaGrid(Floor, 0.1f);
            Obstacle pillar = NewObstacle(Data(ObstacleShape.Circle), new Vector2(1f, 0.5f), 0.6f, move, bullets);
            try
            {
                var aboveFootprint = new Vector2(0f, 0.7f);
                Assert.IsFalse(move.CircleBlocked(aboveFootprint, 0.05f), "walking behind the base is free");
                Assert.IsTrue(bullets.CircleBlocked(aboveFootprint, 0.05f), "a bullet at body height is stopped");
                Assert.IsTrue(move.CircleBlocked(Vector2.zero, 0.05f));
                Assert.IsTrue(bullets.CircleBlocked(Vector2.zero, 0.05f));
                Assert.IsFalse(bullets.CircleBlocked(new Vector2(0f, -0.6f), 0.05f), "nothing in front of the feet");
            }
            finally
            {
                Object.DestroyImmediate(pillar.gameObject);
            }
        }

        [Test]
        public void ABreakableClearsBothGridsWhenItBreaksAndComesBackNextRound()
        {
            var move = new ArenaGrid(Floor, 0.1f);
            var bullets = new ArenaGrid(Floor, 0.1f);
            Obstacle crate = NewObstacle(Data(ObstacleShape.Box, ObstacleKind.Breakable), new Vector2(1f, 0.6f), 0.5f, move, bullets);
            try
            {
                crate.TakeDamage(999f);
                Assert.IsTrue(crate.IsBroken);
                Assert.IsFalse(move.CircleBlocked(Vector2.zero, 0.05f));
                Assert.IsFalse(bullets.CircleBlocked(new Vector2(0f, 0.6f), 0.05f));

                move.Clear();
                bullets.Clear();
                crate.Restore();
                crate.Register(move, bullets);
                Assert.IsTrue(move.CircleBlocked(Vector2.zero, 0.05f));
                Assert.IsTrue(bullets.CircleBlocked(new Vector2(0f, 0.6f), 0.05f));
            }
            finally
            {
                Object.DestroyImmediate(crate.gameObject);
            }
        }

        [Test]
        public void TheBulletColliderCoversTheFootprintAndTheReach()
        {
            var move = new ArenaGrid(Floor, 0.1f);
            Obstacle box = NewObstacle(Data(ObstacleShape.Box), new Vector2(1f, 0.5f), 0.4f, move, null);
            Obstacle ellipse = NewObstacle(Data(ObstacleShape.Circle), new Vector2(1f, 0.5f), 0.4f, move, null);
            try
            {
                var boxCollider = box.GetComponent<BoxCollider2D>();
                Assert.AreEqual(new Vector2(1f, 0.9f), boxCollider.size);
                Assert.AreEqual(0.2f, boxCollider.offset.y, 0.0001f);

                var polygon = ellipse.GetComponent<PolygonCollider2D>();
                float min = float.MaxValue, max = float.MinValue;
                foreach (Vector2 point in polygon.GetPath(0))
                {
                    min = Mathf.Min(min, point.y);
                    max = Mathf.Max(max, point.y);
                }
                Assert.AreEqual(-0.25f, min, 0.001f);      // the lower edge of the footprint
                Assert.AreEqual(0.65f, max, 0.001f);       // the upper edge plus the reach
                Assert.IsTrue(polygon.enabled);
                Assert.IsFalse(ellipse.GetComponent<BoxCollider2D>().enabled);
            }
            finally
            {
                Object.DestroyImmediate(box.gameObject);
                Object.DestroyImmediate(ellipse.gameObject);
            }
        }

        [Test]
        public void ArtWidthFollowsTheFootprintAndExtraDepthAddsHeight()
        {
            ObstacleData data = Data(ObstacleShape.Box);   // footprint 1 x 1 by Configure
            data.ConfigureArt(null, new Vector2(1f, 2f));
            Assert.AreEqual(new Vector2(1f, 2f), data.ArtSizeFor(Vector2.one));
            Assert.AreEqual(new Vector2(2.6f, 1.5f), data.ArtSizeFor(new Vector2(2.6f, 0.5f)));   // wall: wider, shallower
            Assert.AreEqual(new Vector2(0.5f, 2.6f), data.ArtSizeFor(new Vector2(0.5f, 1.6f)));   // wall standing along Y
        }

        // ---- tuning

        [Test]
        public void EnemyFootprintAndHurtboxFollowTheSpriteSize()
        {
            var tuning = ScriptableObject.CreateInstance<PerspectiveTuning>();
            Assert.Greater(tuning.EnemyFootprintRadiusFor(1f), 0f);
            Assert.Less(tuning.EnemyFootprintRadiusFor(1f), 0.5f, "the footprint is smaller than the sprite");
            Assert.AreEqual(tuning.EnemyFootprintRadiusFor(1f) * 2f, tuning.EnemyFootprintRadiusFor(2f), 0.0001f);
            Vector2 hurtbox = tuning.EnemyHurtboxSizeFor(2f);
            Assert.AreEqual(tuning.EnemyHurtboxSizeFor(1f).x * 2f, hurtbox.x, 0.0001f);
            Assert.Greater(tuning.BulletHeadroom, 0f);
        }

        // ---- glyphs

        private static ButtonGlyphLibrary.FamilySet Set(GlyphFamily family, string south)
        {
            return new ButtonGlyphLibrary.FamilySet
            {
                Family = family,
                South = new ButtonGlyph { Label = south },
                East = new ButtonGlyph { Label = "east" },
                West = new ButtonGlyph { Label = "west" },
                North = new ButtonGlyph { Label = "north" },
            };
        }

        [Test]
        public void TheGlyphLibraryReturnsTheSetOfTheFamilyAndFallsBackToTheFirst()
        {
            var library = ScriptableObject.CreateInstance<ButtonGlyphLibrary>();
            library.SetSets(new[] { Set(GlyphFamily.PlayStation, "cross"), Set(GlyphFamily.Xbox, "a") });

            Assert.AreEqual("a", library.Get(GlyphFamily.Xbox, GlyphButton.South).Label);
            Assert.AreEqual("cross", library.Get(GlyphFamily.PlayStation, GlyphButton.South).Label);
            Assert.AreEqual("north", library.Get(GlyphFamily.Xbox, GlyphButton.North).Label);
            Assert.AreEqual("cross", library.Get(GlyphFamily.Nintendo, GlyphButton.South).Label, "unknown family: first set");
        }

        [Test]
        public void EachAmmoSlotMapsToItsFaceButton()
        {
            Assert.AreEqual(GlyphButton.South, ButtonGlyphLibrary.ForAmmoSlot(0));
            Assert.AreEqual(GlyphButton.East, ButtonGlyphLibrary.ForAmmoSlot(1));
            Assert.AreEqual(GlyphButton.West, ButtonGlyphLibrary.ForAmmoSlot(2));
            Assert.AreEqual(GlyphButton.North, ButtonGlyphLibrary.ForAmmoSlot(3));
        }

        [TestCase("DualShockGamepad", GlyphFamily.PlayStation)]
        [TestCase("XInputController", GlyphFamily.Xbox)]
        [TestCase("SwitchProControllerHID", GlyphFamily.Nintendo)]
        public void TheControllerLayoutPicksTheGlyphFamily(string layout, GlyphFamily expected)
        {
            InputDevice device = InputSystem.AddDevice(layout);
            try
            {
                Assert.AreEqual(expected, GlyphFamilyDetector.Of(device));
            }
            finally
            {
                InputSystem.RemoveDevice(device);
            }
        }
    }
}
