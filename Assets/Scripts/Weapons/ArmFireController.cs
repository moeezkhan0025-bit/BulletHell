using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Holding R1 fires the selected arm from its muzzle along the arm's facing (slot direction when soft-selected,
    /// aim direction when locked), using the player's active ammo. Each arm slot keeps its own cooldown, heat and
    /// spin-up, so switching arms can't dodge fire rate or overheating. Projectile ammo uses the pool; Beam ammo
    /// (Laser) raycasts every frame and draws with a fixed set of line renderers created once in Awake.
    /// </summary>
    [DefaultExecutionOrder(10)] // after ArmSelectionController has positioned and rotated the arms this frame
    public sealed class ArmFireController : MonoBehaviour
    {
        private static readonly RaycastHit2D[] BeamHit = new RaycastHit2D[1];

        [SerializeField] private GameplayInputReader input;
        [SerializeField] private ArmSelectionController arms;
        [SerializeField] private ProjectilePool pool;
        [SerializeField] private AmmoSlots ammoSlots;
        [Tooltip("Material for the laser beam lines (a sprite/unlit material).")]
        [SerializeField] private Material beamMaterial;
        [SerializeField] private int beamSortingOrder = 50;

        private readonly FireTimer[] timers = new FireTimer[ArmLoadout.SlotCount];
        private readonly FireTimer[] autoTimers = new FireTimer[ArmLoadout.SlotCount];
        private readonly HeatComponent[] heat = new HeatComponent[ArmLoadout.SlotCount];
        private readonly SpinUp[] spins = new SpinUp[ArmLoadout.SlotCount];
        private readonly LineRenderer[] beams = new LineRenderer[ArmLoadout.SlotCount];

        /// <summary>Development tools only (the stress test): every arm fires continuously whatever is selected.</summary>
        public bool DebugFireAll { get; set; }

        /// <summary>Heat state of the arm in a slot (also valid for empty slots).</summary>
        public HeatComponent GetHeat(int slot) => heat[slot];

        private float bulletLift;

        private void Awake()
        {
            bulletLift = GameServices.Ensure().Config.Perspective.BulletVisualLift;
            for (int i = 0; i < timers.Length; i++)
            {
                timers[i] = new FireTimer();
                autoTimers[i] = new FireTimer();
                heat[i] = new HeatComponent();
                spins[i] = new SpinUp();
                beams[i] = CreateBeam(i);
            }
        }

        private void OnEnable() => arms.ArmsRebuilt += ResetSlots;

        private void OnDisable() => arms.ArmsRebuilt -= ResetSlots;

        // The arms in the slots were swapped: heat, cooldown and spin-up belong to the old arms.
        private void ResetSlots()
        {
            for (int i = 0; i < timers.Length; i++)
            {
                timers[i] = new FireTimer();
                autoTimers[i] = new FireTimer();
                heat[i] = new HeatComponent();
                spins[i] = new SpinUp();
                beams[i].enabled = false;
            }
        }

        private void Update()
        {
            using var _ = BulletHell.Perf.PerfMarkers.ArmsFire.Auto();
            float dt = Time.deltaTime;
            int selected = arms.SelectedArm;
            bool held = input.FireHeld;
            AmmoTypeData ammo = ammoSlots.Active;
            HeatSettings heatSettings = ammo != null ? ammo.Heat : default;
            SpinSettings spinSettings = ammo != null ? ammo.Spin : default;

            for (int slot = 0; slot < timers.Length; slot++)
            {
                ArmVisual arm = arms.GetArm(slot);
                HeatComponent slotHeat = heat[slot];
                bool firing = ammo != null && arm != null && (DebugFireAll || (held && slot == selected)) && !slotHeat.IsOverheated;
                bool beaming = firing && ammo.Behavior == AmmoBehavior.Beam;

                float spin = spins[slot].Tick(dt, firing && !beaming, spinSettings);
                float rate = arm != null && ammo != null ? arm.Instance.Stats.FireRate * ammo.FireRateMultiplier * spin : 1f;
                int shots = timers[slot].Tick(dt, firing && !beaming, rate);
                for (int s = 0; s < shots && !slotHeat.IsOverheated; s++)
                {
                    Fire(arm, ammo);
                    slotHeat.AddShot(heatSettings);
                }

                if (beaming)
                    FireBeam(slot, arm, ammo, dt);
                beams[slot].enabled = beaming;

                // Auto-fire: an arm with the effect that is NOT selected shoots enemies inside its slot arc at a fraction of its
                // fire rate. Heat applies like for normal shots; beam ammo does not auto-fire.
                bool autoFiring = false;
                if (arm != null && ammo != null && slot != selected && ammo.Behavior == AmmoBehavior.Projectile &&
                    arm.Instance.Shot.HasAutoFire && !slotHeat.IsOverheated &&
                    TryFindAutoTarget(arm, ammo, out float autoAngle))
                {
                    autoFiring = true;
                    ShotProperties shot = arm.Instance.Shot;
                    float autoRate = arm.Instance.Stats.FireRate * ammo.FireRateMultiplier * shot.AutoFireRate;
                    int autoShots = autoTimers[slot].Tick(dt, true, autoRate);
                    for (int s = 0; s < autoShots && !slotHeat.IsOverheated; s++)
                    {
                        Fire(arm, ammo, autoAngle);
                        slotHeat.AddShot(heatSettings);
                    }
                }
                else if (arm != null)
                {
                    autoTimers[slot].Tick(dt, false, 1f);
                }

                slotHeat.Tick(dt, firing || autoFiring, heatSettings);
            }
        }

        // The nearest live enemy inside the arm slot arc (its facing +/- AutoFireArc) and within the bullets reach.
        private bool TryFindAutoTarget(ArmVisual arm, AmmoTypeData ammo, out float angle)
        {
            angle = 0f;
            IReadOnlyList<BulletHell.Enemies.Enemy> enemies = pool.Enemies;
            if (enemies == null)
                return false;

            ShotProperties shot = arm.Instance.Shot;
            Vector2 origin = arms.GroundMuzzle(arm);
            Vector2 facing = arm.transform.right;
            float reach = arm.Instance.Stats.ProjectileSpeed * ammo.ProjectileSpeedMultiplier * ammo.MaxLifetime;
            float bestSqr = float.MaxValue;
            bool found = false;
            for (int i = 0; i < enemies.Count; i++)
            {
                BulletHell.Enemies.Enemy enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;
                Vector2 to = enemy.HitCenter - origin;
                float sqr = to.sqrMagnitude;
                if (sqr >= bestSqr || sqr > reach * reach || Vector2.Angle(facing, to) > shot.AutoFireArc)
                    continue;
                bestSqr = sqr;
                angle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                found = true;
            }
            return found;
        }

        // aimAngle: world angle to fire at; without it the shot follows the arm facing (a normal, selected shot).
        private void Fire(ArmVisual arm, AmmoTypeData ammo, float? aimAngle = null)
        {
            GameServices.Ensure().Audio.Play(ammo.FireSound);
            WeaponArmData data = arm.Data;
            Vector2 origin = arms.GroundMuzzle(arm); // bullets live on the ground plane; the arm is drawn above it
            Vector3 facing = arm.transform.right;
            float baseAngle = aimAngle ?? Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            ArmStats stats = arm.Instance.Stats; // base + armaments; ammo scales these
            ShotProperties shot = arm.Instance.Shot;
            IReadOnlyList<ArmEffect> effects = arm.Instance.Effects;
            int count = stats.ProjectilesPerShot + ammo.ExtraProjectiles;
            float spread = stats.Spread * ammo.SpreadMultiplier + ammo.AddedSpread;
            float speed = stats.ProjectileSpeed * ammo.ProjectileSpeedMultiplier;
            float damage = stats.Damage * ammo.DamageMultiplier;
            float size = data.ProjectileSize * ammo.ProjectileSizeMultiplier;

            for (int i = 0; i < count; i++)
            {
                float offset = count > 1 ? Mathf.Lerp(-spread * 0.5f, spread * 0.5f, i / (count - 1f)) : 0f;
                float radians = (baseAngle + offset) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                pool.Get().Launch(origin, direction, speed, damage, data.IdColor, size,
                                  ammo.ProjectileSprite, ammo.MaxLifetime, shot, effects);
            }
        }

        private void FireBeam(int slot, ArmVisual arm, AmmoTypeData ammo, float dt)
        {
            GameServices.Ensure().Audio.Play(ammo.FireSound);   // every frame the beam is on; the sound's own minimum interval paces it
            Vector2 origin = arms.GroundMuzzle(arm);
            Vector2 direction = arm.transform.right;
            Vector2 end = origin + direction * ammo.BeamRange;

            int count = Physics2D.Raycast(origin, direction, pool.HitFilter, BeamHit, ammo.BeamRange);
            if (count > 0)
            {
                end = BeamHit[0].point;
                if (BeamHit[0].collider.TryGetComponent(out IDamageable target) && target.IsAlive)
                {
                    ArmStats stats = arm.Instance.Stats;
                    target.TakeDamage(stats.Damage * stats.FireRate * ammo.DamageMultiplier * dt);
                    IReadOnlyList<ArmEffect> effects = arm.Instance.Effects;
                    for (int i = 0; i < effects.Count; i++)
                        if (effects[i] != null)
                            effects[i].OnBeamHit(BeamHit[0].collider, dt);
                }
            }

            LineRenderer line = beams[slot];
            line.startColor = line.endColor = ammo.Tint;
            line.widthMultiplier = ammo.BeamWidth;
            // Collision is on the ground; the beam is drawn with the same lift as bullets.
            Vector2 lift = Vector2.up * bulletLift;
            line.SetPosition(0, origin + lift);
            line.SetPosition(1, end + lift);
        }

        private LineRenderer CreateBeam(int slot)
        {
            var go = new GameObject($"Beam_{slot}");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.sharedMaterial = beamMaterial;
            line.sortingLayerID = SortingLayers.Id(SortingLayers.Bullets);
            line.sortingOrder = beamSortingOrder;
            line.numCapVertices = 2;
            line.enabled = false;
            return line;
        }
    }
}
