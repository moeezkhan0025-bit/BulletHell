using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Projectiles;
using BulletHell.Save;
using BulletHell.Weapons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BulletHell.Tests
{
    /// <summary>M9a: armament interaction rules, variable slots, stacks, generated descriptions and saving.</summary>
    public class M9aTests
    {
        private static T Make<T>() where T : ScriptableObject => ScriptableObject.CreateInstance<T>();

        private static WeaponArmData Arm(string id, int slots = 3)
        {
            var arm = Make<WeaponArmData>();
            arm.SetId(id);
            arm.SetArmamentSlots(slots);
            return arm;
        }

        private static ArmamentData Armament(string id, int maxStacks = 3, params ArmEffect[] effects)
        {
            var armament = Make<ArmamentData>();
            armament.SetId(id);
            armament.Set(id, new StatModifier(StatType.Damage, ModifierMode.Percent, 10f));
            armament.SetEffects(effects);
            armament.SetMeta(ArmamentRarity.Common, ArmamentTags.None, 1, maxStacks);
            return armament;
        }

        private static PierceEffect Pierce(int count = 1)
        {
            var effect = Make<PierceEffect>();
            effect.Set(count);
            return effect;
        }

        private static RicochetEffect Ricochet(int bounces = 3, int extra = 1)
        {
            var effect = Make<RicochetEffect>();
            effect.Set(bounces, extra);
            return effect;
        }

        private static HomingEffect Homing()
        {
            var effect = Make<HomingEffect>();
            effect.Set(120f, 60f, 60f, 30f, 9f);
            return effect;
        }

        // ---- interaction rules (pure)

        [Test]
        public void PierceIsUsedUpBeforeABulletStops()
        {
            var state = new ShotState { PierceLeft = 2, BouncesLeft = 0 };
            Assert.AreEqual(HitOutcome.Continue, ProjectileRules.OnEnemyHit(ref state));
            Assert.AreEqual(HitOutcome.Continue, ProjectileRules.OnEnemyHit(ref state));
            Assert.AreEqual(HitOutcome.Stop, ProjectileRules.OnEnemyHit(ref state));
        }

        [Test]
        public void HittingAnEnemyNeverSpendsARicochetBounce()
        {
            var state = new ShotState { PierceLeft = 0, BouncesLeft = 3 };
            Assert.AreEqual(HitOutcome.Stop, ProjectileRules.OnEnemyHit(ref state));
            Assert.AreEqual(3, state.BouncesLeft);
        }

        [Test]
        public void RicochetBouncesOffWallsThenStops_AndNeverSpendsPierce()
        {
            var state = new ShotState { PierceLeft = 1, BouncesLeft = 2 };
            Vector2 velocity = new Vector2(3f, -4f);

            Assert.AreEqual(HitOutcome.Bounce, ProjectileRules.OnWallHit(ref state, velocity, Vector2.up, out Vector2 after));
            Assert.AreEqual(new Vector2(3f, 4f), after);            // reflected about the surface normal
            Assert.AreEqual(HitOutcome.Bounce, ProjectileRules.OnWallHit(ref state, after, Vector2.down, out _));
            Assert.AreEqual(HitOutcome.Stop, ProjectileRules.OnWallHit(ref state, after, Vector2.up, out Vector2 kept));
            Assert.AreEqual(after, kept);
            Assert.AreEqual(1, state.PierceLeft);                   // wall hits never used the pierce
        }

        [Test]
        public void PierceAndRicochetTogether_EnemiesUsePierceWallsUseBounces()
        {
            var state = new ShotState { PierceLeft = 1, BouncesLeft = 1 };
            Assert.AreEqual(HitOutcome.Continue, ProjectileRules.OnEnemyHit(ref state));   // pierce
            Assert.AreEqual(HitOutcome.Bounce, ProjectileRules.OnWallHit(ref state, Vector2.right, Vector2.left, out _));
            Assert.AreEqual(HitOutcome.Stop, ProjectileRules.OnEnemyHit(ref state));       // pierce used up: enemy stops it
            Assert.AreEqual(0, state.BouncesLeft);
        }

        [Test]
        public void HomingMustRetargetAfterEveryPierceAndBounce()
        {
            var state = new ShotState { PierceLeft = 1, BouncesLeft = 1, NeedsRetarget = false };
            ProjectileRules.OnEnemyHit(ref state);
            Assert.IsTrue(state.NeedsRetarget);

            state.NeedsRetarget = false;
            ProjectileRules.OnWallHit(ref state, Vector2.right, Vector2.left, out _);
            Assert.IsTrue(state.NeedsRetarget);

            state.NeedsRetarget = false;
            ProjectileRules.OnEnemyHit(ref state);     // this one stops the bullet: nothing to retarget for
            Assert.IsFalse(state.NeedsRetarget);
        }

        [Test]
        public void HomingConeAndRangeSelectTargets()
        {
            Vector2 origin = Vector2.zero;
            Assert.IsTrue(ProjectileRules.InCone(origin, Vector2.right, new Vector2(5f, 1f), 60f, 9f, out float sqr));
            Assert.AreEqual(26f, sqr, 0.001f);
            Assert.IsFalse(ProjectileRules.InCone(origin, Vector2.right, new Vector2(2f, 4f), 60f, 9f, out _));   // outside the cone
            Assert.IsFalse(ProjectileRules.InCone(origin, Vector2.right, new Vector2(12f, 0f), 60f, 9f, out _));  // out of range
            Assert.IsFalse(ProjectileRules.InCone(origin, Vector2.right, new Vector2(-5f, 0f), 60f, 9f, out _));  // behind
            Assert.IsTrue(ProjectileRules.InCone(origin, Vector2.right, new Vector2(-5f, 0f), 360f, 9f, out _));  // full circle
        }

        [Test]
        public void HomingTurnIsLimitedByTheTurnRateAndKeepsSpeed()
        {
            Vector2 velocity = new Vector2(10f, 0f);
            Vector2 turned = ProjectileRules.TurnToward(velocity, Vector2.zero, new Vector2(0f, 10f), 30f);
            Assert.AreEqual(10f, turned.magnitude, 0.001f);
            Assert.AreEqual(30f, Vector2.SignedAngle(Vector2.right, turned), 0.01f);

            // Enough turn budget: points exactly at the target.
            Vector2 aimed = ProjectileRules.TurnToward(velocity, Vector2.zero, new Vector2(0f, 10f), 180f);
            Assert.AreEqual(90f, Vector2.SignedAngle(Vector2.right, aimed), 0.01f);
        }

        // ---- variable slots, stacks, effects

        [Test]
        public void ArmamentSlotsFollowTheArmData()
        {
            var inventory = new ArmamentInventory();
            ArmamentData a = Armament("a");
            for (int i = 0; i < 3; i++)
                inventory.Add(a);
            var arm = new ArmInstance(Arm("one", 1));

            Assert.AreEqual(1, arm.SlotCount);
            Assert.IsTrue(arm.TryEquip(a, inventory));
            Assert.IsFalse(arm.TryEquip(a, inventory));          // no second slot
            Assert.IsFalse(arm.TryEquipAt(1, a, inventory));     // slot 2 does not exist on this arm
            Assert.IsNull(arm.GetArmament(1));
            Assert.AreEqual(2, inventory.Count);
            Assert.AreEqual(0, arm.LastFilledSlot);
        }

        [Test]
        public void MaxStacksLimitsHowManyCopiesOneArmCarries()
        {
            var inventory = new ArmamentInventory();
            ArmamentData limited = Armament("limited", 2);
            for (int i = 0; i < 3; i++)
                inventory.Add(limited);
            var arm = new ArmInstance(Arm("three", 3));

            Assert.IsTrue(arm.TryEquip(limited, inventory));
            Assert.IsTrue(arm.TryEquip(limited, inventory));
            Assert.IsFalse(arm.TryEquip(limited, inventory));
            Assert.IsFalse(arm.CanEquipAt(2, limited, out string reason));
            StringAssert.Contains("limited to 2", reason);
            Assert.AreEqual(1, inventory.Count);                 // the refused copy stays in the inventory
            Assert.AreEqual(2, arm.CountOf(limited));

            // Swapping a copy in place is not blocked by the limit.
            Assert.IsTrue(arm.CanEquipAt(0, limited, out _));
        }

        [Test]
        public void TheSameEffectOnSeveralArmamentsIsAppliedOnceWithItsStackCount()
        {
            PierceEffect pierce = Pierce(1);
            RicochetEffect ricochet = Ricochet(3, 1);
            var inventory = new ArmamentInventory();
            ArmamentData p = Armament("p", 3, pierce);
            ArmamentData r = Armament("r", 3, ricochet);
            inventory.Add(p);
            inventory.Add(p);
            inventory.Add(r);
            inventory.Add(r);
            var arm = new ArmInstance(Arm("three", 3));

            arm.TryEquip(p, inventory);
            arm.TryEquip(p, inventory);
            arm.TryEquip(r, inventory);

            Assert.AreEqual(2, arm.Effects.Count);
            Assert.AreEqual(2, arm.EffectStacks[0]);
            Assert.AreEqual(2, arm.Shot.Pierce);                 // +1 per stack
            Assert.AreEqual(3, arm.Shot.Bounces);                // one stack: the base count

            arm.Unequip(2, inventory);
            arm.TryEquip(r, inventory);
            arm.TryEquipAt(1, r, inventory);                     // replaces a pierce copy with a second ricochet
            Assert.AreEqual(4, arm.Shot.Bounces);                // two stacks: base 3 + 1
            Assert.AreEqual(1, arm.Shot.Pierce);
        }

        [Test]
        public void HomingStacksRaiseTurnRateAndCone()
        {
            HomingEffect homing = Homing();
            var inventory = new ArmamentInventory();
            ArmamentData h = Armament("h", 3, homing);
            inventory.Add(h);
            inventory.Add(h);
            var arm = new ArmInstance(Arm("two", 2));

            arm.TryEquip(h, inventory);
            Assert.AreEqual(120f, arm.Shot.HomingTurnRate);
            Assert.AreEqual(60f, arm.Shot.HomingCone);

            arm.TryEquip(h, inventory);
            Assert.AreEqual(180f, arm.Shot.HomingTurnRate);
            Assert.AreEqual(90f, arm.Shot.HomingCone);
            Assert.IsTrue(arm.Shot.HasHoming);
        }

        [Test]
        public void AutoFireSetsRateAndArc()
        {
            var effect = Make<AutoFireEffect>();
            effect.Set(0.5f, 45f);
            var inventory = new ArmamentInventory();
            ArmamentData a = Armament("a", 1, effect);
            inventory.Add(a);
            var arm = new ArmInstance(Arm("one", 1));

            Assert.IsFalse(arm.Shot.HasAutoFire);
            arm.TryEquip(a, inventory);
            Assert.AreEqual(0.5f, arm.Shot.AutoFireRate);
            Assert.AreEqual(45f, arm.Shot.AutoFireArc);
        }

        [Test]
        public void VelocityStacksMultiplyBulletSpeed()
        {
            var velocity = Make<ArmamentData>();
            velocity.SetId("velocity");
            velocity.Set("Velocity", new StatModifier(StatType.ProjectileSpeed, ModifierMode.Percent, 25f));
            var arm = Arm("arm");
            var inventory = new ArmamentInventory();
            inventory.Add(velocity);
            inventory.Add(velocity);
            var instance = new ArmInstance(arm);
            float baseSpeed = instance.Stats.ProjectileSpeed;

            instance.TryEquip(velocity, inventory);
            Assert.AreEqual(baseSpeed * 1.25f, instance.Stats.ProjectileSpeed, 0.001f);
            instance.TryEquip(velocity, inventory);
            Assert.AreEqual(baseSpeed * 1.25f * 1.25f, instance.Stats.ProjectileSpeed, 0.001f);
        }

        // ---- generated descriptions

        [Test]
        public void DescriptionsAreGeneratedFromTheArmamentData()
        {
            var velocity = Make<ArmamentData>();
            velocity.SetId("velocity");
            velocity.Set("Velocity", new StatModifier(StatType.ProjectileSpeed, ModifierMode.Percent, 25f),
                         new StatModifier(StatType.ProjectilesPerShot, ModifierMode.Flat, 1f));
            velocity.SetMeta(ArmamentRarity.Rare, ArmamentTags.Speed | ArmamentTags.Damage, 2, 4);
            velocity.SetEffects(Pierce(2), Ricochet(3, 1));

            List<string> lines = ItemDescriber.ArmamentLines(velocity);
            string text = string.Join("\n", lines);

            StringAssert.Contains("Rare", lines[0]);
            StringAssert.Contains("Speed", lines[0]);
            StringAssert.Contains("+25% bullet speed", text);
            StringAssert.Contains("+1 projectiles", text);
            StringAssert.Contains("pass through 2 extra enemies", text);
            StringAssert.Contains("bounce off walls and obstacles up to 3 times", text);
            StringAssert.Contains("Max 4", lines[lines.Count - 1]);
        }

        [Test]
        public void EffectDescriptionsFollowTheirData()
        {
            var lines = new List<string>();
            Homing().Describe(2, lines);
            StringAssert.Contains("90 degree cone", lines[0]);
            StringAssert.Contains("180 deg/s", lines[0]);

            lines.Clear();
            Ricochet(3, 2).Describe(3, lines);
            StringAssert.Contains("7 times", lines[0]);      // 3 + 2 x 2
        }

        [Test]
        public void ArmLineListsOneEntryPerSlotOfThatArm()
        {
            var arm = new ArmInstance(Arm("Two", 2));
            Assert.AreEqual("Arm  [- | -]", ItemDescriber.ArmWithArmaments(arm));
        }

        // ---- saving with variable slots

        private static AssetRegistry Registry(WeaponArmData[] arms, ArmamentData[] armaments)
        {
            var registry = Make<AssetRegistry>();
            registry.Set(arms, armaments, new AmmoTypeData[0]);
            return registry;
        }

        [Test]
        public void SaveWritesOneArmamentIdPerSlotOfTheArm()
        {
            WeaponArmData two = Arm("two", 2);
            AssetRegistry registry = Registry(new[] { two }, new ArmamentData[0]);
            var state = new RunState();
            state.Loadout[0] = new ArmInstance(two);

            SaveData data = RunSaveMapper.ToSave(state);

            Assert.AreEqual(2, data.loadout[0].armamentIds.Length);
            Assert.IsTrue(RunSaveMapper.TryFromSave(data, registry, out RunState loaded));
            Assert.AreEqual(2, loaded.Loadout[0].SlotCount);
        }

        [Test]
        public void SavedArmamentsThatNoLongerFitReturnToTheInventory()
        {
            WeaponArmData oneSlot = Arm("red", 1);
            ArmamentData a = Armament("a");
            ArmamentData b = Armament("b");
            ArmamentData c = Armament("c");
            AssetRegistry registry = Registry(new[] { oneSlot }, new[] { a, b, c });

            // An old save: this arm had 3 slots then, all filled.
            var data = new SaveData
            {
                round = 2,
                loadout = new[] { new ArmSave { armId = "red", armamentIds = new[] { "a", "b", "c" } }, null, null, null, null, null, null, null },
                armamentInventory = new string[0],
            };

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no longer fits"));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no longer fits"));
            Assert.IsTrue(RunSaveMapper.TryFromSave(data, registry, out RunState loaded));

            Assert.AreEqual(1, loaded.Loadout[0].ArmamentCount);
            Assert.AreSame(a, loaded.Loadout[0].GetArmament(0));
            Assert.AreEqual(2, loaded.Armaments.Count);
            Assert.IsTrue(loaded.Armaments.Contains(b));
            Assert.IsTrue(loaded.Armaments.Contains(c));
        }

        [Test]
        public void SavedCopiesOverTheStackLimitReturnToTheInventory()
        {
            WeaponArmData three = Arm("three", 3);
            ArmamentData limited = Armament("limited", 1);
            AssetRegistry registry = Registry(new[] { three }, new[] { limited });
            var data = new SaveData
            {
                round = 1,
                loadout = new[] { new ArmSave { armId = "three", armamentIds = new[] { "limited", "limited", "" } }, null, null, null, null, null, null, null },
                armamentInventory = new string[0],
            };

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no longer fits"));
            Assert.IsTrue(RunSaveMapper.TryFromSave(data, registry, out RunState loaded));

            Assert.AreEqual(1, loaded.Loadout[0].ArmamentCount);
            Assert.AreEqual(1, loaded.Armaments.Count);
        }

        [Test]
        public void NewArmamentsRoundTripThroughJson()
        {
            WeaponArmData arm = Arm("arm", 3);
            ArmamentData homing = Armament("Armament_Homing", 3, Homing());
            ArmamentData auto = Armament("Armament_AutoFire", 1);
            AssetRegistry registry = Registry(new[] { arm }, new[] { homing, auto });
            var state = new RunState();
            state.Loadout[0] = new ArmInstance(arm);
            state.Loadout[0].TryAdd(homing);
            state.Loadout[0].TryAdd(auto);
            state.Armaments.Add(homing);

            SaveData data = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(RunSaveMapper.ToSave(state)));
            Assert.IsTrue(RunSaveMapper.TryFromSave(data, registry, out RunState loaded));

            Assert.AreSame(homing, loaded.Loadout[0].GetArmament(0));
            Assert.AreSame(auto, loaded.Loadout[0].GetArmament(1));
            Assert.IsTrue(loaded.Loadout[0].Shot.HasHoming);
            Assert.AreEqual(1, loaded.Armaments.Count);
        }
    }
}
