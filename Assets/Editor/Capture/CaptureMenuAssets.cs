using System.Collections.Generic;
using BulletHell.Capture;
using BulletHell.Enemies;
using BulletHell.Weapons;
using UnityEditor;
using UnityEngine;

namespace BulletHell.EditorTools.GameplayCapture
{
    /// <summary>
    /// R1 portfolio capture kit - creates (or repairs) the capture data assets in Assets/Data/Capture from the existing game assets:
    /// CaptureTuning and the four scenarios. Re-running keeps the asset GUIDs and rewrites the scenario fields, so it is also the
    /// way back to the shipped setup after experimenting. Menu: BulletHell/Capture/Setup/Create or repair capture assets.
    ///
    /// Scenarios (all start a FRESH run through the normal services; see CaptureDirector):
    ///   Hero loop        round 2 (Grunts + Weavers) with six armed arms (Homing, Ricochet, Auto-fire) and 15 extra enemies kept alive: the busiest 20 s
    ///   Round 1          round 1, the game's starting loadout and ammo
    ///   Two enemy types  round 2 (Chaser + Skirmisher), a mid-run loadout of three arms with a few armaments
    ///   Pumpking         round 3 boss at full HP, four arms with a typical armament set
    /// </summary>
    public static class CaptureMenuAssets
    {
        public const string Folder = "Assets/Data/Capture";
        public const string TuningPath = Folder + "/CaptureTuning.asset";
        public const string HeroLoopPath = Folder + "/Scenario_HeroLoop.asset";
        public const string Round1Path = Folder + "/Scenario_Round1.asset";
        public const string TwoEnemyTypesPath = Folder + "/Scenario_TwoEnemyTypes.asset";
        public const string PumpkingPath = Folder + "/Scenario_Pumpking.asset";

        [MenuItem("BulletHell/Capture/Setup/Create or repair capture assets")]
        public static void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Data", "Capture");

            CaptureTuning tuning = Load<CaptureTuning>(TuningPath);

            // Arms
            WeaponArmData blue = Asset<WeaponArmData>("Arms/Arm_Blue");
            WeaponArmData ember = Asset<WeaponArmData>("Arms/Arm_Ember");
            WeaponArmData green = Asset<WeaponArmData>("Arms/Arm_Green");
            WeaponArmData piercer = Asset<WeaponArmData>("Arms/Arm_Piercer");
            WeaponArmData purple = Asset<WeaponArmData>("Arms/Arm_Purple");
            WeaponArmData red = Asset<WeaponArmData>("Arms/Arm_Red");

            // Armaments
            ArmamentData autoFire = Asset<ArmamentData>("Armaments/Armament_AutoFire");
            ArmamentData speed = Asset<ArmamentData>("Armaments/Armament_BulletSpeed");
            ArmamentData damage = Asset<ArmamentData>("Armaments/Armament_Damage");
            ArmamentData fireRate = Asset<ArmamentData>("Armaments/Armament_FireRate");
            ArmamentData homing = Asset<ArmamentData>("Armaments/Armament_Homing");
            ArmamentData pierce = Asset<ArmamentData>("Armaments/Armament_Pierce");
            ArmamentData ricochet = Asset<ArmamentData>("Armaments/Armament_Ricochet");
            ArmamentData extra = Asset<ArmamentData>("Armaments/Armament_ExtraProjectile");

            // Ammo
            AmmoTypeData basic = Asset<AmmoTypeData>("Ammo/Ammo_Basic");
            AmmoTypeData shotgun = Asset<AmmoTypeData>("Ammo/Ammo_Shotgun");
            AmmoTypeData gatling = Asset<AmmoTypeData>("Ammo/Ammo_Gatling");

            // Enemies
            EnemyData grunt = Asset<EnemyData>("Enemies/Enemy_Grunt");      // Chaser
            EnemyData weaver = Asset<EnemyData>("Enemies/Enemy_Weaver");    // Skirmisher

            // Hero loop: slots N NE E SE S SW W NW. Six arms, most carry Homing / Ricochet / Auto-fire so bullets curve, bounce and
            // fly from the arms that are not selected too. Basic / Shotgun / Gatling to swap between.
            Configure(HeroLoopPath, "Hero loop", "hero-loop", 2, 20f, true, tuning,
                new[]
                {
                    Arm(0, green, homing, autoFire),
                    Arm(1, ember, homing, ricochet),
                    Arm(2, purple, homing, ricochet, autoFire),
                    Arm(4, blue, ricochet, autoFire),
                    Arm(5, red, autoFire),
                    Arm(6, piercer, ricochet, homing, autoFire),
                },
                new[] { basic, shotgun, gatling },
                new[] { Extra(grunt, 8, 3), Extra(weaver, 7, 3) }, 28f);

            // Round 1 with the starting loadout and ammo (no custom loadout, no extras).
            Configure(Round1Path, "Round 1", "round-1", 1, 25f, true, tuning, null, null, null, 0f);

            // Round 2 (Chaser + Skirmisher) with a mid-run loadout.
            Configure(TwoEnemyTypesPath, "Two enemy types", "two-enemy-types", 2, 25f, true, tuning,
                new[]
                {
                    Arm(2, purple, speed, damage),
                    Arm(6, blue, fireRate),
                    Arm(0, green, homing),
                },
                new[] { basic, shotgun },
                null, 0f);

            // The Pumpking at full HP with a typical round-3 kit. No extras: the boss is the show.
            Configure(PumpkingPath, "Pumpking", "pumpking", 3, 40f, true, tuning,
                new[]
                {
                    Arm(2, purple, damage, fireRate, homing),
                    Arm(1, blue, damage, speed),
                    Arm(7, green, pierce, damage),
                    Arm(4, red, damage, extra),
                },
                new[] { basic, shotgun, gatling },
                null, 0f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Capture: scenario assets written to " + Folder);
        }

        private static CaptureArmSetup Arm(int slot, WeaponArmData arm, params ArmamentData[] armaments) =>
            new CaptureArmSetup { slot = slot, arm = arm, armaments = armaments };

        private static CaptureExtraSpawn Extra(EnemyData enemy, int keepAlive, int batch) =>
            new CaptureExtraSpawn { enemy = enemy, keepAlive = keepAlive, batch = batch };

        private static void Configure(string path, string name, string prefix, int round, float seconds, bool autopilot, CaptureTuning tuning,
                                      CaptureArmSetup[] arms, AmmoTypeData[] ammo, CaptureExtraSpawn[] extras, float extraSeconds)
        {
            CaptureScenarioData data = Load<CaptureScenarioData>(path);
            data.EditorConfigure(name, prefix, round, seconds, autopilot, arms, ammo, extras, extraSeconds, tuning);
            EditorUtility.SetDirty(data);

            List<string> problems = data.Validate();
            if (problems.Count > 0)
                Debug.LogWarning($"Capture scenario '{name}': {string.Join("; ", problems)}");
        }

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T Asset<T>(string relativeWithoutExtension) where T : Object
        {
            string path = "Assets/Data/" + relativeWithoutExtension + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogWarning("Capture: missing asset " + path);
            return asset;
        }
    }
}
