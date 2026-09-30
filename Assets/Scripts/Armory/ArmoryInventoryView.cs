using System.Collections.Generic;
using BulletHell.Weapons;

namespace BulletHell.Armory
{
    /// <summary>One armament in the inventory with how many copies the player holds.</summary>
    public struct ArmamentStack
    {
        public ArmamentData Armament;
        public int Count;
    }

    /// <summary>The inventory as the Armory's card grid shows it.</summary>
    public static class ArmoryInventoryView
    {
        /// <summary>Distinct armaments with their counts, best rarity first (then by name), so the grid order is stable.</summary>
        public static List<ArmamentStack> Group(ArmamentInventory inventory)
        {
            var stacks = new List<ArmamentStack>();
            for (int i = 0; i < inventory.Count; i++)
            {
                ArmamentData armament = inventory.Get(i);
                if (armament == null)
                    continue;
                int index = stacks.FindIndex(s => s.Armament == armament);
                if (index >= 0)
                    stacks[index] = new ArmamentStack { Armament = armament, Count = stacks[index].Count + 1 };
                else
                    stacks.Add(new ArmamentStack { Armament = armament, Count = 1 });
            }
            stacks.Sort((a, b) =>
            {
                int byRarity = b.Armament.Rarity.CompareTo(a.Armament.Rarity);
                return byRarity != 0 ? byRarity : string.CompareOrdinal(a.Armament.DisplayName, b.Armament.DisplayName);
            });
            return stacks;
        }

        /// <summary>The spare arms in the arm inventory, in inventory order.</summary>
        public static List<ArmInstance> SpareArms(ArmInventory inventory)
        {
            var arms = new List<ArmInstance>();
            for (int i = 0; i < inventory.Count; i++)
                arms.Add(inventory.Get(i));
            return arms;
        }
    }
}
