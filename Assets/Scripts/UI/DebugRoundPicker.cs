using BulletHell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Debug row on the Pause screen: pick a round with - / + and press Go to restart combat there. Development
    /// builds and the editor only (it hides itself in release builds). Controller navigable like every other button.
    /// </summary>
    public sealed class DebugRoundPicker : MonoBehaviour
    {
        private const int MaxRound = 99;

        [SerializeField] private Text label;
        [SerializeField] private Button lowerButton;
        [SerializeField] private Button raiseButton;
        [SerializeField] private Button goButton;

        private RunManager run;
        private int round = 1;

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
                return;
            }

            run = GameServices.Ensure().Run;
            lowerButton.onClick.AddListener(() => Change(-1));
            raiseButton.onClick.AddListener(() => Change(1));
            goButton.onClick.AddListener(() => run.DebugSkipToRound(round));
        }

        // Starts at the round being played every time the pause screen opens.
        private void OnEnable()
        {
            if (run == null)
                return;
            round = run.State != null ? run.State.Round : 1;
            Refresh();
        }

        private void Change(int delta)
        {
            round = Mathf.Clamp(round + delta, 1, MaxRound);
            Refresh();
        }

        private void Refresh() => label.text = $"DEBUG  skip to round {round}";
    }
}
