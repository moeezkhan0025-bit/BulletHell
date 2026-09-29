using BulletHell.Arena;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>A round's combat: its waves in order, and whether it is a boss round (a tougher placeholder until M7).</summary>
    [CreateAssetMenu(fileName = "Round_", menuName = "BulletHell/Round Data")]
    public sealed class RoundData : ScriptableObject
    {
        [SerializeField] private WaveData[] waves = new WaveData[0];
        [SerializeField] private bool isBossRound;
        [Tooltip("The colosseum layout this round is fought in. Empty = the GameConfig's default arena.")]
        [SerializeField] private ArenaData arena;
        [Tooltip("Seconds of calm between the end of one wave and the start of the next.")]
        [SerializeField, Min(0f)] private float waveBreatherSeconds = 2.5f;

        public WaveData[] Waves => waves;
        public bool IsBossRound => isBossRound;
        public ArenaData Arena => arena;
        public float WaveBreatherSeconds => waveBreatherSeconds;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by setup scripts and tests.</summary>
        public void SetArena(ArenaData newArena)
        {
            arena = newArena;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Editor-only: used by setup scripts and tests.</summary>
        public void Set(WaveData[] newWaves, bool boss, float breather)
        {
            waves = newWaves;
            isBossRound = boss;
            waveBreatherSeconds = breather;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
