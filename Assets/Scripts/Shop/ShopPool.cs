using System;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Shop
{
    public enum ShopItemKind { Arm, Armament }

    /// <summary>One thing for sale: an arm or an armament, and its price.</summary>
    [Serializable]
    public struct ShopEntry
    {
        public ShopItemKind Kind;
        [Tooltip("Used when Kind is Arm.")]
        public WeaponArmData Arm;
        [Tooltip("Used when Kind is Armament.")]
        public ArmamentData Armament;
        [Min(0)] public int Price;

        public bool IsValid => Kind == ShopItemKind.Arm ? Arm != null : Armament != null;
        public string Name => !IsValid ? "?" : Kind == ShopItemKind.Arm ? Arm.DisplayName : Armament.DisplayName;
    }

    /// <summary>What the Shop offers. For now a fixed list (the same every visit); random stock and scaling prices come in M6.</summary>
    [CreateAssetMenu(fileName = "ShopPool", menuName = "BulletHell/Shop Pool")]
    public sealed class ShopPool : ScriptableObject
    {
        [SerializeField] private ShopEntry[] entries = new ShopEntry[0];

        public int Count => entries.Length;
        public ShopEntry Get(int index) => entries[index];
    }
}
