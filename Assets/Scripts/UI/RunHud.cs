using System.Text;
using BulletHell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>Minimal round and currency readout. Rebuilds its text only when a shown value changes.</summary>
    public sealed class RunHud : MonoBehaviour
    {
        [SerializeField] private Text label;

        private readonly StringBuilder builder = new StringBuilder(48);
        private int shownRound = -1, shownCurrency = -1;

        private void Update()
        {
            RunState state = GameServices.Ensure().Run.State;
            if (state == null || (state.Round == shownRound && state.Currency == shownCurrency))
                return;

            shownRound = state.Round;
            shownCurrency = state.Currency;
            builder.Clear();
            builder.Append("ROUND ").Append(shownRound).Append("   CURRENCY ").Append(shownCurrency);
            label.text = builder.ToString();
        }
    }
}
