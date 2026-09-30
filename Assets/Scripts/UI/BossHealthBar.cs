using BulletHell.Bosses;
using BulletHell.Core;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// Screen-space boss bar at the top of the combat HUD: name plate, fill, a slower trail behind the fill, and a tick
    /// at the phase 2 threshold that pops when the phase changes. Fades in when a boss spawns (BossEvents), drains and
    /// fades out after its death sequence, and hides with the HUD outside RoundIntro / Combat / Pause.
    /// </summary>
    public sealed class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text namePlate;
        [SerializeField] private Image fill;
        [SerializeField] private Image trail;
        [SerializeField] private RectTransform phaseTick;
        [SerializeField] private Color phase1Color = new Color(0.96f, 0.45f, 0.12f);
        [SerializeField] private Color phase2Color = new Color(1f, 0.24f, 0.62f);
        [Tooltip("How fast the trail catches up with the fill, in bar fractions per second.")]
        [SerializeField, Min(0.1f)] private float trailSpeed = 0.8f;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.4f;
        [Tooltip("Seconds after the death sequence before the bar fades out.")]
        [SerializeField, Min(0f)] private float hideDelay = 0.6f;

        private RunManager run;
        private BossController boss;
        private float targetAlpha;
        private float hideTimer = -1f;

        private void Awake()
        {
            run = GameServices.Ensure().Run;
            if (group != null)
                group.alpha = 0f;
        }

        private void OnEnable()
        {
            BossEvents.Spawned += Show;
            BossEvents.PhaseChanged += OnPhase;
            BossEvents.Defeated += OnDefeated;
            run.Machine.StateChanged += OnStateChanged;
            if (BossEvents.Active != null)
                Show(BossEvents.Active);
        }

        private void OnDisable()
        {
            BossEvents.Spawned -= Show;
            BossEvents.PhaseChanged -= OnPhase;
            BossEvents.Defeated -= OnDefeated;
            run.Machine.StateChanged -= OnStateChanged;
            Unbind();
        }

        private void Show(BossController spawned)
        {
            Unbind();
            boss = spawned;
            boss.Health.Changed += Refresh;
            namePlate.text = boss.Data.DisplayName.ToUpperInvariant();
            fill.color = boss.PhaseIndex > 0 ? phase2Color : phase1Color;
            LayoutTick();
            Refresh();
            trail.fillAmount = fill.fillAmount;
            targetAlpha = 1f;
            hideTimer = -1f;
        }

        private void Unbind()
        {
            if (boss != null)
                boss.Health.Changed -= Refresh;
            boss = null;
        }

        private void Hide()
        {
            Unbind();
            targetAlpha = 0f;
            hideTimer = -1f;
        }

        // The tick sits at the phase 2 threshold; anchors, so the bar can be any width.
        private void LayoutTick()
        {
            if (phaseTick == null)
                return;
            BossPhase[] phases = boss.Data.Phases;
            bool show = phases.Length > 1;
            phaseTick.gameObject.SetActive(show);
            if (!show)
                return;
            float t = Mathf.Clamp01(phases[1].EnterBelowHp01);
            phaseTick.anchorMin = new Vector2(t, 0f);
            phaseTick.anchorMax = new Vector2(t, 1f);
            phaseTick.anchoredPosition = Vector2.zero;
            phaseTick.localScale = Vector3.one;
        }

        private void Refresh()
        {
            if (boss != null)
                fill.fillAmount = boss.Health.Fraction;
        }

        private void OnPhase(BossController changed)
        {
            if (changed != boss)
                return;
            fill.color = phase2Color;
            if (phaseTick != null)
                Tween.Scale(phaseTick, Vector3.one * 1.8f, 0.18f, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
            Tween.Scale(transform, new Vector3(1.03f, 1.15f, 1f), 0.16f, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
        }

        private void OnDefeated(BossController defeated)
        {
            if (defeated != boss)
                return;
            fill.fillAmount = 0f;
            hideTimer = hideDelay;
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to != GameState.Combat && to != GameState.Pause)
                Hide();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (hideTimer >= 0f)
            {
                hideTimer -= dt;
                if (hideTimer < 0f)
                    Hide();
            }
            if (trail != null)
                trail.fillAmount = Mathf.MoveTowards(trail.fillAmount, fill.fillAmount, trailSpeed * dt);
            if (group != null)
                group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, dt / fadeSeconds);
        }
    }
}
