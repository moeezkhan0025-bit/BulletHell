using System;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>What the player has to do to finish a tutorial step.</summary>
    public enum TutorialStepKind
    {
        /// <summary>Push the right stick for a moment.</summary>
        Move,
        /// <summary>Soft-select an arm with the left stick (a fresh selection after the step began).</summary>
        SelectArm,
        /// <summary>Hold fire with an arm selected.</summary>
        Fire,
        /// <summary>Press L3 to lock the selected arm.</summary>
        Lock,
        /// <summary>Jump.</summary>
        Jump,
        /// <summary>Switch the active ammo slot (needs a second ammo type; waits for a pickup first).</summary>
        SwapAmmo,
    }

    /// <summary>
    /// The round 1 onboarding: which steps to show, in which order, with what text, and the timings. Text uses {0}, {1}
    /// for the button names of <see cref="Step.Keys"/> on the device in use ("Push {0} to move" -> "Push [Right Stick] to move").
    /// </summary>
    [CreateAssetMenu(fileName = "Tutorial", menuName = "BulletHell/Tutorial")]
    public sealed class TutorialData : ScriptableObject
    {
        [Serializable]
        public sealed class Step
        {
            public TutorialStepKind Kind;
            [Tooltip("Small heading above the instruction.")]
            public string Title = "";
            [TextArea(1, 3)] public string Text = "";
            [Tooltip("The controls {0}, {1}... stand for, by meaning (the glyph library names them per device).")]
            public UiAction[] Keys = new UiAction[0];
            [Tooltip("SwapAmmo only: shown while the second ammo type has not been collected yet.")]
            [TextArea(1, 3)] public string WaitingText = "";
        }

        [Tooltip("The round the onboarding plays in.")]
        [SerializeField, Min(1)] private int round = 1;
        [SerializeField] private Step[] steps = new Step[0];
        [Tooltip("Enemies stay away until this many steps are done (0 = they come at once). The first three steps are the basics: move, select, fire.")]
        [SerializeField, Min(0)] private int enemiesWaitForSteps = 3;
        [Tooltip("Seconds of right-stick input that count as having moved.")]
        [SerializeField, Min(0.1f)] private float moveSeconds = 0.6f;
        [Tooltip("Seconds of holding fire (with an arm selected) that count as having fired.")]
        [SerializeField, Min(0.05f)] private float fireSeconds = 0.3f;
        [Tooltip("How long the \"Nice!\" confirmation stays before the next step.")]
        [SerializeField, Min(0.1f)] private float doneSeconds = 0.8f;
        [SerializeField] private string doneText = "Nice!";
        [Tooltip("Shown on the panel: {0} is the skip button.")]
        [SerializeField] private string skipText = "{0} Skip tutorial";
        [SerializeField] private string stepCounterFormat = "LESSON {0} OF {1}";

        public int Round => round;
        public Step[] Steps => steps;
        public int EnemiesWaitForSteps => enemiesWaitForSteps;
        public float MoveSeconds => moveSeconds;
        public float FireSeconds => fireSeconds;
        public float DoneSeconds => doneSeconds;
        public string DoneText => doneText;
        public string SkipText => skipText;
        public string StepCounterFormat => stepCounterFormat;

#if UNITY_EDITOR
        public void SetSteps(Step[] value)
        {
            steps = value;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
