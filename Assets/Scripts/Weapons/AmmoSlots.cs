using System;
using BulletHell.Core;
using BulletHell.Input;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Player-side owner of the 4 ammo slots. Tapping a face button makes that slot's ammo active for every arm.
    /// The slots live in the RunState (so they are saved); the starting ammo comes from the GameConfig.
    /// </summary>
    public sealed class AmmoSlots : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;

        private AmmoSlotSet set;

        public AmmoTypeData Active => set.Active;
        public int ActiveIndex => set.ActiveIndex;
        public AmmoTypeData Get(int index) => set.Get(index);
        public bool Contains(AmmoTypeData ammo) => set.Contains(ammo);
        public bool HasEmptySlot => set.FirstEmpty() >= 0;

        /// <summary>Raised when a slot's contents change (pickup, replace) or the active slot changes.</summary>
        public event Action Changed;

        private void Awake()
        {
            RunManager run = GameServices.Ensure().Run;
            run.EnsureRun();
            set = run.State.Ammo;
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
