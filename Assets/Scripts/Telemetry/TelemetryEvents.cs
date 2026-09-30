using System;
using UnityEngine;

namespace BulletHell.Telemetry
{
    public enum SpendKind { Item, Reroll, Crate }

    public enum ItemKind { Arm, Armament }

    /// <summary>
    /// What the playtest telemetry listens to. The game raises these where things happen (a hit on the player, a purchase); the
    /// <see cref="TelemetryService"/> turns them into the run's numbers. Static so the shop and the damage code need no reference to
    /// the service, and cleared at the start of every Play session (domain reload can be off).
    /// </summary>
    public static class TelemetryEvents
    {
        /// <summary>A hit landed on the player: health lost and what did it ("Tomato (contact)", "Pumpking smash", "Enemy bullet").</summary>
        public static event Action<float, string> PlayerDamaged;
        /// <summary>Currency was paid in the shop.</summary>
        public static event Action<int, SpendKind> Spent;
        /// <summary>An arm or an armament entered the inventory by purchase (a crate counts when its choice is taken).</summary>
        public static event Action<ItemKind, string> ItemAcquired;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PlayerDamaged = null;
            Spent = null;
            ItemAcquired = null;
        }

        public static void RaisePlayerDamaged(float amount, string source) => PlayerDamaged?.Invoke(amount, source);
        public static void RaiseSpent(int amount, SpendKind kind) => Spent?.Invoke(amount, kind);
        public static void RaiseItemAcquired(ItemKind kind, string name) => ItemAcquired?.Invoke(kind, name);
    }
}
