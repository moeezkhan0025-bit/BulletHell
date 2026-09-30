using BulletHell.Core;
using BulletHell.Pickups;
using BulletHell.Platform;
using BulletHell.Player;
using BulletHell.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BulletHell.UI
{
    /// <summary>
    /// The combat HUD, drawn over the crowd and railing so it never covers play space. Bottom-left: portrait, hearts,
    /// heat bar of the selected arm. Bottom-right: the 4 ammo slots with their face-button glyphs. It only reads game
    /// state (health, arm selection, heat, ammo slots, pickup hold) and shows itself during the round intro, combat and
    /// pause. Button pictures come from the glyph library for the controller family in use.
    /// </summary>
    public sealed class CombatHud : MonoBehaviour
    {
        [Header("Game")]
        [SerializeField] private PlayerHealth health;
        [SerializeField] private ArmSelectionController arms;
        [SerializeField] private ArmFireController fire;
        [SerializeField] private AmmoSlots ammo;
        [SerializeField] private AmmoPickupCollector pickups;
        [Header("Parts")]
        [SerializeField] private GameObject content;
        [SerializeField] private HudPortrait portrait;
        [SerializeField] private HudHearts hearts;
        [SerializeField] private HudHeatBar heatBar;
        [SerializeField] private HudAmmoSlot[] slots = new HudAmmoSlot[AmmoSlotSet.Count];
        [SerializeField] private ButtonGlyphLibrary glyphs;

        private RunManager run;
        private int lastArm = ArmSelector.None;
        private GlyphFamily family = (GlyphFamily)(-1);

        private void Awake() => run = GameServices.Ensure().Run;

        private void OnEnable()
        {
            run.Machine.StateChanged += OnStateChanged;
            arms.SelectionChanged += OnSelectionChanged;
            OnStateChanged(GameState.None, run.Machine.Current);
            ApplyGlyphs(GlyphFamilyDetector.Current());
        }

        // The player's arms only exist after the player's own Awake, so the current selection is read here, not in OnEnable.
        private void Start()
        {
            if (arms.SelectedArm != ArmSelector.None)
                lastArm = arms.SelectedArm;
        }

        private void OnDisable()
        {
            run.Machine.StateChanged -= OnStateChanged;
            arms.SelectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged(int slot)
        {
            if (slot != ArmSelector.None)
                lastArm = slot;
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            // Hidden while paused too: the Pause panel and Settings (opened from it) own the screen, and their prompt lines sit over the HUD corners.
            bool show = to == GameState.RoundIntro || to == GameState.Combat;
            content.SetActive(show);
            if (to == GameState.RoundIntro)
                portrait.Apply(GameServices.Ensure().Profile);   // the look chosen on the customization screen
        }

        private void Update()
        {
            if (!content.activeSelf)
                return;

            ApplyGlyphs(GlyphFamilyDetector.Current());   // cheap: does nothing unless the device family changed

            hearts.Refresh(health.Current, health.Max);
            RefreshHeat();
            RefreshAmmo();
        }

        private void RefreshHeat()
        {
            bool selected = arms.SelectedArm != ArmSelector.None;
            int slot = selected ? arms.SelectedArm : lastArm;
            if (slot == ArmSelector.None || arms.GetArm(slot) == null)
            {
                heatBar.Refresh(0f, false, true, Color.gray);
                return;
            }

            HeatComponent heat = fire.GetHeat(slot);
            heatBar.Refresh(heat.Heat01, heat.IsOverheated, !selected, arms.GetArm(slot).Data.IdColor);
        }

        private void RefreshAmmo()
        {
            int holdSlot = pickups.HoldSlot;
            for (int i = 0; i < slots.Length; i++)
            {
                bool holding = holdSlot == i;
                slots[i].Refresh(ammo.Get(i), ammo.ActiveIndex == i, holding, holding ? pickups.HoldProgress01 : 0f);
            }
        }

        private void ApplyGlyphs(GlyphFamily newFamily)
        {
            if (newFamily == family || glyphs == null)
                return;
            family = newFamily;
            for (int i = 0; i < slots.Length; i++)
                slots[i].SetGlyph(glyphs.Get(family, ButtonGlyphLibrary.ForAmmoSlot(i)));
        }
    }
}
