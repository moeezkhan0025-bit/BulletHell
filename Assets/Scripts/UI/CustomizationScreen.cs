using System;
using BulletHell.Core;
using BulletHell.Cosmetics;
using BulletHell.Input;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Character Creation: one row per paper-doll slot (left/right cycles its variants), a live preview of
    /// the gladiator, Randomize (Square) and "To the Arena!" (Confirm). Edits show in the preview at once; Confirm writes the profile file and
    /// Back throws the edits away. Cosmetics are purely visual.
    /// </summary>
    public sealed class CustomizationScreen : MonoBehaviour
    {
        [SerializeField] private SettingRow rowPrefab;
        [SerializeField] private Transform rowParent;
        [Tooltip("Square / West randomizes while this screen is open.")]
        [SerializeField] private MenuInputReader menuInput;
        [SerializeField] private Button randomizeButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;
        [Tooltip("The whole preview (doll, arms, pedestal), shown while this screen is open.")]
        [SerializeField] private GameObject previewRoot;
        [Tooltip("The paper doll inside the preview; the look is applied to it.")]
        [SerializeField] private GladiatorCosmetics preview;

        private readonly SettingRow[] rows = new SettingRow[CosmeticSlots.Count];
        private ProfileService profile;
        private Action onConfirmed;
        private Action onBack;
        private bool built;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Shows the screen with the saved look. Confirmed runs after the profile was saved; back after the edits were dropped.</summary>
        public void Open(Action confirmed, Action back)
        {
            gameObject.SetActive(true);
            EnsureBuilt();
            onConfirmed = confirmed;
            onBack = back;

            profile.Revert();
            previewRoot.SetActive(true);
            if (menuInput != null)
                menuInput.RandomizePressed += Randomize;
            Refresh();
            UIFocusGuard.Focus(rows[0].gameObject);
        }

        private void OnDestroy()
        {
            if (profile != null)
                profile.Changed -= Refresh;
        }

        private void EnsureBuilt()
        {
            if (built)
                return;
            built = true;

            profile = GameServices.Ensure().Profile;
            profile.Changed += Refresh;

            for (int i = 0; i < CosmeticSlots.Count; i++)
            {
                var slot = (CosmeticSlot)i;
                SettingRow row = Instantiate(rowPrefab, rowParent);
                row.Bind(CosmeticSlots.Label(slot), () => NameOf(slot), dir => profile.Cycle(slot, dir));
                row.Cancelled += Back;
                row.transform.SetSiblingIndex(i);
                rows[i] = row;
            }

            randomizeButton.onClick.AddListener(Randomize);
            confirmButton.onClick.AddListener(Confirm);
            backButton.onClick.AddListener(Back);
            foreach (Button button in new[] { randomizeButton, confirmButton, backButton })
                if (button.TryGetComponent(out CancelRelay relay))
                    relay.Cancelled += Back;
        }

        private string NameOf(CosmeticSlot slot)
        {
            CosmeticPartData item = profile.Get(slot);
            return item != null ? item.DisplayName : "-";
        }

        private void Refresh()
        {
            if (!built)
                return;
            foreach (SettingRow row in rows)
                if (row != null)
                    row.Refresh();
            preview.Apply(profile);
        }

        private void Randomize() => profile.Randomize();

        private void Confirm()
        {
            profile.Save();
            Close();
            onConfirmed?.Invoke();
        }

        private void Back()
        {
            if (!IsOpen)
                return;
            profile.Revert();
            Close();
            onBack?.Invoke();
        }

        private void Close()
        {
            if (menuInput != null)
                menuInput.RandomizePressed -= Randomize;
            previewRoot.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
