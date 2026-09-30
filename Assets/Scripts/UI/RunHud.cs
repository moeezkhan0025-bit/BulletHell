using System.Text;
using TMPro;
using BulletHell.Core;
using BulletHell.Enemies;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// The combat HUD's top readouts: the round / wave pill (top center) and the currency pill (top right). Health, heat and
    /// ammo live in the CombatHud. Text is rebuilt only when a shown value changes.
    /// </summary>
    public sealed class RunHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private TMP_Text currencyLabel;
        [Tooltip("Gives the current wave number.")]
        [SerializeField] private WaveSpawner spawner;

        private readonly StringBuilder builder = new StringBuilder(32);
        private int shownRound = -1, shownCurrency = -1, shownEarned = -1, shownWave = -1, shownWaveCount = -1;

        private void Update()
        {
            RunManager run = GameServices.Ensure().Run;
            RunState state = run.State;
            if (state == null)
                return;

            if (state.Round != shownRound && roundLabel != null)
            {
                shownRound = state.Round;
                roundLabel.text = "ROUND " + shownRound;
            }

            int wave = spawner != null ? spawner.WaveNumber : 0;
            int count = spawner != null ? spawner.WaveCount : 0;
            if ((wave != shownWave || count != shownWaveCount) && waveLabel != null)
            {
                shownWave = wave;
                shownWaveCount = count;
                builder.Clear();
                builder.Append("Wave ");
                if (wave > 0)
                    builder.Append(wave);
                else
                    builder.Append('-');
                builder.Append(" / ").Append(count);
                waveLabel.text = builder.ToString();
            }

            int earned = run.RoundEarnings;
            if ((state.Currency != shownCurrency || earned != shownEarned) && currencyLabel != null)
            {
                shownCurrency = state.Currency;
                shownEarned = earned;
                builder.Clear();
                builder.Append(shownCurrency.ToString("N0"));
                if (earned > 0)
                    builder.Append(" <size=60%>+").Append(earned).Append("</size>");
                currencyLabel.text = builder.ToString();
            }
        }
    }
}
