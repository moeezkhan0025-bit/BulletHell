using System;
using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// TEMPORARY size test (Docs/ART_SPEC.md section 9): one multiplier for the player, its arms and every enemy, while the
    /// arena, obstacles and traps stay put. A debug key cycles the steps; remove once the character size is decided.
    /// </summary>
    public static class CharacterScale
    {
        private static readonly float[] Steps = { 1f, 1.15f, 1.2f, 1.25f };
        private static int index;

        public static float Value => Steps[index];

        /// <summary>Raised after the multiplier changed.</summary>
        public static event Action Changed;

        public static void Cycle()
        {
            index = (index + 1) % Steps.Length;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            index = 0;
            Changed = null;
        }
    }

    /// <summary>Scales the GameObject it sits on (player root, enemy root) by <see cref="CharacterScale"/>.</summary>
    public sealed class CharacterScaleApplier : MonoBehaviour
    {
        private void OnEnable()
        {
            CharacterScale.Changed += Apply;
            Apply();
        }

        private void OnDisable() => CharacterScale.Changed -= Apply;

        private void Apply() => transform.localScale = Vector3.one * CharacterScale.Value;
    }
}
