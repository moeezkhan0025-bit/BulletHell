using UnityEngine;

namespace BulletHell.Weapons
{
    /// <summary>Ammo type: defines projectile look and behaviour. Arm stats scale it. Minimal for M2 (basic straight shot); grows in M3.</summary>
    [CreateAssetMenu(fileName = "Ammo_", menuName = "BulletHell/Ammo Type Data")]
    public sealed class AmmoTypeData : ScriptableObject
    {
        [SerializeField] private string displayName = "Basic";
        [SerializeField] private Sprite projectileSprite;
        [Tooltip("Safety net: a projectile that somehow never leaves the screen is released after this long.")]
        [SerializeField, Min(0.1f)] private float maxLifetime = 6f;

        public string DisplayName => displayName;
        public Sprite ProjectileSprite => projectileSprite;
        public float MaxLifetime => maxLifetime;
    }
}
