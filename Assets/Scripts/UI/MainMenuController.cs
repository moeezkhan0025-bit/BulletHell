using BulletHell.Core;
using BulletHell.Platform;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Main Menu: Start Game (asks before overwriting an existing save), Continue (disabled without a save) and Quit.
    /// Fully navigable with the stick / D-pad; the confirm dialog opens with "No" selected.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text messageText;
        [Header("Overwrite confirmation")]
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        private GameServices services;

        private void Awake()
        {
            services = GameServices.Ensure();
            startButton.onClick.AddListener(OnStartPressed);
            continueButton.onClick.AddListener(OnContinuePressed);
            quitButton.onClick.AddListener(OnQuitPressed);
            confirmYesButton.onClick.AddListener(OnOverwriteConfirmed);
            confirmNoButton.onClick.AddListener(CloseConfirm);
            quitButton.gameObject.SetActive(PlatformCapabilities.CanQuitApplication);
        }

        private void Start()
        {
            Time.timeScale = 1f;
            messageText.text = "";
            CloseConfirm();
        }

        private void OnStartPressed()
        {
            if (!services.Save.HasSave)
            {
                StartNewRun();
                return;
            }

            SetMenuInteractable(false);
            confirmPanel.SetActive(true);
            UIFocusGuard.Focus(confirmNoButton.gameObject);
        }

        private void OnOverwriteConfirmed()
        {
            services.Save.Delete();
            StartNewRun();
        }

        private void CloseConfirm()
        {
            confirmPanel.SetActive(false);
            SetMenuInteractable(true);
            UIFocusGuard.Focus((continueButton.interactable ? continueButton : startButton).gameObject);
        }

        private void StartNewRun()
        {
            services.Run.StartNewRun();
            services.Scenes.Load(SceneLoader.Game);
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
            UIFocusGuard.Focus(startButton.gameObject);
        }

        private void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetMenuInteractable(bool interactable)
        {
            startButton.interactable = interactable;
            continueButton.interactable = interactable && services.Save.HasSave;
            quitButton.interactable = interactable;
        }
    }
}
