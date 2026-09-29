using BulletHell.Core;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>
    /// Fires an enemy's attack patterns. Each pattern keeps its own timer (and spiral turn). Stopped while the enemy is
    /// dead, and paused while it is stunned. Bullets come from the shared projectile pool.
    /// </summary>
    public sealed class EnemyAttacker : MonoBehaviour
    {
        private static readonly float[] Angles = new float[AttackPattern.MaxBulletsPerVolley];

        private AttackPattern[] patterns = new AttackPattern[0];
        private float[] timers = new float[0];
        private float[] spiralOffsets = new float[0];
        private ProjectilePool pool;
        private PlayerHealth target;
        private StatusEffects status;
        private bool firing;

        /// <summary>Scales how often volleys fire (difficulty). 1 = the pattern's own interval.</summary>
        public float FireRateMultiplier { get; set; } = 1f;
        /// <summary>Scales bullet speed (difficulty).</summary>
        public float BulletSpeedMultiplier { get; set; } = 1f;

        /// <summary>
        /// While true no volley is fired (no line of sight, still moving into position, overheated...). Timers stay
        /// ready, so the enemy fires the moment it is let go.
        /// </summary>
        public bool HoldFire { get; set; }

        /// <summary>Raised after every volley, so a Sentry can add heat.</summary>
        public event System.Action Fired;

        public void Configure(AttackPattern[] attackPatterns)
        {
            patterns = attackPatterns ?? new AttackPattern[0];
            timers = new float[patterns.Length];
            spiralOffsets = new float[patterns.Length];
            TryGetComponent(out status);
            enabled = false;
        }

        /// <summary>Where bullets come out (the enemy's body centre). Empty = this object's own position.</summary>
        public Transform Muzzle { get; set; }

        public void Bind(ProjectilePool bulletPool, PlayerHealth player)
        {
            pool = bulletPool;
            target = player;
        }

        /// <summary>Starts attacking: every pattern waits its initial delay first.</summary>
        public void Begin()
        {
            for (int i = 0; i < patterns.Length; i++)
            {
                timers[i] = patterns[i] != null ? patterns[i].InitialDelay : 0f;
                spiralOffsets[i] = 0f;
            }
            HoldFire = false;
            firing = true;
            enabled = true;
        }

        public void Stop()
        {
            firing = false;
            enabled = false;
        }

        private void Update()
        {
            if (!firing || pool == null || target == null || !target.IsAlive)
                return;
            if (status != null && status.IsStunned)
                return;

            float dt = Time.deltaTime;
            for (int i = 0; i < patterns.Length; i++)
            {
                AttackPattern pattern = patterns[i];
                if (pattern == null)
                    continue;

                timers[i] = Mathf.Max(0f, timers[i] - dt);
                if (timers[i] > 0f || HoldFire)
                    continue;

                Fire(i, pattern);
                timers[i] = pattern.FireInterval / Mathf.Max(0.05f, FireRateMultiplier);
                Fired?.Invoke();
            }
        }

        private void Fire(int index, AttackPattern pattern)
        {
            Vector2 origin = Muzzle != null ? Muzzle.position : transform.position;
            Vector2 toPlayer = target.Position - origin;
            float aimAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

            int count = AttackPatternMath.FillAngles(pattern.Shape, pattern.BulletCount, pattern.SpreadAngle,
                                                     aimAngle, spiralOffsets[index], Angles);
            float speed = pattern.BulletSpeed * BulletSpeedMultiplier;
            for (int i = 0; i < count; i++)
            {
                float radians = Angles[i] * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                pool.Get().LaunchHostile(origin, direction, speed, pattern.Damage, pattern.BulletColor,
                                         pattern.BulletSize, pattern.BulletSprite, pattern.BulletLifetime);
            }

            if (pattern.Shape == AttackShape.Spiral)
                spiralOffsets[index] = Mathf.Repeat(spiralOffsets[index] + pattern.SpiralStep, 360f);
        }
    }
}
