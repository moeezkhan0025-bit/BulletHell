using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BulletHell.Telemetry
{
    /// <summary>
    /// The playtest CSV: one header line, then one row per finished run. Lists inside a cell (per-round numbers, item names) are
    /// joined with ";" so a cell never needs more than standard CSV quoting. Reading goes by header name, so a column added later
    /// does not break older files (missing columns read as empty).
    /// </summary>
    public static class TelemetryCsv
    {
        public static readonly string[] Columns =
        {
            "run_id", "started_utc", "ended_utc", "build", "version", "continued", "debug_used", "result", "cause_of_death",
            "round_reached", "rounds_cleared", "combat_seconds", "round_seconds", "damage_total", "round_damage",
            "currency_earned", "currency_spent", "currency_end", "items_bought", "arms_bought", "armaments_bought", "rerolls",
            "crates_opened", "items", "armaments_equipped", "loadout", "boss_reached", "boss_phase_max", "boss_defeated",
        };

        public static string Header => string.Join(",", Columns);

        private static string F(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);
        private static string B(bool value) => value ? "1" : "0";

        public static string ToLine(RunRecord r)
        {
            var cells = new[]
            {
                r.RunId, r.StartedUtc, r.EndedUtc, r.Build, r.Version, B(r.Continued), B(r.DebugUsed), r.Result, r.CauseOfDeath,
                r.RoundReached.ToString(CultureInfo.InvariantCulture), r.RoundsCleared.ToString(CultureInfo.InvariantCulture),
                F(r.CombatSeconds), JoinFloats(r.RoundSeconds), F(r.DamageTotal), JoinFloats(r.RoundDamage),
                r.CurrencyEarned.ToString(CultureInfo.InvariantCulture), r.CurrencySpent.ToString(CultureInfo.InvariantCulture),
                r.CurrencyEnd.ToString(CultureInfo.InvariantCulture), r.ItemsBought.ToString(CultureInfo.InvariantCulture),
                r.ArmsBought.ToString(CultureInfo.InvariantCulture), r.ArmamentsBought.ToString(CultureInfo.InvariantCulture),
                r.Rerolls.ToString(CultureInfo.InvariantCulture), r.CratesOpened.ToString(CultureInfo.InvariantCulture),
                string.Join(";", r.Items), r.ArmamentsEquipped.ToString(CultureInfo.InvariantCulture), r.Loadout,
                r.BossReached, r.BossPhaseMax.ToString(CultureInfo.InvariantCulture), B(r.BossDefeated),
            };
            var builder = new StringBuilder(256);
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0)
                    builder.Append(',');
                builder.Append(Escape(cells[i]));
            }
            return builder.ToString();
        }

        private static string JoinFloats(List<float> values)
        {
            var parts = new string[values.Count];
            for (int i = 0; i < parts.Length; i++)
                parts[i] = F(values[i]);
            return string.Join(";", parts);
        }

        /// <summary>Standard CSV quoting: a cell with a comma, quote or line break is wrapped in quotes and its quotes doubled.</summary>
        public static string Escape(string cell)
        {
            if (string.IsNullOrEmpty(cell))
                return "";
            if (cell.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
                return cell;
            return "\"" + cell.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>Splits CSV text into rows of cells (quoted cells may contain commas, doubled quotes and line breaks).</summary>
        public static List<List<string>> Split(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }
                    continue;
                }
                if (c == '"')
                    quoted = true;
                else if (c == ',')
                {
                    row.Add(cell.ToString());
                    cell.Clear();
                }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;
                    row.Add(cell.ToString());
                    cell.Clear();
                    if (row.Count > 1 || row[0].Length > 0)
                        rows.Add(row);
                    row = new List<string>();
                }
                else
                {
                    cell.Append(c);
                }
            }
            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row);
            }
            return rows;
        }

        /// <summary>Reads every run of a CSV text. Rows that cannot be read are skipped.</summary>
        public static List<RunRecord> Parse(string text)
        {
            var records = new List<RunRecord>();
            if (string.IsNullOrWhiteSpace(text))
                return records;
            List<List<string>> rows = Split(text);
            if (rows.Count < 2)
                return records;

            var index = new Dictionary<string, int>();
            for (int i = 0; i < rows[0].Count; i++)
                index[rows[0][i].Trim()] = i;

            for (int r = 1; r < rows.Count; r++)
            {
                List<string> row = rows[r];
                string Get(string name) => index.TryGetValue(name, out int i) && i < row.Count ? row[i] : "";
                int Int(string name) => int.TryParse(Get(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
                bool Bool(string name) => Get(name) == "1";

                var record = new RunRecord
                {
                    RunId = Get("run_id"), StartedUtc = Get("started_utc"), EndedUtc = Get("ended_utc"), Build = Get("build"),
                    Version = Get("version"), Continued = Bool("continued"), DebugUsed = Bool("debug_used"), Result = Get("result"),
                    CauseOfDeath = Get("cause_of_death"), RoundReached = Int("round_reached"), RoundsCleared = Int("rounds_cleared"),
                    CurrencyEarned = Int("currency_earned"), CurrencySpent = Int("currency_spent"), CurrencyEnd = Int("currency_end"),
                    ArmsBought = Int("arms_bought"), ArmamentsBought = Int("armaments_bought"), Rerolls = Int("rerolls"),
                    CratesOpened = Int("crates_opened"), ArmamentsEquipped = Int("armaments_equipped"), Loadout = Get("loadout"),
                    BossReached = Get("boss_reached"), BossPhaseMax = Int("boss_phase_max"), BossDefeated = Bool("boss_defeated"),
                };
                ParseFloats(Get("round_seconds"), record.RoundSeconds);
                ParseFloats(Get("round_damage"), record.RoundDamage);
                string items = Get("items");
                if (items.Length > 0)
                    record.Items.AddRange(items.Split(';'));
                if (record.RunId.Length == 0 && record.Result.Length == 0)
                    continue;
                records.Add(record);
            }
            return records;
        }

        private static void ParseFloats(string cell, List<float> into)
        {
            if (string.IsNullOrEmpty(cell))
                return;
            foreach (string part in cell.Split(';'))
                into.Add(float.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f);
        }
    }
}
