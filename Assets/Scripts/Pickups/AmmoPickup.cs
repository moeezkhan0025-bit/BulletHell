using System.Collections.Generic;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Pickups
{
    /// <summary>
    /// An ammo type lying in the world. Registers itself in a static list so the collector can find the nearest one
    /// without FindObjectOfType. Look comes from the ammo asset (sprite, tint) and PickupTuning (size).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AmmoPickup : MonoBehaviour
    {
        private static readonly List<AmmoPickup> Registry = new List<AmmoPickup>();

        [SerializeField] private AmmoTypeData ammo;
        [SerializeField] private PickupTuning tuning;

        private float immuneUntil;
        private bool consumed;

        public AmmoTypeData Ammo => ammo;
        public static int Count => Registry.Count;

        /// <summary>Nearest grabbable pickup within a radius, or null.</summary>
        public static AmmoPickup FindNearest(Vector2 position, float radius)
        {
            AmmoPickup best = null;
            float bestSqr = radius * radius;
            float now = Time.time;
            for (int i = 0; i < Registry.Count; i++)
            {
                AmmoPickup pickup = Registry[i];
                if (pickup.consumed || now < pickup.immuneUntil)
                    continue;
                float sqr = ((Vector2)pickup.transform.position - position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = pickup;
                }
            }
            return best;
        }

        /// <summary>Configures a freshly spawned (dropped) pickup.</summary>
        public void Setup(AmmoTypeData ammoType, PickupTuning pickupTuning, float immunitySeconds)
        {
            ammo = ammoType;
            tuning = pickupTuning;
            immuneUntil = Time.time + immunitySeconds;
            ApplyLook();
        }

        public void Consume()
        {
            consumed = true;
            Registry.Remove(this);
            Destroy(gameObject);
        }

        private void OnEnable()
        {
            Registry.Add(this);
            if (tuning != null && Registry.Count > tuning.WarnPickupCount)
                Debug.LogWarning($"AmmoPickup count is {Registry.Count} (expected at most {tuning.WarnPickupCount}).", this);
            ApplyLook();
        }

        private void OnDisable() => Registry.Remove(this);

        private void ApplyLook()
        {
            if (ammo == null)
                return;
            var spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = ammo.ProjectileSprite;
            spriteRenderer.color = ammo.Tint;
            if (tuning != null)
                transform.localScale = Vector3.one * tuning.VisualSize;
        }
    }
}
