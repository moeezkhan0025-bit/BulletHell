using UnityEngine;

namespace BulletHell.Audio
{
    /// <summary>
    /// One sound cue: its clips (a random one plays), volume and pitch range, and the limits that keep a heavy fight from turning into
    /// noise: how many copies of it may play at once and how soon it may repeat. Clips are placeholders (CC0, see Docs/CREDITS.md);
    /// swapping them is an asset change, not a code change.
    /// </summary>
    [CreateAssetMenu(fileName = "Sfx_", menuName = "BulletHell/Audio/Sfx Data")]
    public sealed class SfxData : ScriptableObject
    {
        [Tooltip("Variations: a random one plays each time.")]
        [SerializeField] private AudioClip[] clips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [Tooltip("Random pitch per play (min, max); 1 = unchanged.")]
        [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        [SerializeField] private AudioBus bus = AudioBus.Sfx;
        [Tooltip("At most this many copies of this sound play at the same time (the oldest is cut when a new one arrives and it is old enough; otherwise the new one is dropped).")]
        [SerializeField, Min(1)] private int maxVoices = 4;
        [Tooltip("Shortest time between two plays of this sound, in seconds (0 = no limit).")]
        [SerializeField, Min(0f)] private float minInterval;
        [Tooltip("Who wins when every voice is busy: a sound only steals a voice of the same or lower priority.")]
        [SerializeField, Range(0, 3)] private int priority = 1;
        [Tooltip("Stingers: the music drops to this fraction of its volume while the sound plays (1 = no ducking).")]
        [SerializeField, Range(0f, 1f)] private float duckMusicTo = 1f;
        [SerializeField, Min(0f)] private float duckSeconds = 1.5f;

        public int ClipCount => clips != null ? clips.Length : 0;
        public float Volume => volume;
        public Vector2 PitchRange => pitchRange;
        public AudioBus Bus => bus;
        public int MaxVoices => maxVoices;
        public float MinInterval => minInterval;
        public int Priority => priority;
        public float DuckMusicTo => duckMusicTo;
        public float DuckSeconds => duckSeconds;

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0)
                return null;
            return clips[clips.Length == 1 ? 0 : Random.Range(0, clips.Length)];
        }

#if UNITY_EDITOR
        public void Configure(AudioClip[] newClips, float newVolume, Vector2 newPitch, AudioBus newBus, int newMaxVoices, float newMinInterval,
                              int newPriority, float newDuck = 1f, float newDuckSeconds = 1.5f)
        {
            clips = newClips;
            volume = newVolume;
            pitchRange = newPitch;
            bus = newBus;
            maxVoices = newMaxVoices;
            minInterval = newMinInterval;
            priority = newPriority;
            duckMusicTo = newDuck;
            duckSeconds = newDuckSeconds;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
