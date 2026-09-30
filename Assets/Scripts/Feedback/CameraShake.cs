using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>
    /// Trauma-based screen shake for the fixed orthographic camera. Instead of moving the camera transform (the player
    /// clamp, wave spawner and bullet despawn all read its position) the shake shifts the projection matrix, so gameplay
    /// bounds never wobble. Runs on unscaled time so it keeps decaying during a hitstop, and is off when the Screen shake
    /// setting is off. Add trauma with CameraShake.Add(0..1).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraShake : MonoBehaviour
    {
        private static CameraShake active;

        private Camera cam;
        private FeedbackTuning tuning;
        private float trauma;
        private bool matrixDirty;
        private float seed;

        /// <summary>Adds shake strength (0..1). Ignored when there is no shaking camera or shake is disabled in Settings.</summary>
        public static void Add(float amount)
        {
            if (active != null && amount > 0f)
                active.AddTrauma(amount);
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            seed = Random.value * 100f;
        }

        private void OnEnable() => active = this;

        private void OnDisable()
        {
            if (active == this)
                active = null;
            Restore();
        }

        private void AddTrauma(float amount)
        {
            GameServices services = GameServices.Ensure();
            if (services.Settings.Current.shakeIntensity <= 0f)
                return;
            trauma = Mathf.Clamp01(trauma + amount);
        }

        private void LateUpdate()
        {
            if (trauma <= 0f)
            {
                Restore();
                return;
            }
            if (tuning == null)
                tuning = GameServices.Ensure().Config.Feedback;

            float dt = Time.unscaledDeltaTime;
            trauma = Mathf.Max(0f, trauma - tuning.ShakeDecay * dt);

            float strength = trauma * trauma;
            float intensity = GameServices.Ensure().Settings.Current.shakeIntensity;   // Settings > Gameplay > Screen Shake (0 = none)
            float t = Time.unscaledTime * tuning.ShakeFrequency;
            float x = (Mathf.PerlinNoise(seed, t) * 2f - 1f) * strength * tuning.ShakeMaxOffset * intensity;
            float y = (Mathf.PerlinNoise(seed + 50f, t) * 2f - 1f) * strength * tuning.ShakeMaxOffset * intensity;

            cam.ResetProjectionMatrix();
            Matrix4x4 projection = cam.projectionMatrix;
            projection.m03 += x / (cam.orthographicSize * cam.aspect);
            projection.m13 += y / cam.orthographicSize;
            cam.projectionMatrix = projection;
            matrixDirty = true;
        }

        private void Restore()
        {
            if (!matrixDirty || cam == null)
                return;
            cam.ResetProjectionMatrix();
            matrixDirty = false;
        }
    }
}
