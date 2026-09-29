using UnityEngine;

namespace BulletHell.Cosmetics
{
    public enum CosmeticSlot { CandyCoating, Headgear, Cape, ArmTint }

    public static class CosmeticSlots
    {
        public const int Count = 4;

        public static string Label(CosmeticSlot slot)
        {
            switch (slot)
            {
                case CosmeticSlot.CandyCoating: return "Candy coating";
                case CosmeticSlot.Headgear: return "Headgear";
                case CosmeticSlot.Cape: return "Cape / trail";
                case CosmeticSlot.ArmTint: return "Arm tint";
                default: return slot.ToString();
            }
        }
    }

    /// <summary>
    /// One cosmetic item for one slot. Purely visual, never changes stats. A tint colours the slot's layer; an optional
    /// sprite is the layer's art (headgear, cape). New items are new assets, not code. The first item of each slot in the
    /// registry is that slot's default.
    /// </summary>
    [CreateAssetMenu(fileName = "Cosmetic", menuName = "BulletHell/Cosmetic")]
    public sealed class CosmeticData : ScriptableObject
    {
        [Tooltip("Stable ID stored in the profile file. Never change it once players have saved it.")]
        [SerializeField] private string id;
        [SerializeField] private CosmeticSlot slot;
        [SerializeField] private string displayName = "Cosmetic";
        [SerializeField] private Color tint = Color.white;
        [Tooltip("Layer art for headgear and cape. Empty = nothing drawn (the 'none' item).")]
        [SerializeField] private Sprite sprite;

        public string Id => id;
        public CosmeticSlot Slot => slot;
        public string DisplayName => displayName;
        public Color Tint => tint;
        public Sprite Sprite => sprite;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the setup script and tests.</summary>
        public void Configure(string newId, CosmeticSlot newSlot, string newName, Color newTint, Sprite newSprite)
        {
            id = newId;
            slot = newSlot;
            displayName = newName;
            tint = newTint;
            sprite = newSprite;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
