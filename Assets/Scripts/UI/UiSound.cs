using BulletHell.Core;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>The UI events that have a sound hook. Clips are assigned on the UITheme; unassigned hooks are silent.</summary>
    public enum UiSoundKind { Focus, Confirm, Back, Buy, Equip, Error }

    /// <summary>
    /// Plays UI sounds. Focus, confirm (buttons) and back (Circle) are automatic through the shared components; the Shop and
    /// Armory call Buy / Equip / Error. Everything is a no-op until clips exist, but the hooks and counters are live.
    /// </summary>
    public static class UiSound
    {
        private static AudioSource source;
        private static float lastFocusTime = -1f;

        /// <summary>The last sound requested, whether or not a clip is assigned (for tests and the debug overlay).</summary>
        public static UiSoundKind? Last { get; private set; }
        public static int RequestCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            source = null;
            Last = null;
            RequestCount = 0;
            lastFocusTime = -1f;
        }

        public static void Play(UiSoundKind kind)
        {
            // Focus moves can arrive several per frame when a screen opens; one blip is enough.
            if (kind == UiSoundKind.Focus)
            {
                if (Time.unscaledTime - lastFocusTime < 0.03f)
                    return;
                lastFocusTime = Time.unscaledTime;
            }

            Last = kind;
            RequestCount++;

            UITheme theme = UITheme.Current;
            AudioClip clip = theme != null ? theme.GetSound(kind) : null;
            if (clip == null || !Application.isPlaying)
                return;

            if (source == null)
            {
                var go = new GameObject("UiSound");
                Object.DontDestroyOnLoad(go);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
            }
            source.PlayOneShot(clip, GameServices.Ensure().Audio.SfxVolume);
        }
    }
}
