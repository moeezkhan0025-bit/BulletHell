using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>Which arm type sits in each of the 8 slots. Slot 0 = N, then clockwise. Empty slot = null.</summary>
    [CreateAssetMenu(fileName = "Loadout", menuName = "BulletHell/Arm Loadout")]
    public sealed class ArmLoadout : ScriptableObject
    {
        public const int SlotCount = 8;

        [Tooltip("N, NE, E, SE, S, SW, W, NW. The same arm type may fill several slots.")]
        [SerializeField] private WeaponArmData[] slots = new WeaponArmData[SlotCount];

        public WeaponArmData GetSlot(int slot) => slots[slot];

        public bool IsFilled(int slot) => slots[slot] != null;

        public void SetSlot(int slot, WeaponArmData arm) => slots[slot] = arm;

        private void OnValidate()
        {
            if (slots == null || slots.Length != SlotCount)
                System.Array.Resize(ref slots, SlotCount);
        }
    }
}
