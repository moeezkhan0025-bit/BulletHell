using System.IO;
using BulletHell.Core;
using BulletHell.Save;
using BulletHell.Weapons;
using NUnit.Framework;
using UnityEngine;

namespace BulletHell.Tests
{
    public class GameFlowTests
    {
        // ---- state machine

        private static GameStateMachine InCombat()
        {
            var machine = new GameStateMachine();
            machine.TryEnter(GameState.RoundIntro);
            machine.TryEnter(GameState.Combat);
            return machine;
        }

        [Test]
        public void RunLoopFollowsTheDesignedOrder()
        {
            var machine = new GameStateMachine();
            Assert.IsTrue(machine.TryEnter(GameState.RoundIntro));
            Assert.IsTrue(machine.TryEnter(GameState.Combat));
            Assert.IsTrue(machine.TryEnter(GameState.RoundResults));
            Assert.IsTrue(machine.TryEnter(GameState.Shop));
            Assert.IsTrue(machine.TryEnter(GameState.Armory));
            Assert.IsTrue(machine.TryEnter(GameState.RoundIntro));
            Assert.IsTrue(machine.TryEnter(GameState.Combat));
        }

        [Test]
        public void IllegalTransitionsAreRefused()
        {
            var machine = InCombat();
            Assert.IsFalse(machine.TryEnter(GameState.Shop));
            Assert.IsFalse(machine.TryEnter(GameState.Armory));
            Assert.AreEqual(GameState.Combat, machine.Current);
        }

        [Test]
        public void ContinueCanStartAtTheShopButNotAtTheArmory()
        {
            var shop = new GameStateMachine();
            Assert.IsTrue(shop.TryEnter(GameState.Shop));
            var armory = new GameStateMachine();
            Assert.IsFalse(armory.TryEnter(GameState.Armory));
        }

        [Test]
        public void PauseAndGameOverOnlyFromCombat_PauseResumesToCombat()
        {
            var machine = InCombat();
            Assert.IsTrue(machine.TryEnter(GameState.Pause));
            Assert.IsFalse(machine.TryEnter(GameState.GameOver));
            Assert.IsTrue(machine.TryEnter(GameState.Combat));

            machine.TryEnter(GameState.RoundResults);
            Assert.IsFalse(machine.TryEnter(GameState.Pause));
            Assert.IsFalse(machine.TryEnter(GameState.GameOver));
        }

        [Test]
        public void StateChangedReportsFromAndTo()
        {
            var machine = InCombat();
            GameState from = GameState.None, to = GameState.None;
            machine.StateChanged += (f, t) => { from = f; to = t; };

            machine.TryEnter(GameState.RoundResults);

            Assert.AreEqual(GameState.Combat, from);
            Assert.AreEqual(GameState.RoundResults, to);
            Assert.AreEqual(GameState.Combat, machine.Previous);
        }

        // ---- save file

        private static string TempSavePath() => Path.Combine(Path.GetTempPath(), "bullethell_test_" + System.Guid.NewGuid() + ".json");

        [Test]
        public void SaveFileRoundTripsAndDeletes()
        {
            string path = TempSavePath();
            var system = new LocalFileSaveSystem(path);
            Assert.IsFalse(system.HasSave);

            system.Save(new SaveData { round = 4, currency = 123, ammoSlots = new[] { "a", "", "", "" } });
            Assert.IsTrue(system.HasSave);
            Assert.IsTrue(system.TryLoad(out SaveData loaded));
            Assert.AreEqual(4, loaded.round);
            Assert.AreEqual(123, loaded.currency);
            Assert.AreEqual("a", loaded.ammoSlots[0]);

            system.Delete();
            Assert.IsFalse(system.HasSave);
            Assert.IsFalse(File.Exists(path + ".tmp"));
        }

        [Test]
        public void SavingTwiceReplacesTheFileWithoutLeavingATempFile()
        {
            string path = TempSavePath();
            var system = new LocalFileSaveSystem(path);
            system.Save(new SaveData { round = 1 });
            system.Save(new SaveData { round = 2 });

            system.TryLoad(out SaveData loaded);
            Assert.AreEqual(2, loaded.round);
            Assert.IsFalse(File.Exists(path + ".tmp"));
            system.Delete();
        }

        [Test]
        public void CorruptOrWrongVersionSaveIsReportedAsUnusableNotThrown()
        {
            string path = TempSavePath();
            var system = new LocalFileSaveSystem(path);

            File.WriteAllText(path, "{ this is not json");
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("could not be read|unsupported version"));
            Assert.IsFalse(system.TryLoad(out _));

            File.WriteAllText(path, JsonUtility.ToJson(new SaveData { version = SaveData.CurrentVersion + 1 }));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unsupported version"));
            Assert.IsFalse(system.TryLoad(out _));
            system.Delete();
        }

        // ---- run state <-> save data

        private static T Make<T>(System.Action<T> init) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            init(asset);
            return asset;
        }

        private static WeaponArmData Arm(string id) => Make<WeaponArmData>(a => a.SetId(id));
        private static ArmamentData Armament(string id) => Make<ArmamentData>(a => a.SetId(id));
        private static AmmoTypeData Ammo(string id) => Make<AmmoTypeData>(a => a.SetId(id));

        private static AssetRegistry Registry(WeaponArmData[] arms, ArmamentData[] armaments, AmmoTypeData[] ammo)
        {
            var registry = ScriptableObject.CreateInstance<AssetRegistry>();
            registry.Set(arms, armaments, ammo);
            return registry;
        }

        [Test]
        public void RunStateSurvivesASaveAndLoad()
        {
            WeaponArmData red = Arm("red"), blue = Arm("blue");
            ArmamentData dmg = Armament("dmg"), burn = Armament("burn");
            AmmoTypeData basic = Ammo("basic"), laser = Ammo("laser");
            AssetRegistry registry = Registry(new[] { red, blue }, new[] { dmg, burn }, new[] { basic, laser });

            var state = new RunState { Round = 5, Currency = 340 };
            state.Loadout[0] = new ArmInstance(red);
            state.Loadout[3] = new ArmInstance(blue);
            var inv = new ArmamentInventory();
            inv.Add(dmg);
            inv.Add(burn);
            inv.Add(burn);
            state.Loadout[3].TryEquipAt(1, dmg, inv);    // slot 2 of the SE arm
            state.Loadout[3].TryEquipAt(2, burn, inv);
            state.Armaments.Add(burn);
            state.SpareArms.Add(new ArmInstance(red));
            state.Ammo.Set(0, basic);
            state.Ammo.Set(2, laser);
            state.Ammo.TrySelect(2);

            SaveData data = RunSaveMapper.ToSave(state);
            data = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data)); // through real JSON
            Assert.IsTrue(RunSaveMapper.TryFromSave(data, registry, out RunState loaded));

            Assert.AreEqual(5, loaded.Round);
            Assert.AreEqual(340, loaded.Currency);
            Assert.AreSame(red, loaded.Loadout[0].Data);
            Assert.IsNull(loaded.Loadout[1]);
            Assert.AreSame(blue, loaded.Loadout[3].Data);
            Assert.IsNull(loaded.Loadout[3].GetArmament(0));
            Assert.AreSame(dmg, loaded.Loadout[3].GetArmament(1));
            Assert.AreSame(burn, loaded.Loadout[3].GetArmament(2));
            Assert.AreEqual(1, loaded.Armaments.Count);
            Assert.AreSame(burn, loaded.Armaments.Get(0));
            Assert.AreEqual(1, loaded.SpareArms.Count);
            Assert.AreSame(basic, loaded.Ammo.Get(0));
            Assert.IsNull(loaded.Ammo.Get(1));
            Assert.AreSame(laser, loaded.Ammo.Get(2));
            Assert.AreEqual(2, loaded.Ammo.ActiveIndex);
        }

        [Test]
        public void UnknownIdsInASaveAreSkippedNotFatal()
        {
            WeaponArmData red = Arm("red");
            AssetRegistry registry = Registry(new[] { red }, new ArmamentData[0], new AmmoTypeData[0]);
            var data = new SaveData
            {
                round = 2,
                loadout = new[] { new ArmSave { armId = "red" }, new ArmSave { armId = "deleted_arm" } },
                armamentInventory = new[] { "deleted_armament" },
                ammoSlots = new[] { "deleted_ammo", "", "", "" },
            };

            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unknown arm"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unknown armament"));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("unknown ammo"));
            Assert.IsTrue(RunSaveMapper.TryFromSave(data, registry, out RunState loaded));

            Assert.IsNotNull(loaded.Loadout[0]);
            Assert.IsNull(loaded.Loadout[1]);
            Assert.AreEqual(0, loaded.Armaments.Count);
            Assert.IsNull(loaded.Ammo.Get(0));
        }

        [Test]
        public void SaveWithNoUsableArmsCannotBeLoaded()
        {
            AssetRegistry registry = Registry(new WeaponArmData[0], new ArmamentData[0], new AmmoTypeData[0]);
            Assert.IsFalse(RunSaveMapper.TryFromSave(new SaveData(), registry, out RunState loaded));
            Assert.IsNull(loaded);
        }

        [Test]
        public void RegistryFindsAssetsByIdAndReturnsNullForUnknown()
        {
            WeaponArmData red = Arm("red");
            AssetRegistry registry = Registry(new[] { red }, new ArmamentData[0], new AmmoTypeData[0]);
            Assert.AreSame(red, registry.GetArm("red"));
            Assert.IsNull(registry.GetArm("nope"));
            Assert.IsNull(registry.GetArm(""));
            Assert.AreEqual("red", AssetRegistry.IdOf(red));
            Assert.AreEqual("", AssetRegistry.IdOf((WeaponArmData)null));
        }
    }
}
