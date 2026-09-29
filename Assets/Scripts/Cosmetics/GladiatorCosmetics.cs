using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Cosmetics
{
    /// <summary>
    /// Dresses a gladiator from the profile: the coating tints the body, headgear and cape are their own sprite layers,
    /// and the arm tint is handed to the arms (ArmTint, plus any preview arm renderers listed here). The same component
    /// is on the player in the Game scene (applies the profile when it wakes, before PlayerHealth caches the body
    /// colour) and on the customization preview (the screen calls Apply when the look changes).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class GladiatorCosmetics : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer headgear;
        [SerializeField] private SpriteRenderer cape;
        [Tooltip("Arm sprites that exist in the scene all the time (the customization preview). Spawned arms ask for ArmTint.")]
        [SerializeField] private SpriteRenderer[] previewArms = new SpriteRenderer[0];
        [SerializeField] private bool applyProfileOnAwake = true;

        public Color ArmTint { get; private set; } = Color.white;

        private void Awake()
        {
            if (applyProfileOnAwake)
                Apply(GameServices.Ensure().Profile);
        }

        public void Apply(ProfileService profile)
        {
            body.color = TintOf(profile.Get(CosmeticSlot.CandyCoating));
            ApplyLayer(headgear, profile.Get(CosmeticSlot.Headgear));
            ApplyLayer(cape, profile.Get(CosmeticSlot.Cape));

            ArmTint = TintOf(profile.Get(CosmeticSlot.ArmTint));
            for (int i = 0; i < previewArms.Length; i++)
                previewArms[i].color = ArmTint;
        }

        private static Color TintOf(CosmeticData item) => item != null ? item.Tint : Color.white;

        private static void ApplyLayer(SpriteRenderer layer, CosmeticData item)
        {
            if (layer == null)
                return;
            layer.sprite = item != null ? item.Sprite : null;
            layer.color = TintOf(item);
            layer.enabled = layer.sprite != null;
        }
    }
}
