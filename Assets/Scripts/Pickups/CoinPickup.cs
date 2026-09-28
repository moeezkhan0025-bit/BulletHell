using UnityEngine;

namespace BulletHell.Pickups
{
    /// <summary>A currency coin's state. The CoinField moves and collects every coin from one loop, so this has no Update of its own.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CoinPickup : MonoBehaviour
    {
        public int Value { get; set; }
        /// <summary>Pop-out velocity, slowed by drag until the coin rests.</summary>
        public Vector2 Velocity { get; set; }
        /// <summary>True once the magnet caught this coin: it then keeps chasing the player.</summary>
        public bool Engaged { get; set; }
        public float MagnetSpeed { get; set; }
        /// <summary>Index in the CoinField's active list.</summary>
        public int ActiveIndex { get; set; } = -1;
    }
}
