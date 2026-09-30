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
        [Tooltip("What stands in the colosseum this round (obstacles, traps, spawn, gates). Empty = the GameConfig's default layout.")]
        [SerializeField] private ArenaLayoutData layout;
        [Tooltip("The most hazards and obstacles this round's layout may hold. Checked by the layout validator.")]
        [SerializeField] private HazardBudget hazardBudget;
        [Tooltip("Seconds of calm between the end of one wave and the start of the next.")]
        [SerializeField, Min(0f)] private float waveBreatherSeconds = 2.5f;

        public WaveData[] Waves => waves;
        public bool IsBossRound => isBossRound;
        public ArenaLayoutData Layout => layout;
        public HazardBudget HazardBudget => hazardBudget;
        public float WaveBreatherSeconds => waveBreatherSeconds;

        /// <summary>The boss fought this round (the first wave group with a BossData), or null.</summary>
        public BulletHell.Bosses.BossData FindBoss()
        {
            for (int i = 0; i < waves.Length; i++)
            {
                BulletHell.Bosses.BossData boss = waves[i] != null ? waves[i].FindBoss() : null;
                if (boss != null)
                    return boss;
            }
            return null;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: used by setup scripts and tests.</summary>
        public void SetLayout(ArenaLayoutData newLayout, HazardBudget budget)
        {
            layout = newLayout;
            hazardBudget = budget;
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
