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
        private readonly HeatComponent[] heat = new HeatComponent[ArmLoadout.SlotCount];
        private readonly SpinUp[] spins = new SpinUp[ArmLoadout.SlotCount];
        private readonly LineRenderer[] beams = new LineRenderer[ArmLoadout.SlotCount];

        /// <summary>Heat state of the arm in a slot (also valid for empty slots).</summary>
        public HeatComponent GetHeat(int slot) => heat[slot];

        private void Awake()
        {
            for (int i = 0; i < timers.Length; i++)
            {
                timers[i] = new FireTimer();
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
                heat[i] = new HeatComponent();
                spins[i] = new SpinUp();
                beams[i].enabled = false;
            }
        }

        private void Update()
        {
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
                bool firing = ammo != null && arm != null && held && slot == selected && !slotHeat.IsOverheated;
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

                slotHeat.Tick(dt, firing, heatSettings);
            }
        }

        private void Fire(ArmVisual arm, AmmoTypeData ammo)
        {
            WeaponArmData data = arm.Data;
            Vector2 origin = arm.Muzzle.position;
            Vector3 facing = arm.transform.right;
            float baseAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
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
            Vector2 origin = arm.Muzzle.position;
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
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
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
