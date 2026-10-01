using BulletHell.Core;
using TMPro;
using BulletHell.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Main Menu: New Game, Continue (disabled without a run save), Settings and Quit.
    /// New Game asks before replacing an existing run save, then opens the Gladiator customization; confirming the look
    /// starts the run (and only then deletes the old save, so backing out never loses it). Continue skips customization:
    /// the look comes from the profile file. Fully navigable with the stick / D-pad; the confirm dialogs (new game over a save, quit) open with Cancel selected.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject menuButtons;
        [Tooltip("The game title: shown with the menu buttons, hidden while Settings or customization is open.")]
        [SerializeField] private GameObject title;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text messageText;
        [Tooltip("Bottom-right version line; filled at run time from the build number (BuildInfo).")]
        [SerializeField] private TMP_Text versionLabel;
        [Tooltip("Optional: the button prompt line at the bottom.")]
        [SerializeField] private TMP_Text hintLabel;
        [Header("Confirmation")]
        [SerializeField] private ConfirmDialog confirmDialog;
        [Header("Screens")]
        [SerializeField] private SettingsScreen settingsScreen;
        [SerializeField] private CustomizationScreen customizationScreen;

        private GameServices services;
        private bool replacesSave;

        private void Awake()
        {
            services = GameServices.Ensure();
            newGameButton.onClick.AddListener(OnNewGamePressed);
            continueButton.onClick.AddListener(OnContinuePressed);
            settingsButton.onClick.AddListener(OnSettingsPressed);
            quitButton.onClick.AddListener(OnQuitPressed);
            quitButton.gameObject.SetActive(PlatformCapabilities.CanQuitApplication);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            messageText.text = "";
            BulletHell.Core.GameServices.Ensure().Audio.PlayMusic(BulletHell.Audio.MusicContext.Menu);
            if (versionLabel != null)
                versionLabel.text = BulletHell.Core.BuildInfo.MenuLine;
            ShowMenu();
        }

        private void OnNewGamePressed()
        {
            if (!services.Save.HasSave)
            {
                replacesSave = false;
                OpenCustomization();
                return;
            }

            SetMenuInteractable(false);
            confirmDialog.Ask("Start a new run?",
                "You have a run in progress. It is replaced once you confirm your gladiator; backing out keeps it.",
                "New Game", "Cancel", OnOverwriteConfirmed, CloseConfirm);
        }

        private void OnOverwriteConfirmed()
        {
            replacesSave = true;
            OpenCustomization();
        }

        private void CloseConfirm()
        {
            ShowMenu();
        }

        private void OpenCustomization()
        {
            SetMenuVisible(false);
            customizationScreen.Open(OnLookConfirmed, ShowMenu);
        }

        // The look is saved: now the old run (if any) really is replaced.
        private void OnLookConfirmed()
        {
            if (replacesSave)
                services.Save.Delete();
            services.Run.StartNewRun();
            services.Scenes.Load(SceneLoader.Game);
        }

        private void OnSettingsPressed()
        {
            SetMenuVisible(false);
            settingsScreen.Open(ShowMenu);
        }

        private void OnContinuePressed()
        {
            if (services.Run.ContinueRun())
            {
                services.Scenes.Load(SceneLoader.Game);
                return;
            }

            messageText.text = "The save could not be loaded.";
            SetMenuInteractable(true);
            UIFocusGuard.Focus(newGameButton.gameObject);
        }

        private void OnQuitPressed()
        {
            confirmDialog.Ask("Quit the game?", "Your run save stays where it is.", "Quit", "Cancel", QuitNow);
        }

        private static void QuitNow()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Shows the four menu buttons and focuses Continue (or New Game when there is nothing to continue).</summary>
        private void ShowMenu()
        {
            SetMenuVisible(true);
            ScreenTransition.In(menuButtons);
            PromptHint.Show(hintLabel, PromptHint.P(UiAction.Confirm, "Select"));
            SetMenuInteractable(true);
            UIFocusGuard.Focus((continueButton.interactable ? continueButton : newGameButton).gameObject);
        }

        private void SetMenuVisible(bool visible)
        {
            menuButtons.SetActive(visible);
            if (title != null)
                title.SetActive(visible);
        }

        private void SetMenuInteractable(bool interactable)
        {
            newGameButton.interactable = interactable;
            continueButton.interactable = interactable && services.Save.HasSave;
            settingsButton.interactable = interactable;
            quitButton.interactable = interactable;
        }
    }
}
