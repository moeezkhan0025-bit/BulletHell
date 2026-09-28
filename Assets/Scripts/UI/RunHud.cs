using System.Text;
using BulletHell.Core;
using BulletHell.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>Minimal round, currency and health readout. Rebuilds its text only when a shown value changes.</summary>
    public sealed class RunHud : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private PlayerHealth playerHealth;

        private readonly StringBuilder builder = new StringBuilder(64);
        private int shownRound = -1, shownCurrency = -1, shownHealth = -1, shownEarned = -1;

        private void Update()
        {
            RunManager run = GameServices.Ensure().Run;
            RunState state = run.State;
            if (state == null)
                return;

            int health = playerHealth != null ? Mathf.CeilToInt(playerHealth.Current) : 0;
            int earned = run.RoundEarnings;
            if (state.Round == shownRound && state.Currency == shownCurrency && health == shownHealth && earned == shownEarned)
                return;

            shownRound = state.Round;
            shownCurrency = state.Currency;
            shownHealth = health;
            shownEarned = earned;
            builder.Clear();
            builder.Append("ROUND ").Append(shownRound).Append("   CURRENCY ").Append(shownCurrency);
            if (earned > 0)
                builder.Append(" (+").Append(earned).Append(")");
            if (playerHealth != null)
                builder.Append("   HP ").Append(shownHealth).Append('/').Append(Mathf.CeilToInt(playerHealth.Max));
            label.text = builder.ToString();
        }
    }
}
