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
        private int shownRound = -1, shownCurrency = -1, shownHealth = -1;

        private void Update()
        {
            RunState state = GameServices.Ensure().Run.State;
            if (state == null)
                return;

            int health = playerHealth != null ? Mathf.CeilToInt(playerHealth.Current) : 0;
            if (state.Round == shownRound && state.Currency == shownCurrency && health == shownHealth)
                return;

            shownRound = state.Round;
            shownCurrency = state.Currency;
            shownHealth = health;
            builder.Clear();
            builder.Append("ROUND ").Append(shownRound).Append("   CURRENCY ").Append(shownCurrency);
            if (playerHealth != null)
                builder.Append("   HP ").Append(shownHealth).Append('/').Append(Mathf.CeilToInt(playerHealth.Max));
            label.text = builder.ToString();
        }
    }
}
