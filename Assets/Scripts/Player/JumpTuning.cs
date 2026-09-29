using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// Every number of the jump. The jump is fake height: the player's root stays on the ground plane and only the
    /// visuals rise, so all of this is looks plus the few rules that change while airborne.
    /// </summary>
    [CreateAssetMenu(fileName = "JumpTuning", menuName = "BulletHell/Jump Tuning")]
    public sealed class JumpTuning : ScriptableObject
    {
        [Header("Arc")]
        [Tooltip("Seconds from takeoff to landing.")]
        [SerializeField, Min(0.1f)] private float airtime = 0.45f;
        [Tooltip("Peak height above the ground, in world units.")]
        [SerializeField, Min(0f)] private float maxHeight = 0.9f;
        [Tooltip("Height over the jump: X = 0..1 of the airtime, Y = 0..1 of the peak height.")]
        [SerializeField] private AnimationCurve heightCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 4f), new Keyframe(0.5f, 1f, 0f, 0f), new Keyframe(1f, 0f, -4f, 0f));
        [Tooltip("Seconds after landing before the next jump is possible.")]
        [SerializeField, Min(0f)] private float cooldownAfterLanding = 0.2f;

        [Header("Air rules")]
        [Tooltip("Move speed while airborne, as a multiple of the ground speed (1 = full steering).")]
        [SerializeField, Range(0f, 1f)] private float airControl = 1f;
        [Tooltip("The body must be at least this high (world units) to pass over Low obstacles. ART_SPEC: the apex lifts the body >= 0.7 P; this must stay below Max Height.")]
        [SerializeField, Min(0f)] private float lowClearHeight = 0.45f;
        [Tooltip("On: enemy bullets pass through the player while airborne. Off (default): still hit, it stays a bullet hell.")]
        [SerializeField] private bool jumpDodgesBullets;

        [Header("Look")]
        [Tooltip("Extra scale of the body at the apex (1 = none).")]
        [SerializeField, Min(1f)] private float apexScale = 1.12f;
        [Tooltip("Scale on takeoff (wide and short), easing back to 1.")]
        [SerializeField] private Vector2 takeoffSquash = new Vector2(1.2f, 0.8f);
        [SerializeField, Min(0.01f)] private float takeoffSquashSeconds = 0.1f;
        [Tooltip("Scale on landing (wide and short), easing back to 1.")]
        [SerializeField] private Vector2 landingSquash = new Vector2(1.3f, 0.7f);
        [SerializeField, Min(0.01f)] private float landingSquashSeconds = 0.14f;
        [Tooltip("The ground shadow scale at the apex, as a fraction of its grounded scale.")]
        [SerializeField, Range(0.1f, 1f)] private float shadowScaleAtApex = 0.6f;
        [Tooltip("The ground shadow opacity at the apex, as a fraction of its grounded opacity.")]
        [SerializeField, Range(0f, 1f)] private float shadowAlphaAtApex = 0.45f;

        [Header("Sorting")]
        [Tooltip("The body draws above the characters near it once it is at least this high (0..1 of the peak).")]
        [SerializeField, Range(0f, 1f)] private float airborneSortHeight = 0.08f;

        [Header("Landing dust")]
        [SerializeField] private Sprite dustSprite;
        [SerializeField] private Color dustColor = new Color(0.9f, 0.82f, 0.68f, 0.7f);
        [SerializeField, Range(0, 12)] private int dustCount = 6;
        [SerializeField, Min(0.05f)] private float dustSeconds = 0.4f;
        [SerializeField, Min(0.01f)] private float dustStartSize = 0.16f;
        [SerializeField, Min(0.01f)] private float dustEndSize = 0.42f;
        [Tooltip("How far the puffs drift out from the landing spot.")]
        [SerializeField, Min(0f)] private float dustSpread = 0.55f;

        public float Airtime => airtime;
        public float MaxHeight => maxHeight;
        public float CooldownAfterLanding => cooldownAfterLanding;
        public float AirControl => airControl;
        public bool JumpDodgesBullets => jumpDodgesBullets;
        public float LowClearHeight => Mathf.Min(lowClearHeight, maxHeight * 0.95f);
        public float ApexScale => apexScale;
        public Vector2 TakeoffSquash => takeoffSquash;
        public float TakeoffSquashSeconds => takeoffSquashSeconds;
        public Vector2 LandingSquash => landingSquash;
        public float LandingSquashSeconds => landingSquashSeconds;
        public float ShadowScaleAtApex => shadowScaleAtApex;
        public float ShadowAlphaAtApex => shadowAlphaAtApex;
        public float AirborneSortHeight => airborneSortHeight;
        public Sprite DustSprite => dustSprite;
        public Color DustColor => dustColor;
        public int DustCount => dustCount;
        public float DustSeconds => dustSeconds;
        public float DustStartSize => dustStartSize;
        public float DustEndSize => dustEndSize;
        public float DustSpread => dustSpread;

        /// <summary>Height 0..1 of the peak at a point of the jump (0 = takeoff, 1 = landing).</summary>
        public float HeightAt(float progress01) => Mathf.Clamp01(heightCurve.Evaluate(Mathf.Clamp01(progress01)));

#if UNITY_EDITOR
        public void SetDustSprite(Sprite sprite)
        {
            dustSprite = sprite;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
