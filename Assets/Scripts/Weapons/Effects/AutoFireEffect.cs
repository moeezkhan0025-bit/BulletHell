using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// The arm also fires on its own at enemies inside its slot arc while it is NOT selected, at a fraction of its fire
    /// rate. Heat still applies. When the arm is selected it fires normally.
    /// </summary>
    [CreateAssetMenu(fileName = "Effect_AutoFire", menuName = "BulletHell/Effects/Auto-fire")]
    public sealed class AutoFireEffect : ArmEffect
    {
        [Tooltip("Fire-rate multiplier while auto-firing (0.5 = half the arm fire rate).")]
        [SerializeField, Range(0.05f, 1f)] private float rateMultiplier = 0.5f;
        [Tooltip("Half-width in degrees around the arm slot direction in which it will shoot (45 = a 90 degree cone).")]
        [SerializeField, Range(5f, 180f)] private float arc = 45f;

        public override void ModifyShot(ref ShotProperties shot, int stacks)
        {
            shot.AutoFireRate = Mathf.Max(shot.AutoFireRate, rateMultiplier);
            shot.AutoFireArc = Mathf.Max(shot.AutoFireArc, arc);
        }

        public override void Describe(int stacks, List<string> lines) =>
            lines.Add("Fires on its own at enemies in front of it (" + (arc * 2f).ToString("0") + " degree arc) at " +
                      (rateMultiplier * 100f).ToString("0") + "% fire rate while not selected");

#if UNITY_EDITOR
        public void Set(float rate, float halfArc)
        {
            rateMultiplier = rate;
            arc = halfArc;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
