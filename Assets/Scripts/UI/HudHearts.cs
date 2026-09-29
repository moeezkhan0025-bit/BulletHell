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
            shownHealth = health;
            shownMax = total;

            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i].gameObject.SetActive(i < total);
                hearts[i].sprite = i < health ? fullHeart : emptyHeart;
            }
        }
    }
}
