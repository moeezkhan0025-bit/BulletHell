using System;
using BulletHell.Core;
using BulletHell.Enemies;

namespace BulletHell.Audio
{
    /// <summary>
    /// Decides which music plays and when the stingers fire, from the run state alone (so no scene has to be wired): combat music
    /// (a different track per round) or the boss track from the round intro, the Shop track from Round Results through the Armory,
    /// quieter music while paused, silence and a sting at Game Over. The Main Menu asks for its own track (MainMenuController).
    /// </summary>
    public sealed class AudioDirector : IDisposable
    {
        private readonly AudioService audio;
        private readonly RunManager run;
        private readonly GameConfig config;

        public AudioDirector(AudioService audioService, RunManager runManager, GameConfig gameConfig)
        {
            audio = audioService;
            run = runManager;
            config = gameConfig;
            run.RoundIntroStarted += OnRoundIntro;
            run.Machine.StateChanged += OnStateChanged;
        }

        public void Dispose()
        {
            run.RoundIntroStarted -= OnRoundIntro;
            run.Machine.StateChanged -= OnStateChanged;
        }

        /// <summary>The context and variant the round intro starts: the boss track for boss rounds, else combat (track by round).</summary>
        public static MusicContext ContextForRound(bool bossRound) => bossRound ? MusicContext.Boss : MusicContext.Combat;

        private void OnRoundIntro(int round)
        {
            RoundData data = config.GetRound(round);
            bool boss = data != null && data.IsBossRound;
            audio.PlayMusic(ContextForRound(boss), round - 1);
            audio.Play(boss ? SfxId.StingerBoss : SfxId.StingerRound);
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            audio.SetPaused(to == GameState.Pause);
            switch (to)
            {
                case GameState.RoundResults:
                    audio.Play(SfxId.StingerClear);
                    audio.PlayMusic(MusicContext.Shop);
                    break;
                case GameState.Shop:
                case GameState.Armory:
                    audio.PlayMusic(MusicContext.Shop);
                    break;
                case GameState.GameOver:
                    audio.StopMusic();
                    audio.Play(SfxId.StingerGameOver);
                    break;
            }
        }
    }
}
