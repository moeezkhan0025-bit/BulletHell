namespace BulletHell.Core
{
    /// <summary>Anything a projectile can hurt: enemies now, bosses and the player later.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(float amount);
    }
}
