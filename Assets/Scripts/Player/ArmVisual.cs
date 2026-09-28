using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// Look of one spawned arm: hidden, soft-selected (dim halo) or locked (solid halo), halo in the arm's ID colour.
    /// The root sits at the attach point with +X as the firing direction; Art is rotated by the data's
    /// art rotation and Muzzle sits at its muzzle offset.
    /// To align art: open the Arm prefab, set Preview Data, rotate Art and move Muzzle in the Scene view,
    /// then use the component menu "Save Art Alignment To Data".
    /// </summary>
    public sealed class ArmVisual : MonoBehaviour
    {
        public enum State { Hidden, Selected, Locked }

        [SerializeField] private SpriteRenderer art;
        [SerializeField] private SpriteRenderer halo;
        [SerializeField] private Transform muzzle;
        [SerializeField, Range(0f, 1f)] private float selectedHaloAlpha = 0.3f;
        [SerializeField, Range(0f, 1f)] private float lockedHaloAlpha = 0.9f;
        [SerializeField, Min(0f)] private float selectedScale = 1.15f;

        [Tooltip("Editor only: arm shown when editing the prefab. Spawned arms get their data from the loadout.")]
        [SerializeField] private WeaponArmData previewData;

        private ArmInstance instance;
        private WeaponArmData data;

        public WeaponArmData Data => data;
        /// <summary>Run state of this arm (upgrades and final stats).</summary>
        public ArmInstance Instance => instance;
        public Transform Muzzle => muzzle;

        public void Setup(ArmInstance armInstance)
        {
            instance = armInstance;
            data = armInstance.Data;
            ApplyData(data);
            SetState(State.Hidden);
        }

        public void SetState(State state)
        {
            art.enabled = state != State.Hidden;
            halo.enabled = state != State.Hidden;
            Color color = data != null ? data.IdColor : Color.white;
            color.a = state == State.Locked ? lockedHaloAlpha : selectedHaloAlpha;
            halo.color = color;
            transform.localScale = Vector3.one * (state == State.Hidden ? 1f : selectedScale);
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
