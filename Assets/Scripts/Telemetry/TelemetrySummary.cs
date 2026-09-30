using System;
using System.Collections.Generic;

namespace BulletHell.Telemetry
{
    /// <summary>Count, mean, median and range of one number across runs.</summary>
    public readonly struct Stat
    {
        public readonly int Count;
        public readonly float Mean;
        public readonly float Median;
        public readonly float Min;
        public readonly float Max;

        public Stat(List<float> values)
        {
            Count = values.Count;
            if (Count == 0)
            {
                Mean = Median = Min = Max = 0f;
                return;
            }
            var sorted = new List<float>(values);
            sorted.Sort();
            float sum = 0f;
            foreach (float v in sorted)
                sum += v;
            Mean = sum / Count;
            Median = Count % 2 == 1 ? sorted[Count / 2] : (sorted[Count / 2 - 1] + sorted[Count / 2]) * 0.5f;
            Min = sorted[0];
            Max = sorted[Count - 1];
        }
    }

    /// <summary>Average time and damage of one round across the runs that played it.</summary>
    public readonly struct RoundAverage
    {
        public readonly int Round;
        public readonly int Runs;
        public readonly float Seconds;
        public readonly float Damage;

        public RoundAverage(int round, int runs, float seconds, float damage)
        {
            Round = round;
            Runs = runs;
            Seconds = seconds;
            Damage = damage;
        }
    }

    /// <summary>
    /// Averages and distributions over the logged runs, for the debug summary screen. Pure maths on <see cref="RunRecord"/>s so it
    /// can be tested without a scene. Runs that used a debug tool are left out unless asked for.
    /// </summary>
    public sealed class TelemetrySummary
    {
        public int Logged;
        public int Counted;
        public int Died;
        public int Quit;
        public int DebugExcluded;

        public Stat RoundReached;
        public Stat CombatSeconds;
        public Stat DamageTaken;
        public Stat CurrencyEarned;
        public Stat CurrencySpent;
        public Stat ItemsBought;
        public Stat ArmamentsEquipped;

        /// <summary>How many runs ended in each round (round number to runs).</summary>
        public SortedDictionary<int, int> RoundHistogram = new SortedDictionary<int, int>();
        /// <summary>What killed the player, most common first (runs that died only).</summary>
        public List<KeyValuePair<string, int>> Causes = new List<KeyValuePair<string, int>>();
        /// <summary>Runs per highest boss phase seen: index 0 = never met a boss.</summary>
        public int[] BossPhases = new int[4];
        public int BossesDefeated;
        public List<RoundAverage> PerRound = new List<RoundAverage>();

        public static TelemetrySummary Compute(IReadOnlyList<RunRecord> records, bool includeDebug = false)
        {
            var s = new TelemetrySummary { Logged = records.Count };
            var reached = new List<float>();
            var seconds = new List<float>();
            var damage = new List<float>();
            var earned = new List<float>();
            var spent = new List<float>();
            var items = new List<float>();
            var armaments = new List<float>();
            var causes = new Dictionary<string, int>();
            var roundSeconds = new List<float>();
            var roundDamage = new List<float>();
            var roundRuns = new List<int>();

            foreach (RunRecord r in records)
            {
                if (r.DebugUsed && !includeDebug)
                {
                    s.DebugExcluded++;
                    continue;
                }
                s.Counted++;
                if (r.Died)
                {
                    s.Died++;
                    string cause = string.IsNullOrEmpty(r.CauseOfDeath) ? "unknown" : r.CauseOfDeath;
                    causes[cause] = causes.TryGetValue(cause, out int n) ? n + 1 : 1;
                }
                else
                {
                    s.Quit++;
                }

                reached.Add(r.RoundReached);
                seconds.Add(r.CombatSeconds);
                damage.Add(r.DamageTotal);
                earned.Add(r.CurrencyEarned);
                spent.Add(r.CurrencySpent);
                items.Add(r.ItemsBought);
                armaments.Add(r.ArmamentsEquipped);
                s.RoundHistogram[r.RoundReached] = s.RoundHistogram.TryGetValue(r.RoundReached, out int h) ? h + 1 : 1;
                s.BossPhases[Math.Clamp(r.BossPhaseMax, 0, s.BossPhases.Length - 1)]++;
                if (r.BossDefeated)
                    s.BossesDefeated++;

                int rounds = Math.Max(r.RoundSeconds.Count, r.RoundDamage.Count);
                for (int i = 0; i < rounds; i++)
                {
                    float sec = i < r.RoundSeconds.Count ? r.RoundSeconds[i] : 0f;
                    float dmg = i < r.RoundDamage.Count ? r.RoundDamage[i] : 0f;
                    if (sec <= 0f && dmg <= 0f)
                        continue;   // a round the run never played (skipped with a debug tool)
                    while (roundSeconds.Count <= i)
                    {
                        roundSeconds.Add(0f);
                        roundDamage.Add(0f);
                        roundRuns.Add(0);
                    }
                    roundSeconds[i] += sec;
                    roundDamage[i] += dmg;
                    roundRuns[i]++;
                }
            }

            s.RoundReached = new Stat(reached);
            s.CombatSeconds = new Stat(seconds);
            s.DamageTaken = new Stat(damage);
            s.CurrencyEarned = new Stat(earned);
            s.CurrencySpent = new Stat(spent);
            s.ItemsBought = new Stat(items);
            s.ArmamentsEquipped = new Stat(armaments);

            foreach (var pair in causes)
                s.Causes.Add(pair);
            s.Causes.Sort((a, b) => b.Value != a.Value ? b.Value.CompareTo(a.Value) : string.CompareOrdinal(a.Key, b.Key));

            for (int i = 0; i < roundRuns.Count; i++)
                if (roundRuns[i] > 0)
                    s.PerRound.Add(new RoundAverage(i + 1, roundRuns[i], roundSeconds[i] / roundRuns[i], roundDamage[i] / roundRuns[i]));
            return s;
        }
    }
}
