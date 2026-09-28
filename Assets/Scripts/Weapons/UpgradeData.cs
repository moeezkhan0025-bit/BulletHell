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

    /// <summary>An upgrade an arm instance can hold in one of its upgrade slots: a list of stat modifiers.</summary>
    [CreateAssetMenu(fileName = "Upg_", menuName = "BulletHell/Upgrade Data")]
    public sealed class UpgradeData : ScriptableObject
    {
        [SerializeField] private string displayName = "Upgrade";
        [SerializeField] private StatModifier[] modifiers = Array.Empty<StatModifier>();

        public string DisplayName => displayName;
        public IReadOnlyList<StatModifier> Modifiers => modifiers;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by setup scripts and tests to fill in an upgrade.</summary>
        public void Set(string name, params StatModifier[] newModifiers)
        {
            displayName = name;
            modifiers = newModifiers;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
