using System.Collections.Generic;
using BulletHell.Audio;
using UnityEngine;

namespace BulletHell.Core
{
    /// <summary>
    /// The game's audio: a pool of pre-built audio sources for sound effects (with limits on identical sounds), two sources that
    /// crossfade the music, ducking for stingers and pause, and the volume settings routed to the mixer groups. Gameplay and UI only
    /// ever call <see cref="Play(SfxId)"/> / <see cref="PlayMusic"/>; the clips, limits and tracks are data on the
    /// <see cref="AudioLibrary"/>. Without a library (edit-mode tests) every call is a harmless no-op and only the volumes are stored.
    /// </summary>
    public sealed class AudioService
    {
        private const float DuckFollowSpeed = 4f;     // how fast the music ducks and recovers (fractions per second)
        private const float PauseFollowSpeed = 6f;

        private AudioLibrary library;
        private bool ready;

        // ---- sound effect voices (a fixed pool, never grown during play)
        private AudioSource[] voices;
        private int[] voiceKey;
        private float[] voiceStart;
        private int[] voicePriority;
        private readonly Dictionary<int, float> lastPlayed = new Dictionary<int, float>();

        // ---- music
        private AudioSource[] music;
        private readonly float[] musicGain = new float[2];
        private readonly float[] musicTarget = new float[2];
        private readonly float[] musicTrackVolume = new float[2];
        private int activeMusic;
        private MusicContext context;
        private AudioClip currentClip;
        private float duck = 1f;
        private float duckTarget = 1f;
        private float duckReleaseAt;
        private float pauseFactor = 1f;
        private bool paused;

        public float MasterVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;

        public bool IsReady => ready;
        public MusicContext CurrentMusic => context;
        public AudioClip CurrentMusicClip => currentClip;
        /// <summary>Sound effects that were asked for and dropped by the limits (for the debug overlay and tests).</summary>
        public int DroppedCount { get; private set; }
        public int PlayedCount { get; private set; }

        /// <summary>Builds the source pool and the music sources under <paramref name="host"/>. Safe to call once.</summary>
        public void Initialize(AudioLibrary audioLibrary, GameObject host)
        {
            if (ready || audioLibrary == null || host == null)
                return;
            library = audioLibrary;

            int count = library.VoicePool;
            voices = new AudioSource[count];
            voiceKey = new int[count];
            voiceStart = new float[count];
            voicePriority = new int[count];
            var pool = new GameObject("SfxVoices").transform;
            pool.SetParent(host.transform, false);
            for (int i = 0; i < count; i++)
                voices[i] = NewSource(pool, "Voice" + i, false);

            music = new AudioSource[2];
            var musicRoot = new GameObject("Music").transform;
            musicRoot.SetParent(host.transform, false);
            for (int i = 0; i < 2; i++)
            {
                music[i] = NewSource(musicRoot, "Music" + (char)('A' + i), true);
                music[i].outputAudioMixerGroup = library.MusicGroup;
            }

            host.AddComponent<AudioHost>().Service = this;
            ready = true;
            ApplyVolumes();
        }

        private static AudioSource NewSource(Transform parent, string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;   // the game is 2D: no distance falloff
            return source;
        }

        // ------------------------------------------------------------------ volumes

        /// <summary>Master, music and sound effect volumes (0..1) from the Settings. They drive the mixer's exposed volumes (in dB).</summary>
        public void SetVolumes(float master, float musicVolume, float sfxVolume)
        {
            MasterVolume = Mathf.Clamp01(master);
            MusicVolume = Mathf.Clamp01(musicVolume);
            SfxVolume = Mathf.Clamp01(sfxVolume);
            ApplyVolumes();
        }

        private bool HasMixer => ready && library.Mixer != null;

        private void ApplyVolumes()
        {
            if (HasMixer)
            {
                library.Mixer.SetFloat(AudioLibrary.MasterParameter, SfxVoiceLimiter.ToDecibels(MasterVolume));
                library.Mixer.SetFloat(AudioLibrary.MusicParameter, SfxVoiceLimiter.ToDecibels(MusicVolume));
                library.Mixer.SetFloat(AudioLibrary.SfxParameter, SfxVoiceLimiter.ToDecibels(SfxVolume));
                AudioListener.volume = 1f;
            }
            else
            {
                AudioListener.volume = MasterVolume;   // no mixer (tests, an incomplete setup): master scales everything
            }
        }

        // ------------------------------------------------------------------ sound effects

        public void Play(SfxId id)
        {
            if (!ready || id == SfxId.None)
                return;
            Play(library.Get(id));
        }

        public void Play(SfxData data, float volumeScale = 1f)
        {
            if (!ready || data == null)
                return;
            AudioClip clip = data.PickClip();
            if (clip == null)
                return;
            Start(data.GetInstanceID(), clip, data.Volume * volumeScale, data.PitchRange, data.Bus, data.MaxVoices, data.MinInterval, data.Priority);
            if (data.DuckMusicTo < 1f)
                Duck(data.DuckMusicTo, data.DuckSeconds);
        }

        /// <summary>Plays a bare clip (a UITheme override) through a bus, with the default limits.</summary>
        public void PlayClip(AudioClip clip, AudioBus bus, float volume = 1f)
        {
            if (ready && clip != null)
                Start(clip.GetInstanceID(), clip, volume, Vector2.one, bus, 3, 0.02f, 1);
        }

        private void Start(int key, AudioClip clip, float volume, Vector2 pitch, AudioBus bus, int maxVoices, float minInterval, int priority)
        {
            float now = Time.unscaledTime;
            float since = lastPlayed.TryGetValue(key, out float last) ? now - last : float.MaxValue;

            int active = 0;
            int oldest = -1;
            float oldestAge = 0f;
            for (int i = 0; i < voices.Length; i++)
            {
                if (voiceKey[i] != key || !voices[i].isPlaying)
                    continue;
                active++;
                float age = now - voiceStart[i];
                if (oldest < 0 || age > oldestAge)
                {
                    oldest = i;
                    oldestAge = age;
                }
            }

            VoiceDecision decision = SfxVoiceLimiter.Decide(active, maxVoices, since, minInterval, oldestAge);
            if (decision == VoiceDecision.Drop)
            {
                DroppedCount++;
                return;
            }

            int slot = decision == VoiceDecision.StealOldest ? oldest : FreeVoice(priority, now);
            if (slot < 0)
            {
                DroppedCount++;   // every voice is busy with something at least as important
                return;
            }

            AudioSource source = voices[slot];
            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume) * (HasMixer ? 1f : SfxVolume);
            source.pitch = pitch.x == pitch.y ? pitch.x : Random.Range(pitch.x, pitch.y);
            source.outputAudioMixerGroup = library.GroupFor(bus);
            source.Play();
            voiceKey[slot] = key;
            voiceStart[slot] = now;
            voicePriority[slot] = priority;
            lastPlayed[key] = now;
            PlayedCount++;
        }

        // A silent voice, or else the oldest voice of the lowest priority that is not more important than the new sound.
        private int FreeVoice(int priority, float now)
        {
            int steal = -1;
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying)
                    return i;
                if (voicePriority[i] > priority)
                    continue;
                if (steal < 0 || voicePriority[i] < voicePriority[steal] ||
                    (voicePriority[i] == voicePriority[steal] && voiceStart[i] < voiceStart[steal]))
                    steal = i;
            }
            return steal;
        }

        /// <summary>How many sound effect voices are playing right now.</summary>
        public int ActiveVoices
        {
            get
            {
                int count = 0;
                if (voices != null)
                    for (int i = 0; i < voices.Length; i++)
                        if (voices[i].isPlaying)
                            count++;
                return count;
            }
        }

        // ------------------------------------------------------------------ music

        /// <summary>Crossfades to the music of a context (nothing happens when it already plays). variant picks among several tracks.</summary>
        public void PlayMusic(MusicContext newContext, int variant = 0)
        {
            if (!ready)
                return;
            if (newContext == MusicContext.None)
            {
                StopMusic();
                return;
            }
            AudioClip clip = library.GetMusic(newContext, variant, out float trackVolume);
            if (clip == null || (clip == currentClip && music[activeMusic].isPlaying && musicTarget[activeMusic] > 0f))
            {
                context = newContext;
                return;
            }

            // The incoming track takes the source that is not the active one (it may still be fading out from before).
            int outgoing = activeMusic;
            int incoming = 1 - activeMusic;
            musicTarget[outgoing] = 0f;
            AudioSource source = music[incoming];
            source.Stop();
            source.clip = clip;
            musicTrackVolume[incoming] = trackVolume;
            musicTarget[incoming] = 1f;
            source.volume = 0f;
            musicGain[incoming] = 0f;
            source.Play();
            activeMusic = incoming;
            currentClip = clip;
            context = newContext;
        }

        /// <summary>Fades the music out (game over, leaving a run).</summary>
        public void StopMusic()
        {
            if (!ready)
                return;
            musicTarget[0] = musicTarget[1] = 0f;
            currentClip = null;
            context = MusicContext.None;
        }

        /// <summary>The music drops to a fraction of its volume for a few seconds (stingers do this through <see cref="SfxData.DuckMusicTo"/>).</summary>
        public void Duck(float fraction, float seconds)
        {
            duckTarget = Mathf.Min(duckTarget, Mathf.Clamp01(fraction));
            duckReleaseAt = Mathf.Max(duckReleaseAt, Time.unscaledTime + seconds);
        }

        /// <summary>While paused the music plays quieter.</summary>
        public void SetPaused(bool value) => paused = value;

        /// <summary>Called every frame by the <see cref="AudioHost"/>: moves the crossfade, the duck and the pause dip.</summary>
        public void Tick(float dt)
        {
            if (!ready)
                return;

            if (duckTarget < 1f && Time.unscaledTime >= duckReleaseAt)
                duckTarget = 1f;
            duck = Mathf.MoveTowards(duck, duckTarget, DuckFollowSpeed * dt);
            pauseFactor = Mathf.MoveTowards(pauseFactor, paused ? library.PausedMusicVolume : 1f, PauseFollowSpeed * dt);

            float fadeSpeed = 1f / library.CrossfadeSeconds;
            float manual = HasMixer ? 1f : MusicVolume;
            for (int i = 0; i < 2; i++)
            {
                musicGain[i] = Mathf.MoveTowards(musicGain[i], musicTarget[i], fadeSpeed * dt);
                music[i].volume = musicGain[i] * musicTrackVolume[i] * duck * pauseFactor * manual;
                if (musicGain[i] <= 0f && musicTarget[i] <= 0f && music[i].isPlaying)
                    music[i].Stop();
            }
        }
    }
}
