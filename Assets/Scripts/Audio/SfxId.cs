namespace BulletHell.Audio
{
    /// <summary>Every sound the game asks for by name. The AudioLibrary maps each to an <see cref="SfxData"/> (clips, volume, limits). Append only.</summary>
    public enum SfxId
    {
        None,
        EnemyHit, EnemyDeath, PlayerHit, Jump, Land, PickupAmmo, PickupCoin,
        UiFocus, UiConfirm, UiBack, UiBuy, UiEquip, UiError,
        StingerRound, StingerBoss, StingerClear, StingerGameOver, Countdown, CountdownGo,
    }

    /// <summary>The music that plays in each part of the game. Crossfaded by the AudioService.</summary>
    public enum MusicContext { None, Menu, Combat, Boss, Shop }

    /// <summary>Which mixer group a sound plays through (all three sit under the SFX volume slider; Music has its own).</summary>
    public enum AudioBus { Sfx, Ui, Announcer }
}
