using BulletHell.Core;
using BulletHell.Enemies;
using PrimeTween;
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
            BulletHell.Bosses.BossData bossData = data != null ? data.FindBoss() : null;
            string challenger = bossData != null ? $"THE {bossData.DisplayName.ToUpperInvariant()} enters the colosseum!" : "A mighty challenger enters the colosseum!";
            label.text = boss
                ? $"<color={BossColor}>BOSS ROUND {round}</color>\n<size=56>{challenger}</size>"
                : $"ROUND {round}\n<size=56>Get ready, gladiator!</size>";
            phase = Phase.Title;
            timeLeft = tuning.IntroBannerSeconds;
            PopIn(0.6f);
        }

        private void Stop()
        {
            Tween.StopAll(label);
            Tween.StopAll(label.transform);
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
                    break;

                case Phase.Begin:
                    if (timeLeft <= 0f)
                        Stop();
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
            PopIn(1.6f); // each number punches in big and settles
        }

        private void StartBegin()
        {
            label.text = "BEGIN!";
            phase = Phase.Begin;
            timeLeft = tuning.BeginSeconds;
            run.BeginCombat();
            PopIn(0.7f);
            float fade = Mathf.Min(0.4f, tuning.BeginSeconds);
            Tween.Alpha(label, baseColor.a, 0f, fade, Ease.Linear, startDelay: tuning.BeginSeconds - fade);
        }

        // Shows the label at full alpha, scaled from `fromScale` to 1 with a springy overshoot.
        private void PopIn(float fromScale)
        {
            Tween.StopAll(label);
            Tween.StopAll(label.transform);
            label.color = baseColor;
            label.transform.localScale = Vector3.one * fromScale;
            label.gameObject.SetActive(true);
            Tween.Scale(label.transform, 1f, 0.3f, Ease.OutBack);
        }
    }
}
