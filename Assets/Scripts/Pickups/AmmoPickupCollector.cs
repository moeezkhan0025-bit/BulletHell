using BulletHell.Input;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Pickups
{
    /// <summary>
    /// On the player. Walking over a pickup fills the first empty ammo slot. With all 4 slots full, holding a face
    /// button near a pickup replaces that slot; the replaced ammo drops as a pickup. A tap only switches ammo.
    /// A pickup of a type already carried is ignored (stays on the ground).
    /// </summary>
    public sealed class AmmoPickupCollector : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private AmmoSlots slots;
        [SerializeField] private PickupTuning tuning;
        [SerializeField] private AmmoPickup pickupPrefab;

        private int holdSlot = -1;
        private float holdTime;
        private bool needRelease;

        /// <summary>The pickup in prompt range that can be swapped in, or null.</summary>
        public AmmoPickup NearPickup { get; private set; }

        /// <summary>Slot being held for a swap, or -1.</summary>
        public int HoldSlot => holdSlot;

        public float HoldProgress01 => holdSlot < 0 ? 0f : Mathf.Clamp01(holdTime / tuning.HoldDuration);

        private void Update()
        {
            NearPickup = null;
            Vector2 position = transform.position;

            AmmoPickup pickup = AmmoPickup.FindNearest(position, tuning.PromptRadius);
            if (pickup != null && slots.Contains(pickup.Ammo))
                pickup = null;

            if (pickup == null)
            {
                ResetHold(AnyHeld());
                return;
            }

            if (slots.HasEmptySlot)
            {
                float sqr = ((Vector2)pickup.transform.position - position).sqrMagnitude;
                if (sqr <= tuning.PickupRadius * tuning.PickupRadius && slots.TryAutoFill(pickup.Ammo))
                    pickup.Consume();
                ResetHold(AnyHeld());
                return;
            }

            NearPickup = pickup;
            UpdateHold(pickup);
        }

        private void UpdateHold(AmmoPickup pickup)
        {
            int held = FirstHeldSlot();
            if (needRelease)
            {
                if (held < 0)
                    needRelease = false;
                ResetHold(true);
                return;
            }

            if (held < 0 || held != holdSlot)
            {
                holdSlot = held;
                holdTime = 0f;
                if (held < 0)
                    return;
            }

            holdTime += Time.deltaTime;
            if (holdTime < tuning.HoldDuration)
                return;

            AmmoTypeData replaced = slots.Replace(holdSlot, pickup.Ammo);
            pickup.Consume();
            Drop(replaced);
            NearPickup = null;
            needRelease = true; // don't chain into swapping the ammo we just dropped
            ResetHold(true);
        }

        private void Drop(AmmoTypeData ammo)
        {
            if (ammo == null)
                return;
            Vector2 offset = Random.insideUnitCircle.normalized * tuning.DropDistance;
            AmmoPickup dropped = Instantiate(pickupPrefab, (Vector2)transform.position + offset, Quaternion.identity);
            dropped.Setup(ammo, tuning, tuning.DropImmunity);
        }

        private void ResetHold(bool keepNeedRelease)
        {
            holdSlot = -1;
            holdTime = 0f;
            if (!keepNeedRelease)
                needRelease = false;
        }

        private int FirstHeldSlot()
        {
            for (int i = 0; i < AmmoSlotSet.Count; i++)
                if (input.IsAmmoHeld(i))
                    return i;
            return -1;
        }

        private bool AnyHeld() => FirstHeldSlot() >= 0;
    }
}
