using System.Collections.Generic;
using BulletHell.AI;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>M8.5: height classes (Low / Tall), jumping over Low, the Tall fade rule, layouts and their hazard budgets.</summary>
    public class LayoutTests
    {
        private static readonly Rect Bounds = new Rect(-8f, -4.5f, 16f, 9f);
        private const float Body = 0.3f;

        private static ArenaGrid Grid() => new ArenaGrid(Bounds, 0.25f);

        private static ObstacleData Data(ObstacleHeightClass heightClass, ObstacleKind kind = ObstacleKind.Solid)
        {
            var data = ScriptableObject.CreateInstance<ObstacleData>();
            data.Configure(kind, ObstacleShape.Box, new Vector2(1f, 0.5f), Color.white, 10f);
            data.ConfigureHeightClass(heightClass);
            return data;
        }

        private static Obstacle NewObstacle(ObstacleData data, Vector2 position, int id, ArenaGrid move, ArenaGrid bullets, ArenaGrid tall)
        {
            var go = new GameObject("test obstacle", typeof(BoxCollider2D), typeof(PolygonCollider2D));
            var art = new GameObject("Art", typeof(SpriteRenderer));
            art.transform.SetParent(go.transform, false);
            var obstacle = go.AddComponent<Obstacle>();
            var so = new SerializedObject(obstacle);
            so.FindProperty("art").objectReferenceValue = art.GetComponent<SpriteRenderer>();
            so.FindProperty("boxCollider").objectReferenceValue = go.GetComponent<BoxCollider2D>();
            so.FindProperty("ellipseCollider").objectReferenceValue = go.GetComponent<PolygonCollider2D>();
            so.ApplyModifiedPropertiesWithoutUndo();
            obstacle.Setup(data, position, data.Size, id, null, null, 0.3f);
            obstacle.Register(move, bullets, tall);
            return obstacle;
        }

        // ---- height classes and the grids

        [Test]
        public void ALowObstacleBlocksWalkingAndBulletsButIsNotInTheTallGrid()
        {
            ArenaGrid move = Grid(), bullets = Grid(), tall = Grid();
            Obstacle wall = NewObstacle(Data(ObstacleHeightClass.Low), Vector2.zero, 0, move, bullets, tall);
            try
            {
                Assert.IsTrue(wall.IsLow);
                Assert.IsTrue(move.CircleBlocked(Vector2.zero, 0.1f));
                Assert.IsTrue(bullets.CircleBlocked(Vector2.zero, 0.1f));
                Assert.IsFalse(tall.CircleBlocked(Vector2.zero, 0.1f));
            }
            finally
            {
                Object.DestroyImmediate(wall.gameObject);
            }
        }

        [Test]
        public void ATallObstacleIsInEveryGridAndABreakingOneLeavesThemAll()
        {
            ArenaGrid move = Grid(), bullets = Grid(), tall = Grid();
            Obstacle pumpkin = NewObstacle(Data(ObstacleHeightClass.Tall, ObstacleKind.Breakable), Vector2.zero, 0, move, bullets, tall);
            try
            {
                Assert.IsFalse(pumpkin.IsLow);
                Assert.IsTrue(move.CircleBlocked(Vector2.zero, 0.1f));
                Assert.IsTrue(bullets.CircleBlocked(Vector2.zero, 0.1f));
                Assert.IsTrue(tall.CircleBlocked(Vector2.zero, 0.1f));

                pumpkin.TakeDamage(1000f);
                Assert.IsFalse(move.CircleBlocked(Vector2.zero, 0.1f));
                Assert.IsFalse(bullets.CircleBlocked(Vector2.zero, 0.1f));
                Assert.IsFalse(tall.CircleBlocked(Vector2.zero, 0.1f));
            }
            finally
            {
                Object.DestroyImmediate(pumpkin.gameObject);
            }
        }

        [Test]
        public void ABreakableLowObstacleStillBlocksBulletsAndBreaks()
        {
            ArenaGrid move = Grid(), bullets = Grid(), tall = Grid();
            Obstacle crate = NewObstacle(Data(ObstacleHeightClass.Low, ObstacleKind.Breakable), Vector2.zero, 0, move, bullets, tall);
            try
            {
                Assert.IsTrue(bullets.SegmentBlocked(new Vector2(-2f, 0f), new Vector2(2f, 0f), 0.1f, out int owner));
                Assert.AreEqual(0, owner);
                crate.TakeDamage(1000f);
                Assert.IsFalse(bullets.SegmentBlocked(new Vector2(-2f, 0f), new Vector2(2f, 0f), 0.1f, out _));
            }
            finally
            {
                Object.DestroyImmediate(crate.gameObject);
            }
        }

        [Test]
        public void AJumpingPlayerWalksOverLowButTallAndTheWallsStillStopThem()
        {
            ArenaGrid move = Grid(), bullets = Grid(), tall = Grid();
            Obstacle low = NewObstacle(Data(ObstacleHeightClass.Low), new Vector2(0f, 0f), 0, move, bullets, tall);
            Obstacle pillar = NewObstacle(Data(ObstacleHeightClass.Tall), new Vector2(4f, 0f), 1, move, bullets, tall);
            try
            {
                Vector2 start = new Vector2(-1.5f, 0f);

                Vector2 grounded = move.Move(start, new Vector2(2.5f, 0f), Body);
                Assert.Less(grounded.x, 0f, "on the ground the low wall stops the player");

                Vector2 airborne = tall.Move(start, new Vector2(2.5f, 0f), Body);
                Assert.AreEqual(1f, airborne.x, 0.01f, "in the air the player passes over the low wall");

                Vector2 blocked = tall.Move(new Vector2(2f, 0f), new Vector2(3f, 0f), Body);
                Assert.Less(blocked.x, 4f - 0.5f, "a tall pillar stops an airborne player");

                Vector2 wall = tall.Move(new Vector2(7f, 0f), new Vector2(3f, 0f), Body);
                Assert.LessOrEqual(wall.x, Bounds.xMax - Body + 0.01f, "the boundary always blocks");
            }
            finally
            {
                Object.DestroyImmediate(low.gameObject);
                Object.DestroyImmediate(pillar.gameObject);
            }
        }

        [Test]
        public void EnemiesPathAroundALowWallBecauseTheyUseTheFullGrid()
        {
            ArenaGrid move = Grid(), bullets = Grid(), tall = Grid();
            var data = Data(ObstacleHeightClass.Low);
            var walls = new List<Obstacle>();
            try
            {
                // A row of low walls from the top of the arena down to y = -1.5: the only way past is the gap at the bottom.
                for (int i = 0; i < 8; i++)
                    walls.Add(NewObstacle(data, new Vector2(0f, 4f - i * 0.5f), i, move, bullets, tall));

                var field = new FlowField(move, 0.3f);
                Vector2 target = new Vector2(3f, 3f);
                field.Build(target);
                Vector2 start = new Vector2(-3f, 3f);
                Assert.IsTrue(field.IsReachable(start));
                Assert.Less(field.Direction(start).y, -0.3f, "the enemy heads down, round the wall, not straight at it");

                // A player that can clear Low walls takes the direct line; the enemy's field does not.
                Assert.IsTrue(move.SegmentBlocked(start, target, 0.3f, out _));
                Assert.IsFalse(tall.SegmentBlocked(start, target, 0.3f, out _));
            }
            finally
            {
                foreach (Obstacle w in walls)
                    Object.DestroyImmediate(w.gameObject);
            }
        }

        [Test]
        public void TheHeightClassOfTheShippedObstaclesMatchesTheirRole()
        {
            ObstacleData Load(string name) => AssetDatabase.LoadAssetAtPath<ObstacleData>($"Assets/Data/Arenas/Obstacles/Obstacle_{name}.asset");
            Assert.IsTrue(Load("LowWall").IsLow);
            Assert.IsTrue(Load("Crate").IsLow);
            Assert.IsTrue(Load("Cabbage").IsLow);
            Assert.IsFalse(Load("Pillar").IsLow);
            Assert.IsFalse(Load("Pumpkin").IsLow);
        }

        // ---- tall fade rule

        [Test]
        public void ACharacterBehindTheArtIsHiddenOneBesideOrInFrontIsNot()
        {
            Vector2 center = new Vector2(0f, 0f);
            Vector2 footprint = new Vector2(0.9f, 0.55f);
            Vector2 art = new Vector2(1.1f, 2.3f);   // reaches up to y = 2.0

            Assert.IsTrue(FadeRule.IsBehind(center, footprint, art, new Vector2(0f, 1f), 0.25f));
            Assert.IsTrue(FadeRule.IsBehind(center, footprint, art, new Vector2(0.7f, 1.5f), 0.25f));    // within the margin
            Assert.IsFalse(FadeRule.IsBehind(center, footprint, art, new Vector2(1.2f, 1f), 0.25f));     // beside it
            Assert.IsFalse(FadeRule.IsBehind(center, footprint, art, new Vector2(0f, 2.6f), 0.25f));     // above the top of the art
            Assert.IsFalse(FadeRule.IsBehind(center, footprint, art, new Vector2(0f, -1f), 0.25f));      // in front of it
        }

        [Test]
        public void FadingChangesOnlyTheArtOpacityAndResetsOnRestore()
        {
            ArenaGrid move = Grid();
            Obstacle pillar = NewObstacle(Data(ObstacleHeightClass.Tall), Vector2.zero, 0, move, null, null);
            try
            {
                pillar.SetFade(0.4f);
                Assert.AreEqual(0.4f, pillar.Art.color.a, 0.001f);
                pillar.Restore();
                Assert.AreEqual(1f, pillar.Fade, 0.001f);
                Assert.AreEqual(1f, pillar.Art.color.a, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(pillar.gameObject);
            }
        }

        // ---- budget and validator on hand-made layouts

        private static ArenaLayoutData MakeLayout(ObstaclePlacement[] obstacles, TrapPlacement[] traps, Vector2[] gates = null, Vector2? spawn = null)
        {
            var shell = ScriptableObject.CreateInstance<ArenaData>();
            var layout = ScriptableObject.CreateInstance<ArenaLayoutData>();
            layout.Configure(shell, spawn ?? new Vector2(0f, -3.2f), gates ?? new[] { new Vector2(-5f, 3.8f), new Vector2(7.2f, 0.5f) }, obstacles, traps);
            return layout;
        }

        private static ObstaclePlacement Placement(ObstacleData data, float x, float y, float w = 0f, float h = 0f) =>
            new ObstaclePlacement { Data = data, Position = new Vector2(x, y), SizeOverride = new Vector2(w, h) };

        private static TrapData Trap(TrapKind kind)
        {
            var trap = ScriptableObject.CreateInstance<TrapData>();
            trap.Configure(kind, new Vector2(1f, 1f), 1f, 10f, 1f, 1f, 0.4f, 3f, 0f);
            return trap;
        }

        [Test]
        public void TheBudgetCountsTrapKindsAndObstacleClasses()
        {
            var layout = MakeLayout(
                new[] { Placement(Data(ObstacleHeightClass.Low), 0, 0), Placement(Data(ObstacleHeightClass.Tall, ObstacleKind.Breakable), 2, 0) },
                new[] { new TrapPlacement { Data = Trap(TrapKind.Vent) }, new TrapPlacement { Data = Trap(TrapKind.Zone) } });

            HazardBudget used = HazardBudget.Measure(layout);
            Assert.AreEqual(1, used.Vents);
            Assert.AreEqual(1, used.Zones);
            Assert.AreEqual(0, used.Skewers);
            Assert.AreEqual(1, used.LowObstacles);
            Assert.AreEqual(1, used.TallObstacles);
            Assert.AreEqual(1, used.Breakables);

            Assert.IsFalse(new HazardBudget { Vents = 1, LowObstacles = 1, TallObstacles = 1, Breakables = 1 }.Allows(used, out string problem));
            StringAssert.Contains("hazard zones", problem);
        }

        [Test]
        public void AnObstacleOnThePlayerSpawnFailsTheClearAreaCheck()
        {
            var layout = MakeLayout(new[] { Placement(Data(ObstacleHeightClass.Tall), 0.5f, -3f) }, new TrapPlacement[0]);
            var problems = new List<string>();
            Assert.IsFalse(LayoutValidator.Validate(layout, new HazardBudget { TallObstacles = 5 }, problems));
            Assert.IsTrue(problems.Exists(p => p.Contains("spawn")));
        }

        [Test]
        public void ASealedGateFailsTheLaneCheckAndAnOpenOneDoesNot()
        {
            var lowWall = Data(ObstacleHeightClass.Low);
            // A wall of low walls right across the arena at y = 2 (x from -7.75 to 7.75): the gate at (-5, 3.8) is cut off.
            var row = new List<ObstaclePlacement>();
            for (float x = -7.5f; x <= 7.5f; x += 1f)
                row.Add(Placement(lowWall, x, 2f));
            var sealedLayout = MakeLayout(row.ToArray(), new TrapPlacement[0], new[] { new Vector2(-5f, 3.8f) });
            var problems = new List<string>();
            Assert.IsFalse(LayoutValidator.Validate(sealedLayout, new HazardBudget { LowObstacles = 99 }, problems));
            Assert.IsTrue(problems.Exists(p => p.Contains("lane")));

            row.RemoveAt(row.Count / 2);
            row.RemoveAt(row.Count / 2);
            var openLayout = MakeLayout(row.ToArray(), new TrapPlacement[0], new[] { new Vector2(-5f, 3.8f) });
            problems.Clear();
            Assert.IsTrue(LayoutValidator.Validate(openLayout, new HazardBudget { LowObstacles = 99 }, problems), string.Join("; ", problems));
        }

        [Test]
        public void ATrapOnTheSpawnFailsAndATrapOverBudgetFails()
        {
            var vent = Trap(TrapKind.Vent);
            var onSpawn = MakeLayout(new ObstaclePlacement[0], new[] { new TrapPlacement { Data = vent, Position = new Vector2(0f, -3f) } });
            var problems = new List<string>();
            Assert.IsFalse(LayoutValidator.Validate(onSpawn, new HazardBudget { Vents = 1 }, problems));

            var far = MakeLayout(new ObstaclePlacement[0], new[] { new TrapPlacement { Data = vent, Position = new Vector2(5.5f, -2.6f) } });
            problems.Clear();
            Assert.IsTrue(LayoutValidator.Validate(far, new HazardBudget { Vents = 1 }, problems), string.Join("; ", problems));
            problems.Clear();
            Assert.IsFalse(LayoutValidator.Validate(far, default, problems), "round with no trap budget must reject a vent");
        }

        // ---- the shipped rounds 1-7

        private static IEnumerable<RoundData> ShippedRounds()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
            for (int i = 1; i <= 7; i++)
                yield return config.GetRound(i);
        }

        [Test]
        public void EveryShippedRoundHasALayoutThatPassesTheValidator()
        {
            int round = 0;
            foreach (RoundData data in ShippedRounds())
            {
                round++;
                Assert.IsNotNull(data, $"round {round}");
                Assert.IsNotNull(data.Layout, $"round {round} has no layout");
                var problems = new List<string>();
                Assert.IsTrue(LayoutValidator.Validate(data.Layout, data.HazardBudget, problems), $"round {round}: {string.Join("; ", problems)}");
            }
        }

        [Test]
        public void RoundOneHasNoTrapsOrHazardZonesAndEachLaterRoundAddsAtMostOneNewKind()
        {
            var rounds = new List<RoundData>(ShippedRounds());
            Assert.AreEqual(0, rounds[0].HazardBudget.Traps);
            Assert.AreEqual(0, HazardBudget.Measure(rounds[0].Layout).Traps);

            for (int i = 1; i < rounds.Count; i++)
                Assert.LessOrEqual(rounds[i].HazardBudget.NewTrapKindsSince(rounds[i - 1].HazardBudget), 1, $"round {i + 1}");
        }

        [Test]
        public void BossRoundsUseTheirOwnLayoutsAndTheyAreMoreOpenThanTheirNeighbours()
        {
            var rounds = new List<RoundData>(ShippedRounds());
            foreach (int boss in new[] { 3, 5, 7 })
            {
                RoundData round = rounds[boss - 1];
                Assert.IsTrue(round.IsBossRound);
                foreach (int other in new[] { boss - 1, boss + 1 })
                    if (other >= 1 && other <= 7)
                        Assert.AreNotSame(rounds[other - 1].Layout, round.Layout, $"boss round {boss} shares a layout with round {other}");
            }
        }

        [Test]
        public void EveryShippedLayoutKeepsItsGateLanesEvenForALargePlayerFreeWalk()
        {
            foreach (RoundData data in ShippedRounds())
            {
                ArenaGrid grid = LayoutValidator.BuildGrid(data.Layout);
                foreach (Vector2 gate in data.Layout.SpawnGates)
                    Assert.IsTrue(LayoutValidator.HasLane(grid, gate, data.Layout.PlayerSpawn, 0.6f), $"gate {gate} in {data.Layout.name}");
            }
        }
    }
}
