using BulletHell.Bosses;
using TMPro;
using BulletHell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Debug row on the Pause screen: pick a round with - / + and press Go to restart combat there, Boss to jump to the
    /// first boss round, and HP- / HP+ to set the living boss's health in steps of 10%. Development builds and the editor
    /// only (it hides itself in release builds). Controller navigable like every other button.
    /// </summary>
    public sealed class DebugRoundPicker : MonoBehaviour
    {
        private const int MaxRound = 99;
        private const float HealthStep = 0.1f;

        [SerializeField] private TMP_Text label;
        [SerializeField] private Button lowerButton;
        [SerializeField] private Button raiseButton;
        [SerializeField] private Button goButton;
        [Tooltip("Shows the round's arena layout with no enemies and idle traps (walk and jump around it).")]
        [SerializeField] private Button previewButton;
        [Tooltip("Restarts combat at the first boss round. Optional.")]
        [SerializeField] private Button bossButton;
        [Tooltip("Boss health down / up by 10% while a boss is alive. Optional.")]
        [SerializeField] private Button healthDownButton;
        [SerializeField] private Button healthUpButton;

        private RunManager run;
        private GameConfig config;
        private int round = 1;

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                gameObject.SetActive(false);
                return;
            }

            GameServices services = GameServices.Ensure();
            run = services.Run;
            config = services.Config;
            lowerButton.onClick.AddListener(() => Change(-1));
            raiseButton.onClick.AddListener(() => Change(1));
            goButton.onClick.AddListener(() => run.DebugSkipToRound(round));
            if (previewButton != null)
                previewButton.onClick.AddListener(() => run.DebugPreviewLayout(round));
            if (bossButton != null)
                bossButton.onClick.AddListener(GoToBoss);
            if (healthDownButton != null)
                healthDownButton.onClick.AddListener(() => NudgeBossHealth(-HealthStep));
            if (healthUpButton != null)
                healthUpButton.onClick.AddListener(() => NudgeBossHealth(HealthStep));
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

        private void GoToBoss()
        {
            for (int r = 1; r <= config.RoundCount; r++)
            {
                if (config.GetRound(r) != null && config.GetRound(r).IsBossRound)
                {
                    round = r;
                    run.DebugSkipToRound(r);
                    return;
                }
            }
        }

        private void NudgeBossHealth(float delta)
        {
            BossController boss = BossEvents.Active;
            if (boss == null)
                return;
            boss.DebugSetHealth01(boss.Health.Fraction + delta);
            Refresh();
        }

        private void Refresh()
        {
            BossController boss = BossEvents.Active;
            bool hasBoss = boss != null && boss.Health.IsAlive;
            if (healthDownButton != null)
                healthDownButton.interactable = hasBoss;
            if (healthUpButton != null)
                healthUpButton.interactable = hasBoss;
            label.text = hasBoss
                ? $"DEBUG  round {round}   boss {Mathf.RoundToInt(boss.Health.Fraction * 100f)}%"
                : $"DEBUG  round {round}";
        }
    }
}
