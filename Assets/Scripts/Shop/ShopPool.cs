using System;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Shop
{
    public enum ShopItemKind { Arm, Armament, Crate }

    /// <summary>One thing for sale: an arm, an armament or the crate, and its price.</summary>
    [Serializable]
    public struct ShopEntry
    {
        public ShopItemKind Kind;
        [Tooltip("Used when Kind is Arm.")]
        public WeaponArmData Arm;
        [Tooltip("Used when Kind is Armament.")]
        public ArmamentData Armament;
        [Min(0)] public int Price;

        public bool IsValid => Kind == ShopItemKind.Crate || (Kind == ShopItemKind.Arm ? Arm != null : Armament != null);

        public string Name
        {
            get
            {
                if (!IsValid)
                    return "?";
                switch (Kind)
                {
                    case ShopItemKind.Arm: return Arm.DisplayName;
                    case ShopItemKind.Armament: return Armament.DisplayName;
                    default: return "Harvest Crate";
                }
            }
        }

        /// <summary>Rarity of the item (the crate has none of its own: it uses the tuning's crate rarity).</summary>
        public ArmamentRarity Rarity(ShopTuning tuning) =>
            Kind == ShopItemKind.Arm ? Arm.Rarity : Kind == ShopItemKind.Armament ? Armament.Rarity : tuning != null ? tuning.CrateRarity : ArmamentRarity.Rare;
    }

    /// <summary>The candidates the Shop draws its random stock from (arms and armaments). Prices come from the RarityTable.</summary>
    [CreateAssetMenu(fileName = "ShopPool", menuName = "BulletHell/Shop Pool")]
    public sealed class ShopPool : ScriptableObject
    {
        [SerializeField] private WeaponArmData[] arms = new WeaponArmData[0];
        [SerializeField] private ArmamentData[] armaments = new ArmamentData[0];
        // The fixed price list from before the random stock (kept so old assets still load).
        [SerializeField, HideInInspector] private ShopEntry[] entries = new ShopEntry[0];

        public System.Collections.Generic.IReadOnlyList<WeaponArmData> Arms => arms;
        public System.Collections.Generic.IReadOnlyList<ArmamentData> Armaments => armaments;

#if UNITY_EDITOR
        /// <summary>Editor-only: sets the candidates (setup scripts and tests).</summary>
        public void Set(WeaponArmData[] newArms, ArmamentData[] newArmaments)
        {
            arms = newArms;
            armaments = newArmaments;
            entries = new ShopEntry[0];
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
