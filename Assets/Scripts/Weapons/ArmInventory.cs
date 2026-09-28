using System;
using System.Collections.Generic;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Arms the player owns but has not placed in a loadout slot. An arm keeps its equipped armaments while it sits here.
    /// </summary>
    public sealed class ArmInventory
    {
        private readonly List<ArmInstance> items = new List<ArmInstance>();

        public int Count => items.Count;
        /// <summary>Increments on every change, so UI can tell when to refresh.</summary>
        public int Version { get; private set; }

        public event Action Changed;

        public ArmInstance Get(int index) => items[index];

        public bool Contains(ArmInstance arm) => arm != null && items.Contains(arm);

        public void Add(ArmInstance arm)
        {
            if (arm == null)
                return;
            items.Add(arm);
            NotifyChanged();
        }

        public bool Remove(ArmInstance arm)
        {
            if (arm == null || !items.Remove(arm))
                return false;
            NotifyChanged();
            return true;
        }

        private void NotifyChanged()
        {
            Version++;
            Changed?.Invoke();
        }
    }
}
