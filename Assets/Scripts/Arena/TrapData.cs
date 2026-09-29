using UnityEngine;

namespace BulletHell.Arena
{
    public enum TrapKind
    {
        /// <summary>Round floor vent: telegraphs, then bursts once over its area.</summary>
        Vent,
        /// <summary>Long thin box (a spike/skewer line): telegraphs, then strikes once along its length.</summary>
        Skewer,
        /// <summary>Round hazard zone: telegraphs, then ticks damage for as long as it is active.</summary>
        Zone,
    }

    /// <summary>
    /// One kind of trap. It hurts anything inside its area, the player and enemies alike, so luring enemies into it
    /// works. It is always telegraphed before it goes off, and it does nothing during the round intro.
    /// </summary>
    [CreateAssetMenu(fileName = "Trap_", menuName = "BulletHell/Trap Data")]
    public sealed class TrapData : ScriptableObject
    {
        [SerializeField] private TrapKind kind = TrapKind.Vent;
        [Tooltip("Vent / Zone: x is the radius. Skewer: x is the width and y the length (before rotation).")]
        [SerializeField] private Vector2 size = new Vector2(1f, 1f);
        [Tooltip("Optional art. Empty = the placeholder circle / square.")]
        [SerializeField] private Sprite sprite;

        [Header("Damage")]
        [Tooltip("Hits taken by the player per strike (the player's invulnerability frames still apply).")]
        [SerializeField, Min(0f)] private float damageToPlayer = 1f;
        [Tooltip("Hit points taken by an enemy per strike.")]
        [SerializeField, Min(0f)] private float damageToEnemies = 10f;
        [SerializeField] private bool hurtsPlayer = true;
        [SerializeField] private bool hurtsEnemies = true;

        [Header("Timing (seconds)")]
        [Tooltip("After combat begins, before the first telegraph.")]
        [SerializeField, Min(0f)] private float startDelay = 1.5f;
        [SerializeField, Min(0.1f)] private float telegraphSeconds = 1.2f;
        [SerializeField, Min(0.05f)] private float activeSeconds = 0.4f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 3f;
        [Tooltip("While active: a further strike every this many seconds. 0 = one strike per activation.")]
        [SerializeField, Min(0f)] private float hitInterval;

        [Header("Look")]
        [SerializeField] private Color telegraphColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color activeColor = new Color(1f, 0.25f, 0.15f);

        public TrapKind Kind => kind;
        public bool IsBox => kind == TrapKind.Skewer;
        public Vector2 Size => size;
        public Sprite Sprite => sprite;
        public float DamageToPlayer => damageToPlayer;
        public float DamageToEnemies => damageToEnemies;
        public bool HurtsPlayer => hurtsPlayer;
        public bool HurtsEnemies => hurtsEnemies;
        public float StartDelay => startDelay;
        public float TelegraphSeconds => telegraphSeconds;
        public float ActiveSeconds => activeSeconds;
        public float CooldownSeconds => cooldownSeconds;
        public float HitInterval => hitInterval;
        public Color TelegraphColor => telegraphColor;
        public Color ActiveColor => activeColor;

        public TrapTimer CreateTimer(float extraStartDelay = 0f) =>
            new TrapTimer(startDelay + extraStartDelay, telegraphSeconds, activeSeconds, cooldownSeconds, hitInterval);

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the setup script and tests.</summary>
        public void Configure(TrapKind newKind, Vector2 newSize, float toPlayer, float toEnemies,
                              float delay, float telegraph, float active, float cooldown, float interval)
        {
            kind = newKind;
            size = newSize;
            damageToPlayer = toPlayer;
            damageToEnemies = toEnemies;
            startDelay = delay;
            telegraphSeconds = telegraph;
            activeSeconds = active;
            cooldownSeconds = cooldown;
            hitInterval = interval;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
