using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>One heart per hit point (1 heart = 1 HP). Only redraws when the health changes.</summary>
    public sealed class HudHearts : MonoBehaviour
    {
        [SerializeField] private Image[] hearts = new Image[0];
        [SerializeField] private Sprite fullHeart;
        [SerializeField] private Sprite emptyHeart;

        private int shownHealth = -1;
        private int shownMax = -1;

        public void Refresh(float current, float max)
        {
            int health = Mathf.Max(0, Mathf.CeilToInt(current));
            int total = Mathf.Max(1, Mathf.CeilToInt(max));
            if (health == shownHealth && total == shownMax)
                return;
            int previous = shownHealth;
            shownHealth = health;
            shownMax = total;

            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i].gameObject.SetActive(i < total);
                UITheme theme = UITheme.Current;
                Sprite full = theme != null && theme.HeartFull != null ? theme.HeartFull : fullHeart;
                Sprite empty = theme != null && theme.HeartEmpty != null ? theme.HeartEmpty : emptyHeart;
                hearts[i].sprite = i < health ? full : empty;
            }

            // Pop the hearts that just changed (lost or regained); the first draw is silent.
            if (previous >= 0)
            {
                int from = Mathf.Min(previous, health);
                int to = Mathf.Max(previous, health);
                for (int i = from; i < to && i < hearts.Length; i++)
                {
                    Tween.StopAll(hearts[i].transform);
                    hearts[i].transform.localScale = Vector3.one;
                    Tween.PunchScale(hearts[i].transform, Vector3.one * 0.5f, 0.35f, frequency: 6, useUnscaledTime: true);
                }
            }
        }
    }
}
