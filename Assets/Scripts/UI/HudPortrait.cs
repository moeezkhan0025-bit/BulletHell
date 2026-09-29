using BulletHell.Cosmetics;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>The gladiator's portrait, dressed with the current cosmetics from the profile (the same items as the player).</summary>
    public sealed class HudPortrait : MonoBehaviour
    {
        [SerializeField] private Image body;
        [SerializeField] private Image headgear;
        [SerializeField] private Image cape;

        public void Apply(ProfileService profile)
        {
            CosmeticData coating = profile.Get(CosmeticSlot.CandyCoating);
            body.color = coating != null ? coating.Tint : Color.white;
            ApplyLayer(headgear, profile.Get(CosmeticSlot.Headgear));
            ApplyLayer(cape, profile.Get(CosmeticSlot.Cape));
        }

        private static void ApplyLayer(Image layer, CosmeticData item)
        {
            layer.sprite = item != null ? item.Sprite : null;
            layer.color = item != null ? item.Tint : Color.white;
            layer.enabled = layer.sprite != null;
        }
    }
}
