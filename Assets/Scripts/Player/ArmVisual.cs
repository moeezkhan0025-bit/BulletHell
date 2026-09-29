using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// Look of one spawned arm: hidden, soft-selected (thin dim outline) or locked (thick solid pulsing outline), the
    /// outline (shader) in the arm's ID colour; widths and alphas live in ArmRingTuning.
    /// The root sits at the attach point with +X as the firing direction; Art is rotated by the data's
    /// art rotation and Muzzle sits at its muzzle offset.
    /// To align art: open the Arm prefab, set Preview Data, rotate Art and move Muzzle in the Scene view,
    /// then use the component menu "Save Art Alignment To Data".
    /// </summary>
    public sealed class ArmVisual : MonoBehaviour
    {
        public enum State { Hidden, Selected, Locked }

        [SerializeField] private SpriteRenderer art;
        [Tooltip("Retired: the outline shader replaces the halo sprite. Left empty or disabled.")]
        [SerializeField] private SpriteRenderer halo;
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(0f)] private float selectedScale = 1.15f;
        [Tooltip("Flat-colour material for the see-through silhouette drawn over the body when this arm is behind it (Mat_SpriteCharacter: tint amount 1).")]
        [SerializeField] private Material silhouetteMaterial;
        [SerializeField, Range(0f, 1f)] private float silhouetteAlpha = 0.6f;

        [Tooltip("Editor only: arm shown when editing the prefab. Spawned arms get their data from the loadout.")]
        [SerializeField] private WeaponArmData previewData;

        private ArmInstance instance;
        private WeaponArmData data;

        public WeaponArmData Data => data;
        /// <summary>Run state of this arm (armaments and final stats).</summary>
        public ArmInstance Instance => instance;
        public Transform Muzzle => muzzle;

        public void Setup(ArmInstance armInstance)
        {
            instance = armInstance;
            data = armInstance.Data;
            ApplyData(data);
            SetState(State.Hidden);
        }

        private Color artTint = Color.white;
        private State state = State.Hidden;
        private float depthScale = 1f;
        private float brightness = 1f;
        private ArmRingTuning ring;
        private MaterialPropertyBlock block;
        private SpriteRenderer silhouette;
        private MaterialPropertyBlock silhouetteBlock;
        private bool isBack;
        private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
        private static readonly int TintAmountId = Shader.PropertyToID("_TintAmount");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        /// <summary>The arm tint cosmetic: multiplies the arm art's colours. White = art as drawn.</summary>
        public void SetArtTint(Color tint)
        {
            artTint = tint;
            ApplyArtColor();
        }

        public void SetState(State newState)
        {
            state = newState;
            art.enabled = state != State.Hidden;
            ApplySilhouette();
            if (halo != null)
                halo.enabled = false;
            ApplyScale();
            ApplyOutline();
            enabled = state == State.Locked;
        }

        /// <summary>
        /// Where this arm is on the ring: back-half arms sort behind the body, front-half arms in front, and the
        /// optional depth cue shrinks and darkens arms towards the back of the ring (depth01: 0 back, 1 front).
        /// </summary>
        public void SetDepth(bool back, float depth01, ArmRingTuning ring)
        {
            this.ring = ring;
            isBack = back;
            art.sortingOrder = back ? ring.BackArtOrder : ring.FrontArtOrder;
            depthScale = ring.ScaleAt(depth01);
            brightness = ring.BrightnessAt(depth01);
            ApplyScale();
            ApplySilhouette();
            ApplyArtColor();
            ApplyOutline();
        }

        private void Awake() => enabled = false;

        // Locked: the outline breathes so a committed arm is unmistakable.
        private void Update() => ApplyOutline();

        private void ApplyOutline()
        {
            if (art == null)
                return;
            if (block == null)
                block = new MaterialPropertyBlock();
            float width = 0f;
            Color color = data != null ? data.IdColor : Color.white;
            if (ring != null && state != State.Hidden)
            {
                if (state == State.Locked)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * ring.LockedPulseSpeed);
                    width = ring.LockedOutlineWidth * Mathf.Lerp(1f - ring.LockedPulseAmount, 1f, pulse);
                    color.a = ring.LockedOutlineAlpha;
                }
                else
                {
                    width = ring.SoftOutlineWidth;
                    color.a = ring.SoftOutlineAlpha;
                }
            }
            art.GetPropertyBlock(block);
            block.SetColor(OutlineColorId, color);
            block.SetFloat(OutlineWidthId, width);
            art.SetPropertyBlock(block);
        }

        // An arm behind the body is hidden by it, so while it is selected a flat ID-colour copy is drawn over the body: the
        // chosen arm is always visible.
        private void ApplySilhouette()
        {
            bool show = isBack && state != State.Hidden && silhouetteMaterial != null && ring != null;
            if (silhouette == null)
            {
                if (!show)
                    return;
                var go = new GameObject("Occluded");
                go.transform.SetParent(art.transform, false);
                silhouette = go.AddComponent<SpriteRenderer>();
                silhouette.sharedMaterial = silhouetteMaterial;
                silhouetteBlock = new MaterialPropertyBlock();
            }
            silhouette.enabled = show;
            if (!show)
                return;
            silhouette.sprite = art.sprite;
            silhouette.sortingLayerID = art.sortingLayerID;
            silhouette.sortingOrder = ring.FrontArtOrder + 2;
            Color id = data != null ? data.IdColor : Color.white;
            silhouette.color = new Color(1f, 1f, 1f, silhouetteAlpha);
            silhouette.GetPropertyBlock(silhouetteBlock);
            silhouetteBlock.SetColor(TintColorId, id);
            silhouetteBlock.SetFloat(TintAmountId, 1f);
            silhouette.SetPropertyBlock(silhouetteBlock);
        }

        private void ApplyScale() =>
            transform.localScale = Vector3.one * ((state == State.Hidden ? 1f : selectedScale) * depthScale);

        private void ApplyArtColor()
        {
            Color color = artTint;
            color.r *= brightness;
            color.g *= brightness;
            color.b *= brightness;
            art.color = color;
        }

        private void ApplyData(WeaponArmData armData)
        {
            if (armData == null)
                return;
            art.sprite = armData.Sprite;
            art.transform.localRotation = Quaternion.Euler(0f, 0f, armData.ArtRotation);
            muzzle.localPosition = armData.MuzzleOffset;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && art != null && muzzle != null)
                ApplyData(previewData);
        }

        [ContextMenu("Save Art Alignment To Data")]
        private void SaveArtAlignment()
        {
            WeaponArmData target = Application.isPlaying ? data : previewData;
            if (target == null)
            {
                Debug.LogWarning("ArmVisual: set Preview Data before saving art alignment.", this);
                return;
            }
            float rotation = Mathf.DeltaAngle(0f, art.transform.localEulerAngles.z);
            target.SetArtAlignment(rotation, muzzle.localPosition);
            Debug.Log($"Saved art rotation {rotation:0.#} and muzzle {(Vector2)muzzle.localPosition} to {target.name}.", target);
        }

        private void OnDrawGizmosSelected()
        {
            if (muzzle == null)
                return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(muzzle.position, 0.05f);
            Gizmos.DrawLine(muzzle.position, muzzle.position + transform.right * 0.3f);
        }
#endif
    }
}
