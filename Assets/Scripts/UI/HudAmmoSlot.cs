using BulletHell.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>One ammo slot: icon, face-button glyph, active highlight, empty frame, and the hold-to-replace ring.</summary>
    public sealed class HudAmmoSlot : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private Image highlight;
        [SerializeField] private Image holdRing;
        [SerializeField] private Image glyph;
        [SerializeField] private Text glyphLabel;
        [SerializeField] private Color filledFrame = new Color(0.14f, 0.12f, 0.2f, 0.92f);
        [SerializeField] private Color emptyFrame = new Color(0.16f, 0.14f, 0.22f, 0.7f);
        [SerializeField, Min(1f)] private float activeScale = 1.12f;

        private AmmoTypeData shownAmmo;
        private bool shownActive;
        private bool shownHolding;
        private bool initialised;

        public void SetGlyph(ButtonGlyph value)
        {
            glyph.sprite = value.Icon;
            glyph.color = value.Tint;
            glyphLabel.text = value.Label ?? "";
            glyphLabel.color = value.LabelColor;
        }

        /// <param name="holdProgress">0..1 while this slot is being held to replace its ammo; ignored otherwise.</param>
        public void Refresh(AmmoTypeData ammo, bool active, bool holding, float holdProgress)
        {
            if (holding)
                holdRing.fillAmount = holdProgress;

            if (initialised && ammo == shownAmmo && active == shownActive && holding == shownHolding)
                return;
            initialised = true;
            shownAmmo = ammo;
            shownActive = active;
            shownHolding = holding;

            bool filled = ammo != null;
            frame.color = filled ? filledFrame : emptyFrame;
            icon.enabled = filled;
            if (filled)
            {
                icon.sprite = ammo.Icon;
                icon.color = ammo.Tint;
            }
            highlight.enabled = active && filled;
            holdRing.enabled = holding;
            transform.localScale = Vector3.one * (active && filled ? activeScale : 1f);
            glyph.canvasRenderer.SetAlpha(filled ? 1f : 0.55f);
        }
    }
}
