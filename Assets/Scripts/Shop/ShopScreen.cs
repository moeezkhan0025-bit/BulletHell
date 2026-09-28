using System;
using System.Text;
using BulletHell.Core;
using BulletHell.UI;
using BulletHell.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.Shop
{
    /// <summary>
    /// Skeleton Shop: a list of the pool's items with their prices; selecting one buys it into the arm or armament
    /// inventory. Unaffordable items stay selectable (so controller focus never gets stuck) but are dimmed and refuse.
    /// Nothing is saved here: the run is saved when entering the Shop and when leaving the Armory.
    /// </summary>
    public sealed class ShopScreen : MonoBehaviour
    {
        [SerializeField] private Text title;
        [SerializeField] private Text info;
        [SerializeField] private UIList list;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button menuButton;

        private readonly StringBuilder builder = new StringBuilder(160);
        private RunState state;
        private ShopPool pool;
        private string message = "";

        public event Action ContinuePressed;
        public event Action MenuPressed;

        private void Awake()
        {
            continueButton.onClick.AddListener(() => ContinuePressed?.Invoke());
            menuButton.onClick.AddListener(() => MenuPressed?.Invoke());
        }

        public void Show(RunState runState, ShopPool shopPool)
        {
            state = runState;
            pool = shopPool;
            message = "";
            gameObject.SetActive(true);
            Refresh(0);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Refresh(int focus)
        {
            title.text = $"Shop - round {state.Round}";

            builder.Clear();
            builder.Append("Currency: ").Append(state.Currency)
                   .Append("     Armaments owned: ").Append(state.Armaments.Count)
                   .Append("     Spare arms: ").Append(state.SpareArms.Count);
            if (message.Length > 0)
                builder.Append('\n').Append(message);
            info.text = builder.ToString();

            list.Begin();
            for (int i = 0; pool != null && i < pool.Count; i++)
            {
                int index = i;
                ShopEntry entry = pool.Get(i);
                string kind = entry.Kind == ShopItemKind.Arm ? "ARM" : "ARMAMENT";
                string description = !entry.IsValid ? "?" :
                    entry.Kind == ShopItemKind.Arm ? ItemDescriber.Arm(entry.Arm) : ItemDescriber.Armament(entry.Armament);
                list.Add($"{kind}   {description}   -   {entry.Price}", () => Buy(index), null, state.Currency < entry.Price);
            }
            list.End();

            if (list.Count > 0)
                list.Focus(focus);
            else
                UIFocusGuard.Focus(continueButton.gameObject);
        }

        private void Buy(int index)
        {
            ShopEntry entry = pool.Get(index);
            switch (ShopService.TryBuy(state, entry))
            {
                case PurchaseResult.Bought: message = $"Bought {entry.Name}."; break;
                case PurchaseResult.NotEnoughCurrency: message = $"Not enough currency for {entry.Name}."; break;
                default: message = "That item is not available."; break;
            }
            Refresh(index);
        }
    }
}
