using BulletHell.Core;
using BulletHell.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// The announcer moment that opens every round: a "Round N" banner (a special one for boss rounds), a 3-2-1
    /// countdown, then "Begin!" as combat starts. It runs during the RoundIntro state and ends it (RunManager.BeginCombat)
    /// when the countdown reaches zero. The player can move meanwhile; enemies have not spawned yet.
    /// </summary>
    public sealed class RoundIntroBanner : MonoBehaviour
    {
        private enum Phase { Idle, Title, Countdown, Begin }

        private const string BossColor = "#ff5a3c";

        [SerializeField] private CombatTuning tuning;
        [SerializeField] private Text label;

        private RunManager run;
        private GameConfig config;
        private Phase phase = Phase.Idle;
        private float timeLeft;
        private int number;
        private Color baseColor;

        private void Awake()
        {
            GameServices services = GameServices.Ensure();
            run = services.Run;
            config = services.Config;
            baseColor = label.color;
            label.gameObject.SetActive(false);
        }

        private void OnEnable() => run.Machine.StateChanged += OnStateChanged;

        private void OnDisable() => run.Machine.StateChanged -= OnStateChanged;

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.RoundIntro)
            {
                ShowTitle(run.State.Round);
                return;
            }

            // "Begin!" carries on into the first moments of combat; any other change ends the moment.
            if (!(to == GameState.Combat && phase == Phase.Begin))
                Stop();
        }

        private void ShowTitle(int round)
        {
            RoundData data = config.GetRound(round);
            bool boss = data != null && data.IsBossRound;
            label.text = boss
                ? $"<color={BossColor}>BOSS ROUND {round}</color>\n<size=56>A mighty challenger enters the colosseum!</size>"
                : $"ROUND {round}\n<size=56>Get ready, gladiator!</size>";
            phase = Phase.Title;
            timeLeft = tuning.IntroBannerSeconds;
            Present(1f, 1f);
        }

        private void Stop()
        {
            phase = Phase.Idle;
            label.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (phase == Phase.Idle)
                return;

            timeLeft -= Time.deltaTime;
            switch (phase)
            {
                case Phase.Title:
                    if (timeLeft <= 0f)
                        StartCountdown();
                    break;

                case Phase.Countdown:
                    if (timeLeft <= 0f)
                    {
                        number--;
                        if (number <= 0)
                            StartBegin();
                        else
                            ShowNumber();
                    }
                    else
                    {
                        // Each number punches in big and settles.
                        float t = 1f - timeLeft / tuning.CountdownStepSeconds;
                        Present(1f, 1f + 0.35f * (1f - t));
                    }
                    break;

                case Phase.Begin:
                    if (timeLeft <= 0f)
                        Stop();
                    else
                        Present(Mathf.Clamp01(timeLeft / Mathf.Min(0.4f, tuning.BeginSeconds)), 1f);
                    break;
            }
        }

        private void StartCountdown()
        {
            phase = Phase.Countdown;
            number = tuning.CountdownSteps;
            ShowNumber();
        }

        private void ShowNumber()
        {
            label.text = number.ToString();
            timeLeft = tuning.CountdownStepSeconds;
            Present(1f, 1.35f);
        }

        private void StartBegin()
        {
            label.text = "BEGIN!";
            phase = Phase.Begin;
            timeLeft = tuning.BeginSeconds;
            run.BeginCombat();
            Present(1f, 1f);
        }

        private void Present(float alpha, float scale)
        {
            Color color = baseColor;
            color.a = baseColor.a * alpha;
            label.color = color;
            label.transform.localScale = Vector3.one * scale;
            label.gameObject.SetActive(true);
        }
    }
}
