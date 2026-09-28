using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>What a round multiplies: how many enemies, their health, how often they fire and how fast their bullets fly.</summary>
    public readonly struct RoundDifficulty
    {
        public readonly float CountMultiplier;
        public readonly float HealthMultiplier;
        public readonly float FireRateMultiplier;
        public readonly float BulletSpeedMultiplier;

        public RoundDifficulty(float count, float health, float fireRate, float bulletSpeed)
        {
            CountMultiplier = count;
            HealthMultiplier = health;
            FireRateMultiplier = fireRate;
            BulletSpeedMultiplier = bulletSpeed;
        }
    }

    /// <summary>
    /// How hard each round is. Every stat is an AnimationCurve over the round number (x = round). Past the last key the
    /// value keeps growing by "beyond last key per round" so endless rounds keep getting harder, and each stat is capped
    /// so it can't run away.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyCurve", menuName = "BulletHell/Difficulty Curve")]
    public sealed class DifficultyCurve : ScriptableObject
    {
        [System.Serializable]
        public struct Axis
        {
            [Tooltip("Multiplier by round number (x = round, y = multiplier).")]
            public AnimationCurve Curve;
            [Tooltip("Added to the multiplier for every round after the curve's last key.")]
            public float BeyondLastKeyPerRound;
            public float Min;
            public float Max;

            public float Evaluate(int round)
            {
                if (Curve == null || Curve.length == 0)
                    return 1f;

                float lastKey = Curve[Curve.length - 1].time;
                float value = Curve.Evaluate(Mathf.Min(round, lastKey));
                if (round > lastKey)
                    value += (round - lastKey) * BeyondLastKeyPerRound;
                return Mathf.Clamp(value, Min, Max);
            }
        }

        [SerializeField] private Axis enemyCount;
        [SerializeField] private Axis enemyHealth;
        [SerializeField] private Axis fireRate;
        [SerializeField] private Axis bulletSpeed;

        public RoundDifficulty Evaluate(int round)
        {
            round = Mathf.Max(1, round);
            return new RoundDifficulty(enemyCount.Evaluate(round), enemyHealth.Evaluate(round),
                                       fireRate.Evaluate(round), bulletSpeed.Evaluate(round));
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: used by setup scripts and tests.</summary>
        public void Set(Axis count, Axis health, Axis rate, Axis speed)
        {
            enemyCount = count;
            enemyHealth = health;
            fireRate = rate;
            bulletSpeed = speed;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
