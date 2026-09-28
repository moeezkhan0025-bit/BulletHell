using UnityEngine;

namespace BulletHell.Pickups
{
    /// <summary>Tuning for currency coins: how they pop out, the magnet that pulls them to the player, and their pool.</summary>
    [CreateAssetMenu(fileName = "CoinTuning", menuName = "BulletHell/Coin Tuning")]
    public sealed class CoinTuning : ScriptableObject
    {
        [Header("Collecting")]
        [Tooltip("Coins inside this range of the player are pulled toward them.")]
        [SerializeField, Min(0.1f)] private float magnetRadius = 3.2f;
        [Tooltip("Coins this close to the player are collected.")]
        [SerializeField, Min(0.05f)] private float collectRadius = 0.45f;
        [Tooltip("Speed a coin starts moving at once the magnet catches it.")]
        [SerializeField, Min(0f)] private float magnetStartSpeed = 4f;
        [SerializeField, Min(0f)] private float magnetMaxSpeed = 16f;
        [SerializeField, Min(0f)] private float magnetAcceleration = 30f;

        [Header("Drop")]
        [Tooltip("How fast a coin pops out of a dying enemy, in a random direction.")]
        [SerializeField, Min(0f)] private float popSpeed = 2.2f;
        [Tooltip("How quickly the pop slows to a stop.")]
        [SerializeField, Min(0f)] private float popDrag = 6f;

        [Header("Look")]
        [SerializeField, Min(0.05f)] private float coinSize = 0.35f;
        [SerializeField] private Color coinColor = new Color(1f, 0.85f, 0.2f);

        [Header("Pool")]
        [SerializeField, Min(1)] private int poolPrewarm = 64;
        [SerializeField, Min(1)] private int poolMax = 512;

        public float MagnetRadius => magnetRadius;
        public float CollectRadius => collectRadius;
        public float MagnetStartSpeed => magnetStartSpeed;
        public float MagnetMaxSpeed => magnetMaxSpeed;
        public float MagnetAcceleration => magnetAcceleration;
        public float PopSpeed => popSpeed;
        public float PopDrag => popDrag;
        public float CoinSize => coinSize;
        public Color CoinColor => coinColor;
        public int PoolPrewarm => poolPrewarm;
        public int PoolMax => poolMax;
    }
}
