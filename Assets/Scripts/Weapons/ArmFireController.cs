using BulletHell.Core;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>
    /// Holding R1 fires the selected arm from its muzzle along the arm's facing (slot direction when soft-selected,
    /// aim direction when locked). Each arm slot keeps its own cooldown so switching arms can't dodge fire rate.
    /// </summary>
    [DefaultExecutionOrder(10)] // after ArmSelectionController has positioned and rotated the arms this frame
    public sealed class ArmFireController : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private ArmSelectionController arms;
        [SerializeField] private ProjectilePool pool;
        [SerializeField] private AmmoTypeData ammo;

        private readonly FireTimer[] timers = new FireTimer[ArmLoadout.SlotCount];

        private void Awake()
        {
            for (int i = 0; i < timers.Length; i++)
                timers[i] = new FireTimer();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            int selected = arms.SelectedArm;
            bool held = input.FireHeld;

            for (int slot = 0; slot < timers.Length; slot++)
            {
                ArmVisual arm = arms.GetArm(slot);
                float rate = arm != null ? arm.Data.FireRate : 1f;
                int shots = timers[slot].Tick(dt, held && slot == selected, rate);
                for (int s = 0; s < shots; s++)
                    Fire(arm);
            }
        }

        private void Fire(ArmVisual arm)
        {
            WeaponArmData data = arm.Data;
            Vector2 origin = arm.Muzzle.position;
            Vector3 facing = arm.transform.right;
            float baseAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            int count = data.ProjectilesPerShot;

            for (int i = 0; i < count; i++)
            {
                float offset = count > 1 ? Mathf.Lerp(-data.Spread * 0.5f, data.Spread * 0.5f, i / (count - 1f)) : 0f;
                float radians = (baseAngle + offset) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                pool.Get().Launch(origin, direction, data.ProjectileSpeed, data.Damage,
                                  data.IdColor, data.ProjectileSize, ammo.ProjectileSprite, ammo.MaxLifetime);
            }
        }
    }
}
