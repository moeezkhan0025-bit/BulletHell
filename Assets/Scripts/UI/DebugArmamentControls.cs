using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// Debug-only: equips and unequips armaments on the selected arm during play, through the armament inventory.
    /// Next/Prev pick which inventory item Add uses, Add moves it onto the first empty armament slot, Remove moves the
    /// last equipped armament back to the inventory. Does nothing with no arm selected.
    /// </summary>
    public sealed class DebugArmamentControls : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private ArmSelectionController arms;
        [SerializeField] private PlayerInventory inventory;

        private int index;

        public PlayerInventory Inventory => inventory;

        /// <summary>The inventory armament Add will use, or null if the inventory is empty.</summary>
        public ArmamentData Current => Count > 0 ? inventory.Armaments.Get(Mathf.Clamp(index, 0, Count - 1)) : null;

        /// <summary>1-based position of Current in the inventory, 0 when empty.</summary>
        public int CurrentPosition => Count > 0 ? Mathf.Clamp(index, 0, Count - 1) + 1 : 0;

        private int Count => inventory.Armaments.Count;

        private void OnEnable()
        {
            input.DebugAddArmamentPressed += Add;
            input.DebugRemoveArmamentPressed += Remove;
            input.DebugNextArmamentPressed += Next;
            input.DebugPrevArmamentPressed += Prev;
        }

        private void OnDisable()
        {
            input.DebugAddArmamentPressed -= Add;
            input.DebugRemoveArmamentPressed -= Remove;
            input.DebugNextArmamentPressed -= Next;
            input.DebugPrevArmamentPressed -= Prev;
        }

        private void Add() => arms.SelectedInstance?.TryEquip(Current, inventory.Armaments);

        private void Remove()
        {
            ArmInstance instance = arms.SelectedInstance;
            if (instance != null && instance.LastFilledSlot >= 0)
                instance.Unequip(instance.LastFilledSlot, inventory.Armaments);
        }

        private void Next() => Step(1);

        private void Prev() => Step(-1);

        private void Step(int direction)
        {
            if (Count == 0)
                return;
            index = (Mathf.Clamp(index, 0, Count - 1) + direction + Count) % Count;
        }
    }
}
