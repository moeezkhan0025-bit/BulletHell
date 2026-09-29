using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class ArenaTests
    {
        private static readonly Rect Bounds = new Rect(-8f, -4.5f, 16f, 9f);

        private static ArenaGrid Grid() => new ArenaGrid(Bounds, 0.25f);

        // ---- grid

        [Test]
        public void EmptyGridIsFreeInsideAndBlockedAtTheWalls()
        {
            ArenaGrid grid = Grid();
            Assert.IsFalse(grid.CircleBlocked(Vector2.zero, 0.5f));
            Assert.IsFalse(grid.CircleBlocked(new Vector2(7.5f, 4f), 0.5f));   // touching the wall is fine
            Assert.IsTrue(grid.CircleBlocked(new Vector2(7.6f, 0f), 0.5f, out int owner));
            Assert.AreEqual(ArenaGrid.Border, owner);
            Assert.IsTrue(grid.CircleBlocked(new Vector2(0f, -4.2f), 0.5f));
        }

        [Test]
        public void ABoxBlocksExactlyWhereItIs()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(new Vector2(2f, 1f), new Vector2(2f, 1f), 3);

            Assert.IsTrue(grid.CircleBlocked(new Vector2(2f, 1f), 0.1f, out int owner));
            Assert.AreEqual(3, owner);
            Assert.IsTrue(grid.CircleBlocked(new Vector2(3.2f, 1f), 0.3f));    // overlaps the right edge (x = 3)
            Assert.IsFalse(grid.CircleBlocked(new Vector2(3.5f, 1f), 0.3f));   // clear of it
            Assert.IsFalse(grid.CircleBlocked(new Vector2(2f, 2.0f), 0.3f));
        }

        [Test]
        public void ACircleObstacleLeavesItsCornersFree()
        {
            ArenaGrid grid = Grid();
            grid.AddCircle(Vector2.zero, 1f, 0);

            Assert.IsTrue(grid.CircleBlocked(new Vector2(0.9f, 0f), 0.05f));
            Assert.IsFalse(grid.CircleBlocked(new Vector2(1.05f, 1.05f), 0.1f));   // inside the bounding square, outside the circle
        }

        [Test]
        public void ClearingAnOwnerFreesItsCells_OthersStay()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(new Vector2(-3f, 0f), Vector2.one, 0);
            grid.AddBox(new Vector2(3f, 0f), Vector2.one, 1);

            grid.ClearOwner(0);

            Assert.IsFalse(grid.CircleBlocked(new Vector2(-3f, 0f), 0.2f));
            Assert.IsTrue(grid.CircleBlocked(new Vector2(3f, 0f), 0.2f));
        }

        [Test]
        public void ASegmentIsBlockedByWhatLiesAlongIt_AndReportsTheOwner()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(new Vector2(0f, 0f), new Vector2(0.5f, 3f), 7);

            Assert.IsTrue(grid.SegmentBlocked(new Vector2(-2f, 0f), new Vector2(2f, 0f), 0.1f, out int owner));
            Assert.AreEqual(7, owner);
            Assert.IsFalse(grid.SegmentBlocked(new Vector2(-2f, 2f), new Vector2(2f, 2f), 0.1f, out _));   // passes above it
        }

        [Test]
        public void AFastStepCannotTunnelThroughAThinWall()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(Vector2.zero, new Vector2(0.25f, 4f), 0);
            Assert.IsTrue(grid.SegmentBlocked(new Vector2(-3f, 0f), new Vector2(3f, 0f), 0.05f, out _));
        }

        [Test]
        public void MovingIntoAWallStopsAtItsSurfaceAndSlidesAlongIt()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(new Vector2(2f, 0f), new Vector2(1f, 6f), 0);   // a wall from x = 1.5 to 2.5

            Vector2 stopped = grid.Move(new Vector2(0f, 0f), new Vector2(3f, 0f), 0.35f);
            Assert.That(stopped.x, Is.InRange(1.1f, 1.16f));            // rests against the surface (1.5 - 0.35)
            Assert.AreEqual(0f, stopped.y, 0.0001f);

            Vector2 slid = grid.Move(new Vector2(1.14f, 0f), new Vector2(0.5f, 1f), 0.35f);
            Assert.AreEqual(1f, slid.y, 0.0001f);                        // Y still moved
            Assert.LessOrEqual(slid.x, 1.16f);                           // X did not push through
        }

        [Test]
        public void MovingNeverEndsInsideAnObstacleOrOutsideTheArena()
        {
            ArenaGrid grid = Grid();
            grid.AddCircle(new Vector2(0f, 0f), 1f, 0);
            var random = new System.Random(4);
            Vector2 position = new Vector2(-6f, 3f);
            for (int i = 0; i < 2000; i++)
            {
                var delta = new Vector2((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * 0.4f;
                position = grid.Move(position, delta, 0.35f);
                Assert.IsFalse(grid.CircleBlocked(position, 0.35f - 0.001f), $"step {i} at {position}");
            }
        }

        [Test]
        public void NearestFreeMovesAnObjectOutOfAnObstacle_AndLeavesFreeSpotsAlone()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(Vector2.zero, new Vector2(2f, 2f), 0);

            Assert.AreEqual(new Vector2(3f, 3f), grid.NearestFree(new Vector2(3f, 3f), 0.3f));
            Vector2 moved = grid.NearestFree(new Vector2(0.2f, 0.1f), 0.3f);
            Assert.IsFalse(grid.CircleBlocked(moved, 0.3f));
            Assert.Less(Vector2.Distance(moved, new Vector2(0.2f, 0.1f)), 1.6f);
        }

        // ---- traps

        [Test]
        public void ATrapDoesNothingUntilStarted_ThenWaitsOutItsStartDelay()
        {
            var timer = new TrapTimer(1f, 1f, 0.5f, 2f, 0f);
            Assert.AreEqual(0, timer.Tick(5f));                  // idle, not started
            Assert.AreEqual(TrapPhase.Idle, timer.Phase);

            timer.Start();
            timer.Tick(0.9f);
            Assert.AreEqual(TrapPhase.Idle, timer.Phase);        // still in the start delay
            timer.Tick(0.2f);
            Assert.AreEqual(TrapPhase.Telegraph, timer.Phase);   // a warning always comes before a strike
        }

        [Test]
        public void ATrapCyclesTelegraphActiveCooldownAndStrikesOncePerActivation()
        {
            var timer = new TrapTimer(0f, 1f, 0.5f, 2f, 0f);
            timer.Start();
            int strikes = 0;
            var phases = new System.Collections.Generic.List<TrapPhase>();
            for (int i = 0; i < 700; i++)   // 7 seconds at 0.01 s
            {
                strikes += timer.Tick(0.01f);
                if (phases.Count == 0 || phases[phases.Count - 1] != timer.Phase)
                    phases.Add(timer.Phase);
            }

            // Telegraph 1s, Active 0.5s, Cooldown 2s, then again: two activations in 7 s (starts at 1.0 and 4.5).
            Assert.AreEqual(2, strikes);
            CollectionAssert.AreEqual(new[]
            {
                TrapPhase.Telegraph, TrapPhase.Active, TrapPhase.Cooldown, TrapPhase.Telegraph, TrapPhase.Active, TrapPhase.Cooldown,
            }, phases.GetRange(phases[0] == TrapPhase.Idle ? 1 : 0, 6));
        }

        [Test]
        public void AZoneStrikesAgainEveryIntervalWhileActive()
        {
            var timer = new TrapTimer(0f, 0.5f, 2f, 1f, 0.5f);
            timer.Start();
            int strikes = 0;
            for (int i = 0; i < 250; i++)   // 2.5 s: telegraph 0.5, then 2 s of active
                strikes += timer.Tick(0.01f);
            Assert.AreEqual(4, strikes);    // at 0, 0.5, 1.0, 1.5 of the active time (not again at exactly the end)
        }

        [Test]
        public void ALargeStepStillCountsEveryStrikeAndPhase()
        {
            var timer = new TrapTimer(0f, 0.2f, 1f, 0.2f, 0.25f);
            timer.Start();
            int strikes = timer.Tick(1.3f);   // through the whole telegraph and active phase and into cooldown
            Assert.AreEqual(4, strikes);
            Assert.AreEqual(TrapPhase.Cooldown, timer.Phase);
        }

        [Test]
        public void ResetStopsATrapAtOnce()
        {
            var timer = new TrapTimer(0f, 0.1f, 1f, 1f, 0f);
            timer.Start();
            timer.Tick(0.2f);
            Assert.AreEqual(TrapPhase.Active, timer.Phase);
            timer.Reset();
            Assert.AreEqual(TrapPhase.Idle, timer.Phase);
            Assert.AreEqual(0, timer.Tick(5f));
        }

        [Test]
        public void TrapShapesTestCirclesAndRotatedBoxes()
        {
            Assert.IsTrue(TrapShape.CircleOverlapsCircle(Vector2.zero, 1f, new Vector2(1.1f, 0f), 0.2f));
            Assert.IsFalse(TrapShape.CircleOverlapsCircle(Vector2.zero, 1f, new Vector2(1.3f, 0f), 0.2f));

            // A 4 x 0.5 bar lying along X, then turned 90 degrees so it stands along Y.
            var size = new Vector2(4f, 0.5f);
            Assert.IsTrue(TrapShape.CircleOverlapsBox(Vector2.zero, size, 0f, new Vector2(1.9f, 0f), 0.05f));
            Assert.IsFalse(TrapShape.CircleOverlapsBox(Vector2.zero, size, 0f, new Vector2(0f, 1.9f), 0.05f));
            Assert.IsTrue(TrapShape.CircleOverlapsBox(Vector2.zero, size, 90f, new Vector2(0f, 1.9f), 0.05f));
            Assert.IsFalse(TrapShape.CircleOverlapsBox(Vector2.zero, size, 90f, new Vector2(1.9f, 0f), 0.05f));
        }

        // ---- obstacles

        [Test]
        public void DamageStagesWorsenAsHealthDrops()
        {
            Assert.AreEqual(0, ObstacleHealthStages.Stage(1f, 3));
            Assert.AreEqual(0, ObstacleHealthStages.Stage(0.9f, 3));
            Assert.AreEqual(1, ObstacleHealthStages.Stage(0.6f, 3));
            Assert.AreEqual(2, ObstacleHealthStages.Stage(0.3f, 3));
            Assert.AreEqual(2, ObstacleHealthStages.Stage(0.01f, 3));
            Assert.AreEqual(0, ObstacleHealthStages.Stage(0.2f, 1));
        }

        private static ObstacleData BreakableData(float health)
        {
            var data = ScriptableObject.CreateInstance<ObstacleData>();
            data.Configure(ObstacleKind.Breakable, ObstacleShape.Box, Vector2.one, Color.white, health);
            return data;
        }

        private static Obstacle NewObstacle(ObstacleData data, ArenaGrid grid, int id)
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
            obstacle.Setup(data, Vector2.zero, Vector2.one, id, null, null);
            obstacle.Register(grid);
            return obstacle;
        }

        [Test]
        public void ABreakableTakesDamageInStagesThenBreaksIntoDebrisThatNoLongerBlocks()
        {
            ArenaGrid grid = Grid();
            Obstacle crate = NewObstacle(BreakableData(30f), grid, 0);
            int broken = 0;
            crate.Broken += _ => broken++;
            try
            {
                Assert.IsTrue(crate.IsAlive);
                Assert.IsTrue(grid.CircleBlocked(Vector2.zero, 0.2f));

                crate.TakeDamage(12f);   // 60% left
                Assert.AreEqual(1, crate.Stage);
                crate.TakeDamage(10f);   // 27% left
                Assert.AreEqual(2, crate.Stage);
                Assert.IsTrue(grid.CircleBlocked(Vector2.zero, 0.2f));

                crate.TakeDamage(50f);
                Assert.IsTrue(crate.IsBroken);
                Assert.IsFalse(crate.IsAlive);
                Assert.AreEqual(1, broken);
                Assert.IsFalse(grid.CircleBlocked(Vector2.zero, 0.2f));   // debris does not block

                crate.TakeDamage(50f);
                Assert.AreEqual(1, broken);                                // and cannot break twice

                grid.Clear();
                crate.Restore();
                crate.Register(grid);
                Assert.IsFalse(crate.IsBroken);
                Assert.IsTrue(grid.CircleBlocked(Vector2.zero, 0.2f));     // a new round: it is back
            }
            finally
            {
                Object.DestroyImmediate(crate.gameObject);
            }
        }

        [Test]
        public void ASolidObstacleIsNeverDamagedAndIsNotAliveSoBulletsJustStopOnIt()
        {
            var data = ScriptableObject.CreateInstance<ObstacleData>();
            data.Configure(ObstacleKind.Solid, ObstacleShape.Box, Vector2.one, Color.white, 1f);
            Obstacle pillar = NewObstacle(data, Grid(), 0);
            try
            {
                pillar.TakeDamage(9999f);
                Assert.IsFalse(pillar.IsAlive);
                Assert.IsFalse(pillar.IsBroken);
            }
            finally
            {
                Object.DestroyImmediate(pillar.gameObject);
            }
        }

        // ---- spawning and rounds

        [Test]
        public void GatesAreUsedOneAfterAnotherAndWrapAround()
        {
            var gates = new[] { new Vector2(-4f, 3f), new Vector2(0f, 3f), new Vector2(4f, 3f) };
            Assert.AreEqual(gates[0], SpawnPlacement.GatePosition(0, gates, 0f));
            Assert.AreEqual(gates[2], SpawnPlacement.GatePosition(2, gates, 0f));
            Assert.AreEqual(gates[0], SpawnPlacement.GatePosition(3, gates, 0f));
            Vector2 nudged = SpawnPlacement.GatePosition(1, gates, 0.3f);
            Assert.LessOrEqual(Vector2.Distance(nudged, gates[1]), 0.3001f);
        }

        [Test]
        public void ASpawnInsideAnObstacleIsMovedOut()
        {
            ArenaGrid grid = Grid();
            grid.AddCircle(new Vector2(0f, 3.7f), 1.2f, 0);
            Vector2 gate = SpawnPlacement.GatePosition(0, new[] { new Vector2(0f, 3.7f) }, 0f);
            Vector2 spot = grid.NearestFree(gate, 0.5f);
            Assert.IsFalse(grid.CircleBlocked(spot, 0.5f));
        }

        [Test]
        public void ARoundUsesItsOwnArenaElseTheDefault()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var fallback = ScriptableObject.CreateInstance<ArenaData>();
            var special = ScriptableObject.CreateInstance<ArenaData>();
            config.SetDefaultArena(fallback);

            var plain = ScriptableObject.CreateInstance<RoundData>();
            var themed = ScriptableObject.CreateInstance<RoundData>();
            themed.SetArena(special);
            config.SetRounds(new[] { plain, themed }, 1);

            Assert.AreSame(fallback, config.GetArena(1));
            Assert.AreSame(special, config.GetArena(2));
            Assert.AreSame(fallback, config.GetArena(3));   // rounds past the end loop back to round 1
        }
    }
}
