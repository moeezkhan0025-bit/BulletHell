using System;

namespace BulletHell.Weapons
{
    public enum ArmamentRarity { Common, Rare, Epic, Legendary }

    /// <summary>What an armament does, for filtering in the shop/armory and for the generated descriptions.</summary>
    [Flags]
    public enum ArmamentTags
    {
        None = 0,
        Speed = 1 << 0,
        Homing = 1 << 1,
        Pierce = 1 << 2,
        Bounce = 1 << 3,
        Auto = 1 << 4,
        Damage = 1 << 5,
        FireRate = 1 << 6,
        Status = 1 << 7,
    }
}
