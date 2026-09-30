using System;
using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Weapons
{
    public enum ModifierMode { Flat, Percent }

    /// <summary>
    /// One stat change. Flat: value is added to the stat (+1 projectile, +2 shots/s).
    /// Percent: value is in percentage points and multiplies the stat (50 = x1.5, -20 = x0.8).
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType Stat;
        public ModifierMode Mode;
        public float Value;

        public StatModifier(StatType stat, ModifierMode mode, float value)
        {
            Stat = stat;
            Mode = mode;
            Value = value;
        }
    }

    /// <summary>An armament an arm instance can hold in one of its armament slots: stat modifiers plus special effects, with a rarity, tags and a stack limit.</summary>
    [CreateAssetMenu(fileName = "Armament_", menuName = "BulletHell/Armament Data")]
    public sealed class ArmamentData : ScriptableObject
    {
        [Tooltip("Stable ID used in save files. Never change it once players may have saves. Filled by BulletHell/Collect Asset Registry.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName = "Armament";
        [SerializeField] private Sprite icon;
        [SerializeField] private ArmamentRarity rarity = ArmamentRarity.Common;
        [Tooltip("Shop price tier 1-5 (prices scale with rarity and round in the Shop, M9b).")]
        [SerializeField, Range(1, 5)] private int priceTier = 1;
        [SerializeField] private ArmamentTags tags;
        [Tooltip("Most copies of this armament one arm can carry.")]
        [SerializeField, Min(1)] private int maxStacks = 3;
        [SerializeField] private StatModifier[] modifiers = Array.Empty<StatModifier>();
        [Tooltip("Special effects (pierce, burn, ...) this armament adds to the arm it is equipped on.")]
        [SerializeField] private ArmEffect[] effects = Array.Empty<ArmEffect>();

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public ArmamentRarity Rarity => rarity;
        public int PriceTier => priceTier;
        public ArmamentTags Tags => tags;
        public int MaxStacks => maxStacks;
        public IReadOnlyList<StatModifier> Modifiers => modifiers;
        public IReadOnlyList<ArmEffect> Effects => effects ?? Array.Empty<ArmEffect>();

#if UNITY_EDITOR
        /// <summary>Editor-only: assigns the save ID.</summary>
        public void SetId(string value)
        {
            id = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Editor-only: used by setup scripts and tests to fill in an armament.</summary>
        public void Set(string name, params StatModifier[] newModifiers)
        {
            displayName = name;
            modifiers = newModifiers;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Editor-only: sets rarity, tags, price tier and max stacks.</summary>
        public void SetMeta(ArmamentRarity newRarity, ArmamentTags newTags, int newPriceTier, int newMaxStacks)
        {
            rarity = newRarity;
            tags = newTags;
            priceTier = newPriceTier;
            maxStacks = newMaxStacks;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Editor-only: sets the special effects.</summary>
        public void SetEffects(params ArmEffect[] newEffects)
        {
            effects = newEffects;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
