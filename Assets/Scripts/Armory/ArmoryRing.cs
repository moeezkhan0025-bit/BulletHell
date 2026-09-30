using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Cosmetics;
using BulletHell.Player;
using BulletHell.Weapons;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.Armory
{
    /// <summary>
    /// The left half of the Armory: the gladiator on a pedestal with the 8 arm slots on the flattened ellipse around its
    /// FEET (the same ArmRingTuning / ArmRingMath the game uses), drawn with uGUI Images. Arms on the back half of the ring are
    /// drawn behind the body, arms on the front half in front; back arms are a little smaller and darker. A selected arm gets
    /// a glow, and a selected back arm also gets a see-through copy on top of the body so it never disappears.
    /// The doll follows GameConfig.ShowCustomizationInGame like the player does (parts off: only the original body).
    /// </summary>
    public sealed class ArmoryRing : MonoBehaviour
    {
        private static readonly string[] SlotNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        [Header("Layout")]
        [Tooltip("The feet: the centre of the ring and the pedestal. Everything is positioned relative to it.")]
        [SerializeField] private RectTransform feet;
        [Tooltip("UI pixels for one world unit (the game body is 1.15 units tall).")]
        [SerializeField, Min(50f)] private float pixelsPerUnit = 400f;
        [SerializeField] private ArmRingTuning ring;
        [SerializeField] private PlayerData playerData;

        [Header("Layers (back to front)")]
        [SerializeField] private RectTransform backArms;
        [SerializeField] private RectTransform doll;
        [SerializeField] private RectTransform frontArms;
        [Tooltip("Accessory 2, Body, Armor, Head, Accessory 1 (same order as the doll prefab).")]
        [SerializeField] private Image[] dollLayers = new Image[5];

        [Header("Slots")]
        [SerializeField] private ArmorySlotButton[] slotButtons = new ArmorySlotButton[ArmLoadout.SlotCount];
        [Tooltip("Where each slot's arm attaches (the tether starts here). One per slot.")]
        [SerializeField] private RectTransform[] anchors = new RectTransform[ArmLoadout.SlotCount];

        [Header("Look")]
        [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.65f;
        [SerializeField, Min(1f)] private float glowScale = 1.16f;
        [SerializeField, Range(0f, 1f)] private float silhouetteAlpha = 0.5f;

        private readonly Image[] arms = new Image[ArmLoadout.SlotCount];
        private readonly Image[] glows = new Image[ArmLoadout.SlotCount];
        private readonly Image[] silhouettes = new Image[ArmLoadout.SlotCount];
        private bool built;
        private int selected = -1;

        public IReadOnlyList<ArmorySlotButton> Slots => slotButtons;
        public RectTransform Anchor(int slot) => anchors[slot];

        /// <summary>Position of a slot on the ring, in UI pixels relative to the feet.</summary>
        public Vector2 SlotPosition(int slot) => Ring.PositionAt(slot * ArmSelector.SliceDegrees) * pixelsPerUnit;

        private ArmRingTuning Ring => ring != null ? ring : ArmRingTuning.Fallback;

        private void Awake() => Build();

        private void Build()
        {
            if (built)
                return;
            built = true;
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                slotButtons[i].Slot = i;
                Vector2 position = SlotPosition(i);
                ((RectTransform)slotButtons[i].transform).anchoredPosition = position;
                anchors[i].anchoredPosition = position;
                glows[i] = NewImage("Glow" + i, backArms);
                arms[i] = NewImage("Arm" + i, backArms);
                silhouettes[i] = NewImage("Silhouette" + i, frontArms);
            }
            doll.anchoredPosition = new Vector2(0f, (playerData != null ? playerData.BodyCenterHeight : 0.437f) * pixelsPerUnit);
            BreatheForever();
        }

        private static Image NewImage(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.enabled = false;
            return image;
        }

        // The toolkit's idle breathing, as a slow scale yoyo on the doll (unscaled time: the game is paused here).
        private void BreatheForever()
        {
            Tween.Scale(doll, new Vector3(1.012f, 1.03f, 1f), 1.7f, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
        }

        private void OnEnable()
        {
            if (built)
            {
                Tween.StopAll(doll);
                doll.localScale = Vector3.one;
                BreatheForever();
            }
        }

        private void OnDisable() => Tween.StopAll(doll);

        /// <summary>Redraws the doll and every arm from the run state.</summary>
        public void Refresh(RunState state, ProfileService profile, bool showParts)
        {
            Build();
            RefreshDoll(profile, showParts);
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                ArmInstance arm = state.Loadout[i];
                slotButtons[i].Set(SlotNames[i], arm != null);
                PlaceArm(i, arm);
            }
            SetSelected(selected);
        }

        private void RefreshDoll(ProfileService profile, bool showParts)
        {
            // Layer order: Accessory 2, Body, Armor, Head, Accessory 1.
            CosmeticSlot[] slots = { CosmeticSlot.Accessory2, CosmeticSlot.Body, CosmeticSlot.Armor, CosmeticSlot.Head, CosmeticSlot.Accessory1 };
            for (int i = 0; i < dollLayers.Length && i < slots.Length; i++)
            {
                Sprite sprite;
                if (showParts)
                    sprite = profile.SpriteOf(slots[i]);
                else
                    sprite = slots[i] == CosmeticSlot.Body ? profile.DefaultSpriteOf(CosmeticSlot.Body) : null;
                Image layer = dollLayers[i];
                layer.enabled = sprite != null;
                layer.sprite = sprite;
                layer.preserveAspect = true;
                if (sprite != null)
                    layer.rectTransform.sizeDelta = sprite.rect.size / sprite.pixelsPerUnit * pixelsPerUnit;
            }
        }

        private void PlaceArm(int slot, ArmInstance instance)
        {
            Image arm = arms[slot];
            Image glow = glows[slot];
            Image silhouette = silhouettes[slot];
            Sprite sprite = instance != null ? instance.Data.Sprite : null;
            if (sprite == null)
            {
                arm.enabled = glow.enabled = silhouette.enabled = false;
                return;
            }

            float compass = slot * ArmSelector.SliceDegrees;
            bool back = ArmRingMath.IsBack(compass, Ring.BackDeadzone);
            float depth = ArmRingMath.Depth01(compass);
            float scale = Ring.ScaleAt(depth);
            float brightness = Ring.BrightnessAt(depth);
            WeaponArmData data = instance.Data;

            // Back arms live in the container behind the doll, front arms in the one in front.
            RectTransform parent = back ? backArms : frontArms;
            glow.transform.SetParent(parent, false);
            arm.transform.SetParent(parent, false);
            glow.transform.SetAsLastSibling();
            arm.transform.SetAsLastSibling();

            Vector2 size = sprite.rect.size / sprite.pixelsPerUnit * pixelsPerUnit;
            Vector2 pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            Vector2 position = SlotPosition(slot);
            Quaternion rotation = Quaternion.Euler(0f, 0f, 90f - compass + data.ArtRotation);

            Setup(arm, sprite, size, pivot, position, rotation, scale, new Color(brightness, brightness, brightness, 1f));
            Setup(glow, sprite, size, pivot, position, rotation, scale * glowScale, new Color(data.IdColor.r, data.IdColor.g, data.IdColor.b, glowAlpha));
            Setup(silhouette, sprite, size, pivot, position, rotation, scale, new Color(data.IdColor.r, data.IdColor.g, data.IdColor.b, silhouetteAlpha));
            glow.enabled = false;
            silhouette.enabled = false;
        }

        private static void Setup(Image image, Sprite sprite, Vector2 size, Vector2 pivot, Vector2 position, Quaternion rotation, float scale, Color color)
        {
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localRotation = rotation;
            rect.localScale = Vector3.one * scale;
            image.sprite = sprite;
            image.color = color;
            image.enabled = true;
        }

        /// <summary>Glows the selected arm (and shows its see-through copy over the body when it sits behind it). -1 clears.</summary>
        public void SetSelected(int slot)
        {
            selected = slot;
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                bool on = i == slot && arms[i].enabled;
                glows[i].enabled = on;
                bool back = ArmRingMath.IsBack(i * ArmSelector.SliceDegrees, Ring.BackDeadzone);
                silhouettes[i].enabled = on && back;
            }
        }
    }
}
