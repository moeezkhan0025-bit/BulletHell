namespace BulletHell.Core
{
    /// <summary>
    /// Audio stub. Gameplay and UI call these already; the real implementation arrives with the audio pass (M12).
    /// The music and SFX volumes come from the settings and are stored here for that pass to use.
    /// </summary>
    public sealed class AudioService
    {
        public float MusicVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;

        public void SetVolumes(float music, float sfx)
        {
            MusicVolume = music;
            SfxVolume = sfx;
        }

        public void PlaySfx(string id) { }

        public void PlayMusic(string id) { }
    }
}
