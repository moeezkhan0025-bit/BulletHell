namespace BulletHell.Audio
{
    /// <summary>What to do with a sound that wants to play.</summary>
    public enum VoiceDecision { Play, Drop, StealOldest }

    /// <summary>
    /// The rules that keep identical sounds from piling up, as plain maths so they can be tested: a sound repeats no faster than its
    /// minimum interval, no more than its maximum number of copies play at once, and when the limit is hit the oldest copy is cut
    /// only if it has already played for a moment (otherwise the new one is dropped, so a machine gun does not chop its own sound).
    /// </summary>
    public static class SfxVoiceLimiter
    {
        /// <summary>A copy younger than this is never cut to make room.</summary>
        public const float MinStealAge = 0.06f;

        public static VoiceDecision Decide(int activeCopies, int maxVoices, float secondsSinceLastPlay, float minInterval, float oldestCopyAge)
        {
            if (minInterval > 0f && secondsSinceLastPlay < minInterval)
                return VoiceDecision.Drop;
            if (activeCopies < maxVoices)
                return VoiceDecision.Play;
            return oldestCopyAge >= MinStealAge ? VoiceDecision.StealOldest : VoiceDecision.Drop;
        }

        /// <summary>Linear volume (0..1) to mixer decibels; silence is -80 dB.</summary>
        public static float ToDecibels(float linear) => linear <= 0.0001f ? -80f : 20f * UnityEngine.Mathf.Log10(linear);
    }
}
