using UnityEngine;

namespace BulletHell.Pickups
{
    /// <summary>Tuning for ammo pickups: pickup ranges, hold-to-replace time and dropped-ammo behaviour.</summary>
    [CreateAssetMenu(fileName = "PickupTuning", menuName = "BulletHell/Pickup Tuning")]
    public sealed class PickupTuning : ScriptableObject
    {
        [Tooltip("Walking this close to a pickup auto-fills an empty ammo slot.")]
        [SerializeField, Min(0.05f)] private float pickupRadius = 0.6f;
        [Tooltip("Within this range the pickup prompt shows and holding a face button swaps ammo.")]
        [SerializeField, Min(0.05f)] private float promptRadius = 1.2f;
        [Tooltip("Seconds a face button must be held near a pickup to replace that slot.")]
        [SerializeField, Min(0.05f)] private float holdDuration = 0.75f;
        [Tooltip("How far from the player replaced ammo lands.")]
        [SerializeField, Min(0f)] private float dropDistance = 1.4f;
        [Tooltip("A freshly dropped pickup can't be grabbed for this long.")]
        [SerializeField, Min(0f)] private float dropImmunity = 1.5f;
        [Tooltip("World size of a pickup sprite.")]
        [SerializeField, Min(0.05f)] private float visualSize = 0.45f;
        [Tooltip("Warn if more pickups than this exist at once.")]
        [SerializeField, Min(1)] private int warnPickupCount = 32;

        public float PickupRadius => pickupRadius;
        public float PromptRadius => Mathf.Max(promptRadius, pickupRadius);
        public float HoldDuration => holdDuration;
        public float DropDistance => dropDistance;
        public float DropImmunity => dropImmunity;
        public float VisualSize => visualSize;
        public int WarnPickupCount => warnPickupCount;
    }
}
