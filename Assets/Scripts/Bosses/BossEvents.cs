using System;
using UnityEngine;

namespace BulletHell.Bosses
{
    /// <summary>
    /// Static hooks for the one boss on the field: the boss health bar, the debug options and the overlay listen
    /// here instead of searching the scene. Reset on load because Reload Domain is off.
    /// </summary>
    public static class BossEvents
    {
        public static event Action<BossController> Spawned;
        public static event Action<BossController> PhaseChanged;
        /// <summary>Raised when the death sequence has finished (the boss is about to leave the field).</summary>
        public static event Action<BossController> Defeated;

        /// <summary>The boss currently alive, or null.</summary>
        public static BossController Active { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Spawned = null;
            PhaseChanged = null;
            Defeated = null;
            Active = null;
        }

        internal static void RaiseSpawned(BossController boss)
        {
            Active = boss;
            Spawned?.Invoke(boss);
        }

        internal static void RaisePhaseChanged(BossController boss) => PhaseChanged?.Invoke(boss);

        internal static void RaiseDefeated(BossController boss)
        {
            if (Active == boss)
                Active = null;
            Defeated?.Invoke(boss);
        }

        /// <summary>The boss left the field without dying (round skipped, recycled).</summary>
        internal static void Clear(BossController boss)
        {
            if (Active == boss)
                Active = null;
        }
    }
}
