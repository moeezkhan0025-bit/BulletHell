using BulletHell.Bosses;

namespace BulletHell.AI
{
    /// <summary>Weighted choice among a phase's attacks that never repeats the previous kind when another kind is available. Pure, so it is testable.</summary>
    public static class BossAttackPicker
    {
        /// <summary>
        /// The index of the attack picked for a roll in 0..1, or -1 when nothing is pickable (no weight, no pattern).
        /// `lastKind` is the previous attack's kind as an int, or -1 for none.
        /// </summary>
        public static int Pick(BossAttack[] attacks, int lastKind, float roll01)
        {
            if (attacks == null)
                return -1;

            bool otherKind = false;
            for (int i = 0; i < attacks.Length; i++)
                if (Pickable(attacks[i]) && (int)attacks[i].Kind != lastKind)
                    otherKind = true;

            float total = 0f;
            for (int i = 0; i < attacks.Length; i++)
                if (Allowed(attacks[i], lastKind, otherKind))
                    total += attacks[i].Weight;
            if (total <= 0f)
                return -1;

            float roll = roll01 * total;
            int last = -1;
            for (int i = 0; i < attacks.Length; i++)
            {
                if (!Allowed(attacks[i], lastKind, otherKind))
                    continue;
                last = i;
                roll -= attacks[i].Weight;
                if (roll <= 0f)
                    return i;
            }
            return last;   // roll01 == 1 lands on the final allowed entry
        }

        private static bool Pickable(BossAttack attack) => attack != null && attack.Weight > 0f && attack.Pattern != null;

        private static bool Allowed(BossAttack attack, int lastKind, bool otherKind) =>
            Pickable(attack) && (!otherKind || (int)attack.Kind != lastKind);
    }
}
