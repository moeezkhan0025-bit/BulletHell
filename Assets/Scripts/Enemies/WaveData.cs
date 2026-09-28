using System;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>Where a group of enemies appears.</summary>
    public enum SpawnPattern
    {
        /// <summary>Random spots in the arena, away from the player.</summary>
        Scatter,
        /// <summary>Evenly spaced along the top of the arena.</summary>
        Row,
        /// <summary>Evenly spaced on a circle around the arena centre.</summary>
        Ring,
    }

    /// <summary>A batch of one enemy type inside a wave: how many, where, and how fast they arrive.</summary>
    [Serializable]
    public struct SpawnGroup
    {
        public EnemyData Enemy;
        [Min(1)] public int Count;
        public SpawnPattern Pattern;
        [Tooltip("Seconds after the wave starts before the first enemy of this group appears.")]
        [Min(0f)] public float Delay;
        [Tooltip("Seconds between enemies of this group. 0 = all at once.")]
        [Min(0f)] public float Interval;
    }

    /// <summary>One wave of a round. It is cleared when every enemy in it has appeared and been killed.</summary>
    [CreateAssetMenu(fileName = "Wave_", menuName = "BulletHell/Wave Data")]
    public sealed class WaveData : ScriptableObject
    {
        [SerializeField] private SpawnGroup[] groups = new SpawnGroup[0];

        public SpawnGroup[] Groups => groups;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by setup scripts and tests.</summary>
        public void Set(params SpawnGroup[] newGroups)
        {
            groups = newGroups;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
