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

    /// <summary>An armament an arm instance can hold in one of its armament slots: a list of stat modifiers.</summary>
    [CreateAssetMenu(fileName = "Armament_", menuName = "BulletHell/Armament Data")]
    public sealed class ArmamentData : ScriptableObject
    {
        [SerializeField] private string displayName = "Armament";
        [SerializeField] private StatModifier[] modifiers = Array.Empty<StatModifier>();
        [Tooltip("Special effects (pierce, burn, ...) this armament adds to the arm it is equipped on.")]
        [SerializeField] private ArmEffect[] effects = Array.Empty<ArmEffect>();

        public string DisplayName => displayName;
        public IReadOnlyList<StatModifier> Modifiers => modifiers;
        public IReadOnlyList<ArmEffect> Effects => effects ?? Array.Empty<ArmEffect>();

#if UNITY_EDITOR
        /// <summary>Editor-only: used by setup scripts and tests to fill in an armament.</summary>
        public void Set(string name, params StatModifier[] newModifiers)
        {
            displayName = name;
            modifiers = newModifiers;
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
