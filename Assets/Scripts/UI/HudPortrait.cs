using BulletHell.Core;
using BulletHell.Cosmetics;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// The gladiator's portrait. With the customizer parts on (GameConfig.ShowCustomizationInGame) it is built from the same
    /// parts as the player: the chosen head and Accessory 1 over the portrait frame. With them off it shows the player's own
    /// sprite, so the portrait matches the on-screen player. The parts are drawn on the full character canvas, so both
    /// images are the same size and offset and the portrait window shows only the head area.
    /// </summary>
    public sealed class HudPortrait : MonoBehaviour
    {
        [SerializeField] private Image head;
        [SerializeField] private Image accessory;

        public void Apply(ProfileService profile)
        {
            if (GameServices.Ensure().Config.ShowCustomizationInGame)
            {
                ApplyLayer(head, profile.SpriteOf(CosmeticSlot.Head));
                ApplyLayer(accessory, profile.SpriteOf(CosmeticSlot.Accessory1));
            }
            else
            {
                ApplyLayer(head, profile.DefaultSpriteOf(CosmeticSlot.Body));
                ApplyLayer(accessory, null);
            }
        }

        private static void ApplyLayer(Image layer, Sprite sprite)
        {
            layer.sprite = sprite;
            layer.enabled = sprite != null;
        }
    }
}
