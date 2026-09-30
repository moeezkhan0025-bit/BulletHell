using System;
using BulletHell.Audio;
using BulletHell.Core;
using BulletHell.UI;
using BulletHell.Weapons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BulletHell.Tests
{
    /// <summary>D4 audio: the limits on identical sounds, the decibel conversion, and that the shipped library answers every sound and track the game asks for.</summary>
    public class AudioTests
    {
        private static AudioLibrary Library() => AssetDatabase.LoadAssetAtPath<AudioLibrary>("Assets/Data/Audio/AudioLibrary.asset");

        // ---- the limiter

        [Test]
        public void ASoundPlaysWhileUnderItsVoiceLimit()
        {
            Assert.AreEqual(VoiceDecision.Play, SfxVoiceLimiter.Decide(0, 3, float.MaxValue, 0f, 0f));
            Assert.AreEqual(VoiceDecision.Play, SfxVoiceLimiter.Decide(2, 3, 1f, 0.05f, 1f));
        }

        [Test]
        public void AtTheLimitTheOldestCopyIsCutOnlyIfItHasPlayedForAMoment()
        {
            Assert.AreEqual(VoiceDecision.StealOldest, SfxVoiceLimiter.Decide(3, 3, 1f, 0f, SfxVoiceLimiter.MinStealAge + 0.01f));
            // A machine gun must not chop the copy it started a blink ago: the new one is dropped instead.
            Assert.AreEqual(VoiceDecision.Drop, SfxVoiceLimiter.Decide(3, 3, 1f, 0f, 0.01f));
        }

        [Test]
        public void ASoundCannotRepeatFasterThanItsMinimumInterval()
        {
            Assert.AreEqual(VoiceDecision.Drop, SfxVoiceLimiter.Decide(0, 4, 0.02f, 0.05f, 0f));
            Assert.AreEqual(VoiceDecision.Play, SfxVoiceLimiter.Decide(0, 4, 0.06f, 0.05f, 0f));
            Assert.AreEqual(VoiceDecision.Play, SfxVoiceLimiter.Decide(0, 4, 0f, 0f, 0f));   // no interval set: no limit
        }

        [Test]
        public void VolumeSlidersBecomeMixerDecibels()
        {
            Assert.AreEqual(0f, SfxVoiceLimiter.ToDecibels(1f), 0.001f);
            Assert.AreEqual(-6.02f, SfxVoiceLimiter.ToDecibels(0.5f), 0.01f);
            Assert.AreEqual(-20f, SfxVoiceLimiter.ToDecibels(0.1f), 0.001f);
            Assert.AreEqual(-80f, SfxVoiceLimiter.ToDecibels(0f));          // the slider at zero is silence, not -infinity
        }

        // ---- the service without a library

        [Test]
        public void WithoutALibraryTheServiceIsSilentButKeepsTheVolumes()
        {
            float listener = AudioListener.volume;
            try
            {
                var audio = new AudioService();
                audio.Play(SfxId.EnemyHit);            // no-ops, no exceptions
                audio.PlayMusic(MusicContext.Combat);
                audio.StopMusic();
                audio.SetVolumes(0.5f, 0.3f, 0.7f);
                Assert.AreEqual(0.3f, audio.MusicVolume, 0.0001f);
                Assert.AreEqual(0.7f, audio.SfxVolume, 0.0001f);
                Assert.AreEqual(0.5f, AudioListener.volume, 0.0001f);   // with no mixer, master scales everything
                Assert.IsFalse(audio.IsReady);
            }
            finally
            {
                AudioListener.volume = listener;
            }
        }

        // ---- the shipped data

        [Test]
        public void EveryGameSoundHasClipsAndTheFourMusicContextsHaveTracks()
        {
            AudioLibrary library = Library();
            Assert.IsNotNull(library);
            foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
            {
                if (id == SfxId.None)
                    continue;
                SfxData data = library.Get(id);
                Assert.IsNotNull(data, id + " has no SfxData");
                Assert.IsNotNull(data.PickClip(), id + " has no clip");
                Assert.GreaterOrEqual(data.MaxVoices, 1);
            }
            foreach (MusicContext context in new[] { MusicContext.Menu, MusicContext.Combat, MusicContext.Boss, MusicContext.Shop })
                Assert.IsNotNull(library.GetMusic(context, 0, out _), context + " has no track");
        }

        [Test]
        public void CombatMusicPicksATrackPerRoundAndWrapsAround()
        {
            AudioLibrary library = Library();
            AudioClip round1 = library.GetMusic(MusicContext.Combat, 0, out _);
            AudioClip round2 = library.GetMusic(MusicContext.Combat, 1, out _);
            AudioClip round3 = library.GetMusic(MusicContext.Combat, 2, out _);
            Assert.AreNotEqual(round1, round2);
            Assert.AreEqual(round1, round3);
            Assert.IsNull(library.GetMusic(MusicContext.None, 0, out _));
        }

        [Test]
        public void TheLibraryRoutesEachBusToItsMixerGroupUnderTheRightSlider()
        {
            AudioLibrary library = Library();
            Assert.IsNotNull(library.Mixer);
            Assert.AreEqual("Music", library.MusicGroup.name);
            Assert.AreEqual("Sfx", library.GroupFor(AudioBus.Sfx).name);
            Assert.AreEqual("Ui", library.GroupFor(AudioBus.Ui).name);
            Assert.AreEqual("Announcer", library.GroupFor(AudioBus.Announcer).name);
            foreach (string parameter in new[] { AudioLibrary.MasterParameter, AudioLibrary.MusicParameter, AudioLibrary.SfxParameter })
                Assert.IsTrue(library.Mixer.GetFloat(parameter, out _), parameter + " is not exposed on the mixer");
        }

        [Test]
        public void EveryAmmoTypeHasAFireSoundAndEveryUiEventHasASound()
        {
            foreach (string name in new[] { "Basic", "Shotgun", "Laser", "Gatling" })
            {
                var ammo = AssetDatabase.LoadAssetAtPath<AmmoTypeData>("Assets/Data/Ammo/Ammo_" + name + ".asset");
                Assert.IsNotNull(ammo.FireSound, name + " has no fire sound");
            }
            AudioLibrary library = Library();
            foreach (UiSoundKind kind in Enum.GetValues(typeof(UiSoundKind)))
                Assert.IsNotNull(library.Get(UiSound.SfxFor(kind)), kind + " has no library sound");
        }

        [Test]
        public void StingersDuckTheMusicAndAreTheLoudestPriority()
        {
            AudioLibrary library = Library();
            foreach (SfxId id in new[] { SfxId.StingerRound, SfxId.StingerBoss, SfxId.StingerClear, SfxId.StingerGameOver })
            {
                SfxData data = library.Get(id);
                Assert.Less(data.DuckMusicTo, 1f, id + " should duck the music");
                Assert.AreEqual(AudioBus.Announcer, data.Bus);
                Assert.AreEqual(3, data.Priority);
            }
        }
    }
}
