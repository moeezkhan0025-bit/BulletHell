namespace BulletHell.UI
{
    /// <summary>
    /// What a prompt tells the player to press, by meaning rather than by button (the glyph library maps it per device).
    /// The values after Start are the gameplay controls the round 1 onboarding names. Append only: the glyph asset stores
    /// its labels in enum order.
    /// </summary>
    public enum UiAction
    {
        Confirm, Back, Randomize, Details, Reroll, Remove, TabPrev, TabNext, Start,
        Move, Aim, Fire, Lock, Jump, Ammo1, Ammo2, Ammo3, Ammo4, SkipTutorial,
    }
}
