namespace BulletHell.Enemies
{
    /// <summary>Where the bullets of one volley go. Plain math (no Unity objects) so it can be unit tested and never allocates.</summary>
    public static class AttackPatternMath
    {
        /// <summary>
        /// Fills angles (degrees, 0 = +X, counter-clockwise) for one volley and returns how many were written.
        /// aimAngle is the angle from the enemy to the player; spiralOffset is the current turn of a Spiral.
        /// </summary>
        public static int FillAngles(AttackShape shape, int count, float spreadAngle, float aimAngle,
                                     float spiralOffset, float[] angles)
        {
            int n = System.Math.Min(System.Math.Max(1, count), angles.Length);
            switch (shape)
            {
                case AttackShape.Aimed:
                    angles[0] = aimAngle;
                    return 1;

                case AttackShape.Spread:
                    if (n == 1)
                    {
                        angles[0] = aimAngle;
                        return 1;
                    }
                    for (int i = 0; i < n; i++)
                        angles[i] = aimAngle - spreadAngle * 0.5f + spreadAngle * i / (n - 1);
                    return n;

                case AttackShape.Ring:
                    for (int i = 0; i < n; i++)
                        angles[i] = 360f * i / n;
                    return n;

                default: // Spiral
                    for (int i = 0; i < n; i++)
                        angles[i] = spiralOffset + 360f * i / n;
                    return n;
            }
        }
    }
}
