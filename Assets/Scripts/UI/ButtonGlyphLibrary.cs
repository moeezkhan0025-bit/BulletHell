using System;
using BulletHell.Platform;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>The gamepad buttons the UI shows prompts for (by position, so they stay right on every controller family).</summary>
    public enum GlyphButton { South, East, West, North }

    /// <summary>One button picture: an icon, and an optional letter drawn over it (for sets whose icons are blank discs).</summary>
    [Serializable]
    public struct ButtonGlyph
    {
        public Sprite Icon;
        public string Label;
        public Color Tint;
        public Color LabelColor;
    }

    /// <summary>
    /// Button prompts for every controller family (PlayStation, Xbox, Nintendo, Touch). UI never hard-codes a button
    /// name: it asks for a position (South = Cross / A / B) and shows what this returns for the controller in use.
    /// New pictures are new sprites in the asset, not code.
    /// </summary>
    [CreateAssetMenu(fileName = "ButtonGlyphs", menuName = "BulletHell/Button Glyph Library")]
    public sealed class ButtonGlyphLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class FamilySet
        {
            public GlyphFamily Family;
            public ButtonGlyph South;
            public ButtonGlyph East;
            public ButtonGlyph West;
            public ButtonGlyph North;
            [Tooltip("Prompt text per UiAction, in enum order (Confirm, Back, Randomize, Details, Reroll, Remove, TabPrev, TabNext, Start). Empty = this device has no button for it, the prompt is left out.")]
            public string[] ActionLabels = new string[0];

            public string LabelFor(UiAction action)
            {
                int index = (int)action;
                return ActionLabels != null && index < ActionLabels.Length ? ActionLabels[index] : "";
            }

            public ButtonGlyph Get(GlyphButton button)
            {
                switch (button)
                {
                    case GlyphButton.East: return East;
                    case GlyphButton.West: return West;
                    case GlyphButton.North: return North;
                    default: return South;
                }
            }
        }

        [SerializeField] private FamilySet[] sets = new FamilySet[0];

        public FamilySet[] Sets => sets;

        /// <summary>The glyph of a button in a family; falls back to the first set, then to an empty glyph.</summary>
        public ButtonGlyph Get(GlyphFamily family, GlyphButton button)
        {
            for (int i = 0; i < sets.Length; i++)
                if (sets[i].Family == family)
                    return sets[i].Get(button);
            return sets.Length > 0 ? sets[0].Get(button) : default;
        }

        /// <summary>The prompt text for an action on a device family ("Cross", "A", "Esc"); empty when the device has no such button.</summary>
        public string LabelFor(GlyphFamily family, UiAction action)
        {
            for (int i = 0; i < sets.Length; i++)
                if (sets[i].Family == family)
                    return sets[i].LabelFor(action);
            return "";
        }

        /// <summary>The face button that equips ammo slot 0-3 (Cross/Circle/Square/Triangle = South/East/West/North).</summary>
        public static GlyphButton ForAmmoSlot(int slot)
        {
            switch (slot)
            {
                case 1: return GlyphButton.East;
                case 2: return GlyphButton.West;
                case 3: return GlyphButton.North;
                default: return GlyphButton.South;
            }
        }

#if UNITY_EDITOR
        public void SetSets(FamilySet[] newSets)
        {
            sets = newSets;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
