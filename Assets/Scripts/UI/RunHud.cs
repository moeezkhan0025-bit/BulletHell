using System.Text;
using BulletHell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>Small round and currency readout. Health, heat and ammo live in the CombatHud. Rebuilds its text only when a shown value changes.</summary>
    public sealed class RunHud : MonoBehaviour
    {
        [SerializeField] private Text label;

        private readonly StringBuilder builder = new StringBuilder(48);
        private int shownRound = -1, shownCurrency = -1, shownEarned = -1;

        private void Update()
        {
            RunManager run = GameServices.Ensure().Run;
            RunState state = run.State;
            if (state == null)
                return;

            int earned = run.RoundEarnings;
            if (state.Round == shownRound && state.Currency == shownCurrency && earned == shownEarned)
                return;

            shownRound = state.Round;
            shownCurrency = state.Currency;
            shownEarned = earned;
            builder.Clear();
            builder.Append("ROUND ").Append(shownRound).Append("   CURRENCY ").Append(shownCurrency);
            if (earned > 0)
                builder.Append(" (+").Append(earned).Append(")");
            label.text = builder.ToString();
        }
    }
}
