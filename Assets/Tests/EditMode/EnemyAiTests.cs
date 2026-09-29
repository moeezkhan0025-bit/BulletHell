using BulletHell.AI;
using BulletHell.Arena;
using BulletHell.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>M8: flow field, steering limits, grid ray, the move speed difficulty axis.</summary>
    public class EnemyAiTests
    {
        private static readonly Rect Bounds = new Rect(-8f, -4.5f, 16f, 9f);
        private const float NavRadius = 0.3f;

        private static ArenaGrid Grid() => new ArenaGrid(Bounds, 0.25f);

        // Walks a position along the field the way an enemy would (fixed steps), returns steps taken or -1.
        private static int Walk(FlowField field, ArenaGrid grid, Vector2 start, Vector2 target, float stepLength, int maxSteps, float bodyRadius)
        {
            Vector2 position = start;
            for (int i = 0; i < maxSteps; i++)
            {
                if (Vector2.Distance(position, target) < 0.4f)
                    return i;
                Vector2 direction = field.Direction(position);
                if (direction == Vector2.zero)
                    return -1;
                position = grid.Move(position, direction * stepLength, bodyRadius);
            }
            return -1;
        }

        // ---- flow field

        [Test]
        public void InOpenSpaceTheFieldPointsTowardsTheTarget()
        {
            ArenaGrid grid = Grid();
            var field = new FlowField(grid, NavRadius);
            field.Build(new Vector2(3f, 0f));

            Vector2 direction = field.Direction(new Vector2(-3f, 0f));
            Assert.Greater(Vector2.Dot(direction, Vector2.right), 0.95f);
            Vector2 diagonal = field.Direction(new Vector2(-3f, -3f));
            Assert.Greater(Vector2.Dot(diagonal, new Vector2(1f, 1f).normalized), 0.7f);
        }

        [Test]
        public void TheFieldRoutesAroundAWall()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(new Vector2(0f, 0f), new Vector2(0.5f, 6f), 0);    // wall from y=-3 to y=3
            var field = new FlowField(grid, NavRadius);
            Vector2 target = new Vector2(3f, 0f);
            field.Build(target);

            Vector2 start = new Vector2(-3f, 0f);
            Assert.IsTrue(field.IsReachable(start));
            int steps = Walk(field, grid, start, target, 0.2f, 200, 0.2f);
            Assert.GreaterOrEqual(steps, 0, "an enemy following the field must arrive");
            Assert.Greater(steps, 40, "it has to go around the wall, not through it");
        }

        [Test]
        public void ATargetWalledInIsUnreachable()
        {
            ArenaGrid grid = Grid();
            // A closed box of walls around the target.
            grid.AddBox(new Vector2(3f, 1.5f), new Vector2(3f, 0.5f), 0);
            grid.AddBox(new Vector2(3f, -1.5f), new Vector2(3f, 0.5f), 1);
            grid.AddBox(new Vector2(1.5f, 0f), new Vector2(0.5f, 3f), 2);
            grid.AddBox(new Vector2(4.5f, 0f), new Vector2(0.5f, 3f), 3);
            var field = new FlowField(grid, NavRadius);
            field.Build(new Vector2(3f, 0f));

            Assert.IsFalse(field.IsReachable(new Vector2(-3f, 0f)));
            Assert.AreEqual(Vector2.zero, field.Direction(new Vector2(-3f, 0f)));
        }

        [Test]
        public void BreakingTheWallOpensTheDirectRoute()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(new Vector2(0f, 0f), new Vector2(0.5f, 6f), 0);
            var field = new FlowField(grid, NavRadius);
            Vector2 target = new Vector2(3f, 0f);
            field.Build(target);
            Vector2 before = field.Direction(new Vector2(-1f, 0f));
            Assert.Less(Vector2.Dot(before, Vector2.right), 0.9f, "blocked by the wall: not heading straight for the target");

            grid.ClearOwner(0);          // a breakable broke
            field.RebuildMask(NavRadius);
            field.Build(target);

            Assert.Greater(Vector2.Dot(field.Direction(new Vector2(-1f, 0f)), Vector2.right), 0.95f);
        }

        [Test]
        public void CostFallsAlongTheFlow()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(new Vector2(0f, 0f), new Vector2(0.5f, 6f), 0);
            var field = new FlowField(grid, NavRadius);
            field.Build(new Vector2(3f, 0f));

            grid.WorldToCell(new Vector2(-3f, 1f), out int c, out int r);
            for (int i = 0; i < 300 && field.CostAt(c, r) > 0f; i++)
            {
                float here = field.CostAt(c, r);
                Vector2 flow = field.FlowAt(c, r);
                Assert.AreNotEqual(Vector2.zero, flow);
                c += Mathf.RoundToInt(flow.x);
                r += Mathf.RoundToInt(flow.y);
                Assert.Less(field.CostAt(c, r), here);
            }
            Assert.AreEqual(0f, field.CostAt(c, r));
        }

        [Test]
        public void ACellInsideAnObstacleMarginStillPointsOut()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(new Vector2(0f, 0f), new Vector2(1f, 1f), 0);
            var field = new FlowField(grid, NavRadius);
            field.Build(new Vector2(4f, 0f));

            // Just beside the box: inside the safety margin, not walkable, but it has a way out.
            Vector2 position = new Vector2(-0.7f, 0f);
            grid.WorldToCell(position, out int c, out int r);
            Assert.IsFalse(field.IsWalkable(c, r));
            Assert.AreNotEqual(Vector2.zero, field.Direction(position));
        }

        // ---- grid helpers

        [Test]
        public void RayDistanceStopsAtAnObstacleAndReturnsMaxInTheOpen()
        {
            ArenaGrid grid = Grid();
            grid.AddBox(new Vector2(2f, 0f), new Vector2(0.5f, 1f), 0);

            float hit = grid.RayDistance(Vector2.zero, Vector2.right, 10f, 0.1f);
            Assert.AreEqual(1.65f, hit, 0.3f);
            Assert.AreEqual(3f, grid.RayDistance(Vector2.zero, Vector2.up, 3f, 0.1f), 0.001f);
        }

        [Test]
        public void CellCenterAndWorldToCellRoundTrip()
        {
            ArenaGrid grid = Grid();
            Vector2 center = grid.CellCenter(10, 7);
            grid.WorldToCell(center, out int c, out int r);
            Assert.AreEqual(10, c);
            Assert.AreEqual(7, r);
        }

        // ---- steering

        private static Vector2 Steer(Vector2 velocity, Vector2 desired, float dt, float max = 4f, float accel = 10f,
                                     float brake = 20f, float turn = 360f) =>
            Steering.Steer(velocity, desired, max, accel, brake, turn, 0.35f, 0.15f, dt);

        [Test]
        public void SteeringAcceleratesGraduallyUpToTopSpeed()
        {
            Vector2 velocity = Steer(Vector2.zero, Vector2.right, 0.1f);
            Assert.AreEqual(1f, velocity.magnitude, 0.001f);   // 10 per second^2 x 0.1s

            for (int i = 0; i < 100; i++)
                velocity = Steer(velocity, Vector2.right, 0.1f);
            Assert.AreEqual(4f, velocity.magnitude, 0.001f);
        }

        [Test]
        public void SteeringLimitsTheTurnRate()
        {
            var velocity = new Vector2(4f, 0f);
            Vector2 next = Steer(velocity, Vector2.up, 0.1f, turn: 90f);   // 9 degrees allowed this step
            float angle = Vector2.Angle(Vector2.right, next);
            Assert.AreEqual(9f, angle, 0.5f);
        }

        [Test]
        public void SteeringBrakesToAStop()
        {
            var velocity = new Vector2(4f, 0f);
            Vector2 next = Steer(velocity, Vector2.zero, 0.1f);
            Assert.AreEqual(2f, next.magnitude, 0.001f);   // 20 per second^2 x 0.1s
            for (int i = 0; i < 10; i++)
                next = Steer(next, Vector2.zero, 0.1f);
            Assert.AreEqual(0f, next.magnitude, 0.0001f);
        }

        [Test]
        public void FacingAwayFromTheGoalSlowsAnEnemyDown()
        {
            var velocity = new Vector2(4f, 0f);
            Vector2 turning = Steer(velocity, Vector2.left, 0.1f, turn: 90f);   // still facing mostly the wrong way
            Assert.Less(turning.magnitude, 4f);
        }

        [Test]
        public void ThrottleScalesTheTopSpeed()
        {
            Vector2 velocity = Vector2.zero;
            for (int i = 0; i < 100; i++)
                velocity = Steer(velocity, Vector2.right * 0.5f, 0.1f);
            Assert.AreEqual(2f, velocity.magnitude, 0.001f);
        }

        [Test]
        public void SeparationPushesApartOnlyInsideReach()
        {
            Assert.AreEqual(Vector2.zero, Steering.PushAway(Vector2.zero, new Vector2(2f, 0f), 1f));
            Vector2 push = Steering.PushAway(Vector2.zero, new Vector2(0.5f, 0f), 1f);
            Assert.Less(push.x, 0f);
            Assert.AreEqual(0f, push.y, 0.0001f);
            Assert.AreEqual(0.5f, push.magnitude, 0.001f);
            Assert.Greater(Steering.PushAway(Vector2.zero, Vector2.zero, 1f).magnitude, 0f);
        }

        // ---- difficulty

        [Test]
        public void MoveSpeedMultiplierDefaultsToOneWithoutAnAxis()
        {
            var curve = ScriptableObject.CreateInstance<DifficultyCurve>();
            Assert.AreEqual(1f, curve.Evaluate(3).MoveSpeedMultiplier, 0.0001f);
            Object.DestroyImmediate(curve);
        }

        [Test]
        public void RoundDifficultyKeepsTheOldFourArgumentConstructor()
        {
            var difficulty = new RoundDifficulty(1f, 2f, 3f, 4f);
            Assert.AreEqual(1f, difficulty.MoveSpeedMultiplier);
            Assert.AreEqual(4f, difficulty.BulletSpeedMultiplier);
        }
    }
}
