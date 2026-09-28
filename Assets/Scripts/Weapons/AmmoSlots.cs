using System;
using BulletHell.Input;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Player-side owner of the 4 ammo slots. Tapping a face button makes that slot's ammo active for every arm.
    /// Run start: slot 1 holds the starting ammo (Basic), the rest are empty.
    /// </summary>
    public sealed class AmmoSlots : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private AmmoTypeData startingAmmo;

        private readonly AmmoSlotSet set = new AmmoSlotSet();

        public AmmoTypeData Active => set.Active;
        public int ActiveIndex => set.ActiveIndex;
        public AmmoTypeData Get(int index) => set.Get(index);
        public bool Contains(AmmoTypeData ammo) => set.Contains(ammo);
        public bool HasEmptySlot => set.FirstEmpty() >= 0;

        /// <summary>Raised when a slot's contents change (pickup, replace) or the active slot changes.</summary>
        public event Action Changed;

        private void Awake()
        {
            if (startingAmmo != null)
                set.Set(0, startingAmmo);
        }

        private void OnEnable() => input.AmmoPressed += OnAmmoPressed;

        private void OnDisable() => input.AmmoPressed -= OnAmmoPressed;

        public bool TryAutoFill(AmmoTypeData ammo)
        {
            if (!set.TryAutoFill(ammo, out _))
                return false;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Replaces a slot's ammo and returns the ammo that was there.</summary>
        public AmmoTypeData Replace(int index, AmmoTypeData ammo)
        {
            AmmoTypeData old = set.Replace(index, ammo);
            Changed?.Invoke();
            return old;
        }

        private void OnAmmoPressed(int index)
        {
            if (set.TrySelect(index))
                Changed?.Invoke();
        }
    }
}
