using BulletHell.AI;
using BulletHell.Projectiles;
using BulletHell.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BulletHell.EditorTools
{
    /// <summary>
    /// One-shot M9a setup: the effect assets (Pierce, Ricochet, Homing, Auto-fire), the armament catalog with rarity, tags,
    /// price tier and max stacks (the five starters plus the earlier test armaments), the armament slot count of each arm,
    /// the registry, and the projectile pool's link to the enemy list (homing). Safe to run again.
    /// </summary>
    public static class M9aSetup
    {
        private const string EffectDir = "Assets/Data/Effects";
        private const string ArmamentDir = "Assets/Data/Armaments";
        private const string ArmDir = "Assets/Data/Arms";
        private const string GameScene = "Assets/Scenes/Game.unity";

        [MenuItem("BulletHell/M9a/Setup Everything")]
        public static void Run()
        {
            var pierce = Effect<PierceEffect>("Effect_Pierce", "Pierce", e => e.Set(1));
            var ricochet = Effect<RicochetEffect>("Effect_Ricochet", "Ricochet", e => e.Set(3, 1));
            var homing = Effect<HomingEffect>("Effect_Homing", "Homing", e => e.Set(120f, 60f, 60f, 30f, 9f));
            var autoFire = Effect<AutoFireEffect>("Effect_AutoFire", "Auto-fire", e => e.Set(0.5f, 45f));
            var burn = AssetDatabase.LoadAssetAtPath<BurnEffect>($"{EffectDir}/Effect_Burn.asset");
            var stun = AssetDatabase.LoadAssetAtPath<StunEffect>($"{EffectDir}/Effect_Stun.asset");

            // The five starters (Velocity is the old BulletSpeed armament, migrated; its ID is kept so saves still load).
            Armament("Armament_BulletSpeed", "Velocity", ArmamentRarity.Common, ArmamentTags.Speed, 1, 4, null,
                     new StatModifier(StatType.ProjectileSpeed, ModifierMode.Percent, 25f));
            Armament("Armament_Pierce", "Pierce", ArmamentRarity.Rare, ArmamentTags.Pierce, 2, 3, pierce);
            Armament("Armament_Ricochet", "Ricochet", ArmamentRarity.Epic, ArmamentTags.Bounce, 3, 3, ricochet);
            Armament("Armament_Homing", "Homing", ArmamentRarity.Rare, ArmamentTags.Homing, 2, 3, homing);
            Armament("Armament_AutoFire", "Auto-fire", ArmamentRarity.Epic, ArmamentTags.Auto, 3, 1, autoFire);

            // The earlier test armaments keep working, now with a rarity, tags and a stack limit.
            Meta("Armament_Damage", ArmamentRarity.Common, ArmamentTags.Damage, 1, 4);
            Meta("Armament_FireRate", ArmamentRarity.Common, ArmamentTags.FireRate, 1, 4);
            Meta("Armament_ExtraProjectile", ArmamentRarity.Rare, ArmamentTags.Damage, 2, 2);
            if (burn != null)
                Meta("Armament_Burn", ArmamentRarity.Rare, ArmamentTags.Status, 2, 2);
            if (stun != null)
                Meta("Armament_Stun", ArmamentRarity.Rare, ArmamentTags.Status, 2, 2);

            // Armament slots per arm: rarer arms get more.
            SetSlots("Arm_Red", 1);
            SetSlots("Arm_Blue", 2);
            SetSlots("Arm_Green", 2);
            SetSlots("Arm_Ember", 2);
            SetSlots("Arm_Purple", 3);
            SetSlots("Arm_Piercer", 3);

            AssetDatabase.SaveAssets();
            M4Setup.CollectRegistry();
            WirePool();
            AssetDatabase.SaveAssets();
            Debug.Log("M9a setup complete.");
        }

        private static T Effect<T>(string assetName, string displayName, System.Action<T> configure) where T : ArmEffect
        {
            string path = $"{EffectDir}/{assetName}.asset";
            var effect = AssetDatabase.LoadAssetAtPath<T>(path);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(effect, path);
            }
            configure(effect);
            effect.SetDisplayName(displayName);
            return effect;
        }

        private static void Armament(string assetName, string displayName, ArmamentRarity rarity, ArmamentTags tags, int tier, int maxStacks,
                                     ArmEffect effect, params StatModifier[] modifiers)
        {
            string path = $"{ArmamentDir}/{assetName}.asset";
            var armament = AssetDatabase.LoadAssetAtPath<ArmamentData>(path);
            if (armament == null)
            {
                armament = ScriptableObject.CreateInstance<ArmamentData>();
                AssetDatabase.CreateAsset(armament, path);
            }
            armament.Set(displayName, modifiers);
            armament.SetEffects(effect != null ? new[] { effect } : new ArmEffect[0]);
            armament.SetMeta(rarity, tags, tier, maxStacks);
        }

        private static void Meta(string assetName, ArmamentRarity rarity, ArmamentTags tags, int tier, int maxStacks)
        {
            var armament = AssetDatabase.LoadAssetAtPath<ArmamentData>($"{ArmamentDir}/{assetName}.asset");
            if (armament != null)
                armament.SetMeta(rarity, tags, tier, maxStacks);
        }

        private static void SetSlots(string assetName, int slots)
        {
            var arm = AssetDatabase.LoadAssetAtPath<WeaponArmData>($"{ArmDir}/{assetName}.asset");
            if (arm != null)
                arm.SetArmamentSlots(slots);
        }

        // Homing and auto-fire need the live enemy list: the projectile pool gets the navigation service.
        private static void WirePool()
        {
            EditorSceneManager.OpenScene(GameScene, OpenSceneMode.Single);
            var pool = Object.FindFirstObjectByType<ProjectilePool>(FindObjectsInactive.Include);
            var navigation = Object.FindFirstObjectByType<NavigationService>(FindObjectsInactive.Include);
            if (pool == null || navigation == null)
            {
                Debug.LogError("M9a: the Game scene has no ProjectilePool / NavigationService.");
                return;
            }
            var so = new SerializedObject(pool);
            so.FindProperty("navigation").objectReferenceValue = navigation;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
}
