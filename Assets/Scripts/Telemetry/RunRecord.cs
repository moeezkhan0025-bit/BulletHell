using System.Collections.Generic;

namespace BulletHell.Telemetry
{
    /// <summary>How a logged run ended.</summary>
    public enum RunEndReason { Died, Quit }

    /// <summary>
    /// One finished run, as written to a row of the playtest CSV. Plain data: the collector fills it while the run is played and
    /// the summary reads it back from the file. Per-round lists are indexed by round number - 1.
    /// </summary>
    public sealed class RunRecord
    {
        public string RunId = "";
        public string StartedUtc = "";
        public string EndedUtc = "";
        /// <summary>"editor", "dev" or "release".</summary>
        public string Build = "";
        public string Version = "";
        /// <summary>The run was resumed with Continue (an earlier part of it may have been logged as a quit).</summary>
        public bool Continued;
        /// <summary>A debug tool touched the run (round skip, currency grant, god mode): the summary leaves it out by default.</summary>
        public bool DebugUsed;
        public string Result = "";
        public string CauseOfDeath = "";
        public int RoundReached;
        public int RoundsCleared;
        /// <summary>Seconds of combat per round (pause, shop and menus excluded).</summary>
        public List<float> RoundSeconds = new List<float>();
        /// <summary>Health lost per round.</summary>
        public List<float> RoundDamage = new List<float>();
        public int CurrencyEarned;
        public int CurrencySpent;
        public int CurrencyEnd;
        public int ArmsBought;
        public int ArmamentsBought;
        public int Rerolls;
        public int CratesOpened;
        /// <summary>Names of everything bought, in order.</summary>
        public List<string> Items = new List<string>();
        /// <summary>Armaments sitting on the arms when the run ended.</summary>
        public int ArmamentsEquipped;
        /// <summary>"ArmName[Armament+Armament]|ArmName[...]" for the filled slots.</summary>
        public string Loadout = "";
        public string BossReached = "";
        /// <summary>Highest boss phase seen (0 = no boss, 1 = first phase, 2 = second...).</summary>
        public int BossPhaseMax;
        public bool BossDefeated;

        public int ItemsBought => ArmsBought + ArmamentsBought;

        public float CombatSeconds
        {
            get
            {
                float sum = 0f;
                foreach (float s in RoundSeconds)
                    sum += s;
                return sum;
            }
        }

        public float DamageTotal
        {
            get
            {
                float sum = 0f;
                foreach (float d in RoundDamage)
                    sum += d;
                return sum;
            }
        }

        public bool Died => Result == "died";

        /// <summary>Adds to a per-round list, growing it with zeros so round n lands at index n - 1.</summary>
        public static void Add(List<float> list, int round, float amount)
        {
            int index = System.Math.Max(0, round - 1);
            while (list.Count <= index)
                list.Add(0f);
            list[index] += amount;
        }
    }
}
