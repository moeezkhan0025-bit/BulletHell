using System;
using BulletHell.AI;
using BulletHell.Arena;
using BulletHell.Core;
using BulletHell.Feedback;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>
    /// A pooled enemy. The wave spawner takes one from the EnemyPool and calls Initialize with the enemy type and the
    /// round's difficulty; this binds the EnemyData to the shared Health / hit flash / health bar / patrol / attacker
    /// components. When it dies it hides and raises Defeated; the spawner returns it to the pool.
    /// The root stands on the floor at the enemy's FEET: it moves, sorts and collides as a flat footprint there. The
    /// body and health bar hang under the Rig child, which sits over the feet; player bullets hit a hurtbox that stands
    /// on the feet and reaches up over the body (PerspectiveTuning).
    /// </summary>
    public sealed class Enemy : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private CapsuleCollider2D hitbox;
        [Tooltip("Parent of the body and health bar; lifted above the feet by the enemy's size.")]
        [SerializeField] private Transform rig;
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Flat ground shadow at the feet. Optional.")]
        [SerializeField] private SpriteRenderer shadow;
        [SerializeField] private HitFeedback hitFeedback;
        [Tooltip("Single writer of the Motion child: breathing, bob, squash, punch, windup, spawn pop-in.")]
        [SerializeField] private ProceduralMotion motion;
        [SerializeField] private TelegraphFx telegraph;
        [Tooltip("Material for the ground warning lines (the body uses the character shader, which lines must not).")]
        [SerializeField] private Material lineMaterial;
        [SerializeField] private HealthBar healthBar;
        [SerializeField] private PatrolMover patrol;
        [Tooltip("Movement AI for the non-patrol behaviours (Chaser, Skirmisher, Sentry, Charger, Sniper).")]
        [SerializeField] private EnemyBrain brain;

        private EnemyData data;
        private Sprite placeholderSprite;
        private LifeCycleTuning lifeCycle;
        private StatusEffects status;
        private EnemyAttacker attacker;
        private NavigationService navigation;

        public EnemyData Data => data;
        public EnemyBrain Brain => brain;
        /// <summary>Attack wind-up 0..1: inflate, tremble and DANGER pulse. ClearWindup ends it.</summary>
        public void SetWindup(float progress01) => telegraph.SetWindup(progress01);
        public void ClearWindup() => telegraph.ClearWindup();
        /// <summary>A steady colour wash (overheated). ClearStatusTint removes it.</summary>
        public void SetStatusTint(Color tint) => telegraph.SetStatusTint(tint, 0.6f);
        public void ClearStatusTint() => telegraph.SetStatusTint(Color.white, 0f);
        public bool IsAlive => health.IsAlive && body.enabled;
        /// <summary>The enemy's feet: where it stands on the floor.</summary>
        public Vector2 Position => transform.position;
        /// <summary>Middle of the body, where its bullets come out.</summary>
        public Vector2 BodyCenter => rig.position;
        /// <summary>Radius of the flat movement footprint at the feet.</summary>
        public float FootprintRadius => baseFootprint * CharacterScale.Value;
        private float baseFootprint;

        /// <summary>Raised when this enemy dies.</summary>
        public event Action<Enemy> Defeated;

        private void Awake()
        {
            if (!TryGetComponent(out CharacterScaleApplier _))
                gameObject.AddComponent<CharacterScaleApplier>();
            TryGetComponent(out status);
            TryGetComponent(out attacker);
            health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.Died -= OnDied;
        }

        // Recycled into the pool (round change, death): leave the navigation registry and stop the AI.
        private void OnDisable()
        {
            if (navigation != null)
                navigation.Unregister(this);
            if (brain != null)
                brain.Stop();
        }

        /// <summary>Makes this enemy a fresh, alive enemy of the given type at a position, scaled by the round's difficulty.</summary>
        public void Initialize(EnemyData enemyData, Vector2 position, in RoundDifficulty difficulty,
                               ProjectilePool pool, PlayerHealth player, ArenaController arena = null,
                               NavigationService navigationService = null)
        {
            navigation = navigationService;
            data = enemyData;
            transform.position = position;

            GameConfig config = GameServices.Ensure().Config;
            PerspectiveTuning perspective = config.Perspective;
            float size = data.Size;

            // Painted art (scale test): the sprite's pivot is at the feet and it is drawn at its own size. The rig still
            // sits mid-body (bullets leave from it, motion squashes around it); the body hangs down to put the pivot on the feet.
            if (placeholderSprite == null)
                placeholderSprite = body.sprite;
            ScaleTestArt art = config.ScaleTestArt;
            Sprite painted = art != null && art.UsePaintedEnemies ? data.PaintedSprite : null;
            bool isPainted = painted != null;
            body.sprite = isPainted ? painted : placeholderSprite;

            float lift;
            float barHeight;
            Vector2 hurtbox;
            Vector2 hurtboxOffset;
            if (isPainted)
            {
                baseFootprint = data.PaintedFootprintRadius;
                float top = data.PaintedHeight;
                lift = top * 0.5f;
                barHeight = top - lift + 0.2f;
                body.transform.localPosition = new Vector3(0f, -lift, 0f);
                body.transform.localScale = Vector3.one;
                hurtbox = data.PaintedHurtboxSize;
                hurtboxOffset = new Vector2(data.PaintedHurtboxOffsetX, hurtbox.y * 0.5f);
            }
            else
            {
                baseFootprint = perspective.EnemyFootprintRadiusFor(size);
                lift = size * (0.5f - perspective.EnemyFeetInset);
                barHeight = size * 0.5f + 0.25f;
                body.transform.localPosition = Vector3.zero;
                body.transform.localScale = Vector3.one * size;
                hurtbox = perspective.EnemyHurtboxSizeFor(size);
                hurtboxOffset = new Vector2(0f, -size * perspective.EnemyFeetInset + hurtbox.y * 0.5f);
            }
            rig.localPosition = new Vector3(0f, lift, 0f);
            hitbox.size = hurtbox;
            hitbox.offset = hurtboxOffset;

            if (shadow != null)
            {
                float width = baseFootprint * 2f * perspective.ShadowWidth;
                Vector2 native = shadow.sprite != null ? (Vector2)shadow.sprite.bounds.size : Vector2.one;
                shadow.transform.localScale = new Vector3(width / native.x, width * perspective.ShadowFlatness / native.y, 1f);
                shadow.color = perspective.ShadowColor;
            }

            health.Initialize(data.MaxHealth * difficulty.HealthMultiplier);
            FeedbackTuning feedback = GameServices.Ensure().Config.Feedback;
            MotionTuning motionTuning = data.MotionOverride != null ? data.MotionOverride : feedback.EnemyMotion;
            lifeCycle = data.LifeCycleOverride != null ? data.LifeCycleOverride : feedback.EnemyLifeCycle;
            motion.SetTuning(motionTuning);
            motion.ResetState();
            telegraph.Configure(motionTuning);
            telegraph.ClearWindup();
            telegraph.SetStatusTint(Color.white, 0f);
            hitFeedback.Configure(data.HitOverride != null ? data.HitOverride : feedback.EnemyHit, data.HitFlashDuration);
            healthBar.Layout(Mathf.Max(0.6f, isPainted ? hurtbox.x : size), barHeight);
            body.color = isPainted ? Color.white : data.Color;
            bool patrols = data.Behavior == EnemyBehavior.Patrol;
            patrol.Configure(position, patrols ? data.MoveSpeed : 0f, data.MoveRange, data.MoveAxis, arena, FootprintRadius);
            if (status != null)
                status.Clear();
            SetAlive(true);
            motion.PlaySpawn(lifeCycle);

            if (attacker != null)
            {
                attacker.Configure(data.Attacks);
                attacker.Muzzle = rig;
                attacker.Bind(pool, player);
                attacker.FireRateMultiplier = difficulty.FireRateMultiplier;
                attacker.BulletSpeedMultiplier = difficulty.BulletSpeedMultiplier;
                attacker.Begin();
            }

            if (brain != null)
            {
                brain.Configure(this, data, difficulty, pool, player, arena, navigation, attacker, lineMaterial != null ? lineMaterial : body.sharedMaterial);
                if (navigation != null && brain.IsActive)
                    navigation.Register(this);
            }
        }

        private void OnDied()
        {
            telegraph.ClearWindup();
            FeedbackHub.SpawnGhost(body, transform, lifeCycle);
            if (lifeCycle != null && lifeCycle.SplatParticles > 0)
                FeedbackHub.Play(VfxKind.Debris, BodyCenter, lifeCycle.SplatParticles);
            if (status != null)
                status.Clear();
            if (attacker != null)
                attacker.Stop();
            if (brain != null)
                brain.Stop();
            if (navigation != null)
                navigation.Unregister(this);
            SetAlive(false);
            Defeated?.Invoke(this);
        }

        private void SetAlive(bool alive)
        {
            body.enabled = alive;
            hitbox.enabled = alive;
            if (shadow != null)
                shadow.enabled = alive;
            if (!alive)
                patrol.enabled = false;
        }
    }
}
