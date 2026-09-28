namespace BulletHell.Weapons
{
    /// <summary>
    /// The player's 4 ammo slots (Cross / Circle / Square / Triangle) and which one is active.
    /// Plain logic so it can be unit tested; AmmoSlots is the MonoBehaviour wrapper.
    /// </summary>
    public sealed class AmmoSlotSet
    {
        public const int Count = 4;

        private readonly AmmoTypeData[] slots = new AmmoTypeData[Count];

        public int ActiveIndex { get; private set; } = -1;
        public AmmoTypeData Active => ActiveIndex < 0 ? null : slots[ActiveIndex];

        public AmmoTypeData Get(int index) => slots[index];

        public bool Contains(AmmoTypeData ammo)
        {
            for (int i = 0; i < Count; i++)
                if (slots[i] == ammo && ammo != null)
                    return true;
            return false;
        }

        public int FirstEmpty()
        {
            for (int i = 0; i < Count; i++)
                if (slots[i] == null)
                    return i;
            return -1;
        }

        /// <summary>Sets a slot directly (run start). Activates it if nothing is active yet.</summary>
        public void Set(int index, AmmoTypeData ammo)
        {
            slots[index] = ammo;
            if (ActiveIndex < 0 && ammo != null)
                ActiveIndex = index;
        }

        /// <summary>Makes a slot active. Empty slots can't be selected. Returns true if the active slot changed.</summary>
        public bool TrySelect(int index)
        {
            if (index < 0 || index >= Count || slots[index] == null || index == ActiveIndex)
                return false;
            ActiveIndex = index;
            return true;
        }

        /// <summary>Puts ammo in the first empty slot. Does nothing if all slots are full or the type is already carried.</summary>
        public bool TryAutoFill(AmmoTypeData ammo, out int filledIndex)
        {
            filledIndex = -1;
            if (ammo == null || Contains(ammo))
                return false;
            filledIndex = FirstEmpty();
            if (filledIndex < 0)
                return false;
            Set(filledIndex, ammo);
            return true;
        }

        /// <summary>Swaps a slot's ammo and returns the ammo that was there (null if it was empty).</summary>
        public AmmoTypeData Replace(int index, AmmoTypeData ammo)
        {
            AmmoTypeData old = slots[index];
            slots[index] = ammo;
            if (ActiveIndex < 0 && ammo != null)
                ActiveIndex = index;
            return old;
        }
    }
}
