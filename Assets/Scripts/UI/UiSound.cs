using BulletHell.Core;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>The UI events that have a sound hook. Clips are assigned on the UITheme; unassigned hooks are silent.</summary>
    public enum UiSoundKind { Focus, Confirm, Back, Buy, Equip, Error }

    /// <summary>
    /// Plays UI sounds. Focus, confirm (buttons) and back (Circle) are automatic through the shared components; the Shop and
    /// Armory call Buy / Equip / Error. The sounds come from the AudioLibrary (UI bus); a clip on the UITheme overrides one.
    /// </summary>
    public static class UiSound
    {
        private static float lastFocusTime = -1f;

        /// <summary>The last sound requested, whether or not a clip is assigned (for tests and the debug overlay).</summary>
        public static UiSoundKind? Last { get; private set; }
        public static int RequestCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Last = null;
            RequestCount = 0;
            lastFocusTime = -1f;
        }

        /// <summary>The library sound of each UI event.</summary>
        public static BulletHell.Audio.SfxId SfxFor(UiSoundKind kind)
        {
            switch (kind)
            {
                case UiSoundKind.Focus: return BulletHell.Audio.SfxId.UiFocus;
                case UiSoundKind.Confirm: return BulletHell.Audio.SfxId.UiConfirm;
                case UiSoundKind.Back: return BulletHell.Audio.SfxId.UiBack;
                case UiSoundKind.Buy: return BulletHell.Audio.SfxId.UiBuy;
                case UiSoundKind.Equip: return BulletHell.Audio.SfxId.UiEquip;
                default: return BulletHell.Audio.SfxId.UiError;
            }
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

            if (!Application.isPlaying)
                return;

            // A clip assigned on the UITheme overrides the library sound (the look can ship its own); both go through the pooled, limited voices.
            AudioService audio = GameServices.Ensure().Audio;
            UITheme theme = UITheme.Current;
            AudioClip clip = theme != null ? theme.GetSound(kind) : null;
            if (clip != null)
                audio.PlayClip(clip, BulletHell.Audio.AudioBus.Ui);
            else
                audio.Play(SfxFor(kind));
        }
    }
}
