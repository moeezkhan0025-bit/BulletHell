using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// A special effect an arm or armament can carry (pierce, burn, ricochet, homing, ...). New effects are new subclasses
    /// and assets; the firing code only calls these hooks and never knows about specific effects.
    /// Effects are shared data assets: they must not hold run state. An effect that appears on several armaments of one
    /// arm is applied once, with the number of copies passed as stacks.
    /// </summary>
    public abstract class ArmEffect : ScriptableObject
    {
        [SerializeField] private string displayName;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>Adds this effect contribution to the per-projectile properties. Called when an arm loadout changes.</summary>
        public virtual void ModifyShot(ref ShotProperties shot, int stacks) { }

        /// <summary>A projectile from this arm just damaged a target.</summary>
        public virtual void OnHit(Collider2D target, float damage) { }

        /// <summary>A beam from this arm is hitting a target this frame (dt = frame time). Most effects ignore beams.</summary>
        public virtual void OnBeamHit(Collider2D target, float dt) { }

        /// <summary>
        /// Adds human-readable lines describing what this effect does with the given number of stacks. Generated from
        /// the effect own data so descriptions never go stale.
        /// </summary>
        public virtual void Describe(int stacks, List<string> lines) => lines.Add(DisplayName);

#if UNITY_EDITOR
        /// <summary>Editor-only: used by setup scripts and tests.</summary>
        public void SetDisplayName(string value)
        {
            displayName = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
