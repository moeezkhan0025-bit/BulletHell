using System;
using System.Collections.Generic;

namespace BulletHell.Weapons
{
    /// <summary>Armaments the player owns but has not equipped. Duplicates are allowed (two copies of the same armament).</summary>
    public sealed class ArmamentInventory
    {
        private readonly List<ArmamentData> items = new List<ArmamentData>();

        public int Count => items.Count;
        /// <summary>Increments on every change, so UI can tell when to refresh.</summary>
        public int Version { get; private set; }

        public event Action Changed;

        public ArmamentData Get(int index) => items[index];

        public bool Contains(ArmamentData armament) => armament != null && items.Contains(armament);

        public void Add(ArmamentData armament)
        {
            if (armament == null)
                return;
            items.Add(armament);
            NotifyChanged();
        }

        /// <summary>Removes one copy. Returns false if there was none.</summary>
        public bool Remove(ArmamentData armament)
        {
            if (armament == null || !items.Remove(armament))
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
