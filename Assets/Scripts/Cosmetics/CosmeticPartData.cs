using UnityEngine;

namespace BulletHell.Cosmetics
{
    /// <summary>The paper-doll slots, listed back to front. The order is the profile file's layout: append, never reorder.</summary>
    public enum CosmeticSlot { Body, Armor, Head, Accessory1, Accessory2 }

    public static class CosmeticSlots
    {
        public const int Count = 5;

        public static string Label(CosmeticSlot slot)
        {
            switch (slot)
            {
                case CosmeticSlot.Body: return "Body";
                case CosmeticSlot.Armor: return "Armor";
                case CosmeticSlot.Head: return "Head";
                case CosmeticSlot.Accessory1: return "Accessory 1 (head)";
                case CosmeticSlot.Accessory2: return "Accessory 2 (back)";
                default: return slot.ToString();
            }
        }

        /// <summary>The Sprite Library category that holds a slot's variants.</summary>
        public static string Category(CosmeticSlot slot) => slot.ToString();
    }

    /// <summary>
    /// One variant of one paper-doll slot. Purely visual, never changes stats. The art itself lives in the Sprite Library
    /// asset (category = slot, label = <see cref="Label"/>), so a new part is a new library entry plus a new asset of this
    /// type, not code. The first part of each slot in the registry is that slot's default.
    /// </summary>
    [CreateAssetMenu(fileName = "Part", menuName = "BulletHell/Cosmetic Part")]
    public sealed class CosmeticPartData : ScriptableObject
    {
        [Tooltip("Stable ID stored in the profile file. Never change it once players have saved it.")]
        [SerializeField] private string id;
        [SerializeField] private CosmeticSlot slot;
        [SerializeField] private string displayName = "Part";
        [Tooltip("The label of this variant inside the slot's Sprite Library category.")]
        [SerializeField] private string label;

        public string Id => id;
        public CosmeticSlot Slot => slot;
        public string DisplayName => displayName;
        public string Label => label;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the setup script and tests.</summary>
        public void Configure(string newId, CosmeticSlot newSlot, string newName, string newLabel)
        {
            id = newId;
            slot = newSlot;
            displayName = newName;
            label = newLabel;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
