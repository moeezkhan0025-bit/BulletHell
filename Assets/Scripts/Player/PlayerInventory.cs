using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// The player's run inventories: armaments and spare arms that aren't equipped. Seeded from the lists below for
    /// now; the M4 run state replaces the seeding (new run = starting stock, Continue = loaded from the save).
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Tooltip("Armaments in the inventory at start (duplicates allowed).")]
        [SerializeField] private ArmamentData[] startingArmaments;
        [Tooltip("Spare arms in the arm inventory at start.")]
        [SerializeField] private WeaponArmData[] startingSpareArms;

        public ArmamentInventory Armaments { get; } = new ArmamentInventory();
        public ArmInventory Arms { get; } = new ArmInventory();

        private void Awake()
        {
            if (startingArmaments != null)
                foreach (ArmamentData armament in startingArmaments)
                    Armaments.Add(armament);
            if (startingSpareArms != null)
                foreach (WeaponArmData arm in startingSpareArms)
                    if (arm != null)
                        Arms.Add(new ArmInstance(arm));
        }
    }
}
