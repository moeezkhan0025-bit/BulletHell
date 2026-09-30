using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// The card the round 1 onboarding talks through: a small "LESSON 2 OF 6" line, the instruction, progress pips and the
    /// skip prompt. Purely a view; <see cref="TutorialController"/> decides what it says. It is hidden (alpha 0, no raycasts)
    /// unless a step is showing and the game is in combat, and pops in with the theme's bubble timing.
    /// </summary>
    public sealed class TutorialPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text counterLabel;
        [SerializeField] private TMP_Text instruction;
        [SerializeField] private TMP_Text skipLabel;
        [Tooltip("One dot per step; done = leaf, current = gold, to come = marble shade.")]
        [SerializeField] private Image[] pips = new Image[0];

        private bool shown;
        private bool visible = true;

        public bool IsShowing => shown;

        private void Awake() => Apply();

        /// <summary>Shows a step. A step shown after another one (panel already up) punches; the first one pops in.</summary>
        public void ShowStep(string counter, string text, string skip, int current, int total)
        {
            bool popIn = !shown;
            shown = true;
            counterLabel.text = counter;
            skipLabel.text = skip;
            SetInstruction(text, false);
            UITheme theme = UITheme.Current;
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i].gameObject.SetActive(i < total);
                if (theme != null)
                    pips[i].color = i < current ? theme.Leaf : i == current ? theme.Gold : theme.MarbleShade;
            }
            Apply();
            Pop(popIn ? 0.9f : 0.96f);
        }

        /// <summary>Changes the instruction of the step on screen (device switched, or the ammo step became possible).</summary>
        public void SetInstruction(string text, bool done)
        {
            instruction.text = text;
            UITheme theme = UITheme.Current;
            if (theme != null)
                instruction.color = done ? theme.Leaf : theme.InkSoil;
        }

        /// <summary>The "Nice!" confirmation after a step is performed.</summary>
        public void ShowDone(string text, int current)
        {
            SetInstruction(text, true);
            if (current >= 0 && current < pips.Length && UITheme.Current != null)
                pips[current].color = UITheme.Current.Leaf;
            Pop(1.06f);
        }

        public void Hide()
        {
            shown = false;
            Tween.StopAll(card);
            card.localScale = Vector3.one;
            Apply();
        }

        /// <summary>The panel only shows in combat (not while paused or between screens).</summary>
        public void SetVisible(bool value)
        {
            visible = value;
            Apply();
        }

        private void Apply()
        {
            bool on = shown && visible;
            group.alpha = on ? 1f : 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void Pop(float from)
        {
            Tween.StopAll(card);
            card.localScale = Vector3.one * from;
            UITheme theme = UITheme.Current;
            Tween.Scale(card, 1f, theme != null ? theme.BubbleInSeconds : 0.25f, Ease.OutBack, useUnscaledTime: true);
        }

        private void OnDisable()
        {
            if (card != null)
            {
                Tween.StopAll(card);
                card.localScale = Vector3.one;
            }
        }
    }
}
