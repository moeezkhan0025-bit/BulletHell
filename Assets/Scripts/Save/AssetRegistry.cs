using System.Collections.Generic;
using BulletHell.Cosmetics;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Save
{
    /// <summary>
    /// Maps the stable string IDs stored in saves to the WeaponArmData / ArmamentData / AmmoTypeData assets and back.
    /// An asset's Id is a field on the asset (not its file name), so renaming or moving a file never breaks a save.
    /// Fill the lists with the editor menu BulletHell/Collect Asset Registry.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetRegistry", menuName = "BulletHell/Asset Registry")]
    public sealed class AssetRegistry : ScriptableObject
    {
        [SerializeField] private WeaponArmData[] arms = new WeaponArmData[0];
        [SerializeField] private ArmamentData[] armaments = new ArmamentData[0];
        [SerializeField] private AmmoTypeData[] ammoTypes = new AmmoTypeData[0];
        [SerializeField] private CosmeticData[] cosmetics = new CosmeticData[0];

        private Dictionary<string, WeaponArmData> armsById;
        private Dictionary<string, ArmamentData> armamentsById;
        private Dictionary<string, AmmoTypeData> ammoById;
        private Dictionary<string, CosmeticData> cosmeticsById;

        public WeaponArmData GetArm(string id) => Find(ref armsById, arms, a => a.Id, id);
        public ArmamentData GetArmament(string id) => Find(ref armamentsById, armaments, a => a.Id, id);
        public AmmoTypeData GetAmmo(string id) => Find(ref ammoById, ammoTypes, a => a.Id, id);
        public CosmeticData GetCosmetic(string id) => Find(ref cosmeticsById, cosmetics, a => a.Id, id);

        /// <summary>Adds the cosmetics of one slot to the list, in registry order (the first is the slot default).</summary>
        public void GetCosmetics(CosmeticSlot slot, List<CosmeticData> into)
        {
            foreach (CosmeticData item in cosmetics)
                if (item != null && item.Slot == slot)
                    into.Add(item);
        }

        /// <summary>The ID to save for an asset, or "" for null.</summary>
        public static string IdOf(WeaponArmData arm) => arm != null ? arm.Id : "";
        public static string IdOf(ArmamentData armament) => armament != null ? armament.Id : "";
        public static string IdOf(AmmoTypeData ammo) => ammo != null ? ammo.Id : "";
        public static string IdOf(CosmeticData cosmetic) => cosmetic != null ? cosmetic.Id : "";

        private void OnEnable() => Invalidate();

        private void OnValidate() => Invalidate();

        private void Invalidate()
        {
            armsById = null;
            armamentsById = null;
            ammoById = null;
            cosmeticsById = null;
        }

        private T Find<T>(ref Dictionary<string, T> lookup, T[] source, System.Func<T, string> idOf, string id)
            where T : Object
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (lookup == null)
            {
                lookup = new Dictionary<string, T>(source.Length);
                foreach (T asset in source)
                {
                    if (asset == null)
                        continue;
                    string assetId = idOf(asset);
                    if (string.IsNullOrEmpty(assetId))
                        Debug.LogError($"AssetRegistry: {asset.name} has no Id.", asset);
                    else if (!lookup.TryAdd(assetId, asset))
                        Debug.LogError($"AssetRegistry: duplicate Id '{assetId}' on {asset.name}.", asset);
                }
            }

            return lookup.TryGetValue(id, out T found) ? found : null;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: replaces the cosmetics list.</summary>
        public void SetCosmetics(CosmeticData[] newCosmetics)
        {
            cosmetics = newCosmetics;
            Invalidate();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Editor-only: replaces the lists (used by the collect menu and tests).</summary>
        public void Set(WeaponArmData[] newArms, ArmamentData[] newArmaments, AmmoTypeData[] newAmmo)
        {
            arms = newArms;
            armaments = newArmaments;
            ammoTypes = newAmmo;
            Invalidate();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
