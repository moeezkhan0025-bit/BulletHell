using System.Collections.Generic;
using System.Text;

namespace BulletHell.Weapons
{
    /// <summary>Short one-line descriptions of arms, armaments and stats for the skeleton Shop/Armory UI.</summary>
    public static class ItemDescriber
    {
        public static string Armament(ArmamentData armament)
        {
            if (armament == null)
                return "(empty)";
            return armament.DisplayName + EffectSuffix(armament.Effects);
        }

        public static string Arm(WeaponArmData arm)
        {
            if (arm == null)
                return "(empty)";
            return arm.DisplayName + EffectSuffix(arm.Effects) + "  (dmg " + Number(arm.Damage) +
                   ", rate " + Number(arm.FireRate) + ", proj " + arm.ProjectilesPerShot + ")";
        }

        /// <summary>An arm instance with the armaments it carries: "Name [a | b | -]".</summary>
        public static string ArmWithArmaments(ArmInstance arm)
        {
            var builder = new StringBuilder(arm.Data.DisplayName);
            builder.Append(EffectSuffix(arm.Data.Effects)).Append("  [");
            for (int i = 0; i < ArmInstance.ArmamentSlots; i++)
            {
                if (i > 0)
                    builder.Append(" | ");
                ArmamentData armament = arm.GetArmament(i);
                builder.Append(armament != null ? armament.DisplayName : "-");
            }
            return builder.Append(']').ToString();
        }

        public static string Stats(in ArmStats stats) =>
            "dmg " + Number(stats.Damage) + "  rate " + Number(stats.FireRate) + "  speed " + Number(stats.ProjectileSpeed) +
            "  proj " + stats.ProjectilesPerShot + "  spread " + Number(stats.Spread);

        private static string EffectSuffix(IReadOnlyList<ArmEffect> effects)
        {
            StringBuilder builder = null;
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] == null)
                    continue;
                builder = builder == null ? new StringBuilder(" <").Append(effects[i].DisplayName)
                                          : builder.Append(", ").Append(effects[i].DisplayName);
            }
            return builder == null ? "" : builder.Append('>').ToString();
        }

        private static string Number(float value) => value.ToString("0.##");
    }
}
