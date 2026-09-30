using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// Debug-only: give yourself ANY armament and equip it on the selected arm during play.
    /// Next/Prev pick which armament of the whole catalog (the asset registry) Add uses. Add puts a copy in the inventory and
    /// equips it on the selected arm's first empty slot, or says why it cannot (no arm selected, slots full, stack limit).
    /// Remove moves the last equipped armament back to the inventory. Does nothing with no arm selected.
    /// </summary>
    public sealed class DebugArmamentControls : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private ArmSelectionController arms;
        [SerializeField] private PlayerInventory inventory;

        private int index;
        private IReadOnlyList<ArmamentData> catalog;

        public PlayerInventory Inventory => inventory;

        /// <summary>Why the last Add or Remove did nothing (or what it did). Empty at the start.</summary>
        public string Message { get; private set; } = "";

        /// <summary>Changes every time <see cref="Message"/> is set, so the overlay can tell it must redraw.</summary>
        public int MessageVersion { get; private set; }

        public int CatalogCount => Catalog.Count;

        /// <summary>The catalog armament Add will use, or null if there are none.</summary>
        public ArmamentData Current => CatalogCount > 0 ? Catalog[Mathf.Clamp(index, 0, CatalogCount - 1)] : null;

        /// <summary>1-based position of Current in the catalog, 0 when empty.</summary>
        public int CurrentPosition => CatalogCount > 0 ? Mathf.Clamp(index, 0, CatalogCount - 1) + 1 : 0;

        private IReadOnlyList<ArmamentData> Catalog
        {
            get
            {
                if (catalog == null)
                    catalog = GameServices.Ensure().Config.Registry.Armaments;
                return catalog;
            }
        }

        private void OnEnable()
        {
            input.DebugAddArmamentPressed += OnAdd;
            input.DebugRemoveArmamentPressed += OnRemove;
            input.DebugNextArmamentPressed += Next;
            input.DebugPrevArmamentPressed += Prev;
        }

        private void OnDisable()
        {
            input.DebugAddArmamentPressed -= OnAdd;
            input.DebugRemoveArmamentPressed -= OnRemove;
            input.DebugNextArmamentPressed -= Next;
            input.DebugPrevArmamentPressed -= Prev;
        }

        /// <summary>Gives the current catalog armament and equips it on the selected arm. Public so tests and tools can call it.</summary>
        public bool Add()
        {
            ArmInstance instance = arms.SelectedInstance;
            ArmamentData armament = Current;
            if (armament == null)
                return Say("no armaments in the registry");
            if (instance == null)
                return Say("select an arm first");

            int slot = -1;
            for (int i = 0; i < instance.SlotCount; i++)
                if (instance.GetArmament(i) == null)
                {
                    slot = i;
                    break;
                }
            if (slot < 0)
                return Say("all " + instance.SlotCount + (instance.SlotCount == 1 ? " slot is" : " slots are") + " full: remove one first");
            if (!instance.CanEquipAt(slot, armament, out string reason))
                return Say(reason);

            inventory.Armaments.Add(armament); // "give": the armament comes from the catalog, not the inventory
            bool equipped = instance.TryEquip(armament, inventory.Armaments);
            Say(equipped ? "equipped " + armament.DisplayName + " in slot " + (slot + 1) : "could not equip " + armament.DisplayName);
            return equipped;
        }

        public bool Remove()
        {
            ArmInstance instance = arms.SelectedInstance;
            if (instance == null)
                return Say("select an arm first");
            int slot = instance.LastFilledSlot;
            if (slot < 0)
                return Say("no armaments to remove");
            string name = instance.GetArmament(slot).DisplayName;
            instance.Unequip(slot, inventory.Armaments);
            Say("removed " + name + " (back in the inventory)");
            return true;
        }

        private void OnAdd() => Add();

        private void OnRemove() => Remove();

        private void Next() => Step(1);

        private void Prev() => Step(-1);

        private void Step(int direction)
        {
            int count = CatalogCount;
            if (count == 0)
                return;
            index = (Mathf.Clamp(index, 0, count - 1) + direction + count) % count;
            MessageVersion++;
        }

        // Records a message and returns false, so a refused Add can end with "return Say(reason)".
        private bool Say(string text)
        {
            Message = text;
            MessageVersion++;
            return false;
        }
    }
}
