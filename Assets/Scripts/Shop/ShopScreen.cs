using System;
using TMPro;
using System.Collections.Generic;
using BulletHell.Core;
using BulletHell.Input;
using BulletHell.Platform;
using BulletHell.UI;
using BulletHell.Weapons;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.Shop
{
    /// <summary>
    /// The Shop: a merchant and a stall with cards (2 arms on top, 3 armaments below, a crate that opens a pick-1-of-3
    /// armament choice), currency top-right, Reroll and Leave. Stock is random (weighted by the RarityTable and the round)
    /// and comes from the run's ShopVisit, so it does not change between visits to the screen or after Continue. Every buy,
    /// reroll and crate pick saves the run. Controller: stick/D-pad move focus, Cross buys, Triangle rerolls, Square toggles
    /// detailed stats, Circle leaves; the mouse hovers to focus and clicks to buy.
    /// </summary>
    public sealed class ShopScreen : MonoBehaviour
    {
        [Header("Cards")]
        [SerializeField] private ShopCard[] armCards = new ShopCard[0];
        [SerializeField] private ShopCard[] armamentCards = new ShopCard[0];
        [SerializeField] private ShopCard crateCard;
        [Tooltip("Icon shown for items that have no icon art yet (tinted by rarity).")]
        [SerializeField] private Sprite placeholderIcon;

        [Header("Labels and buttons")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text currencyLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private TMP_Text inventoryLabel;
        [SerializeField] private Button rerollButton;
        [SerializeField] private TMP_Text rerollLabel;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button menuButton;
        [Tooltip("Where a bought card flies to.")]
        [SerializeField] private RectTransform inventoryTarget;
        [SerializeField] private ShopTooltip tooltip;

        [Header("Crate choice")]
        [SerializeField] private GameObject crateOverlay;
        [SerializeField] private ShopCard[] crateChoiceCards = new ShopCard[0];

        [Header("Input")]
        [SerializeField] private MenuInputReader input;
        [SerializeField] private ButtonGlyphLibrary glyphs;

        private RunState state;
        private ShopVisit visit;
        private ShopStock stock;
        private ShopPool pool;
        private RarityTable table;
        private ShopTuning tuning;
        private RunManager run;
        private readonly List<ShopCard> cards = new List<ShopCard>();
        private readonly List<ArmamentData> crateOptions = new List<ArmamentData>();
        private bool detailed;
        private bool crateOpen;
        private bool built;
        private int shownCurrency = int.MinValue;
        private GameObject lastSelected;
        private ShopCard shownTooltipCard;
        private int shownRerollCost = -1;

        public event Action ContinuePressed;
        public event Action MenuPressed;

        public bool IsCrateOpen => crateOpen;

        private void Awake()
        {
            leaveButton.onClick.AddListener(Leave);
            menuButton.onClick.AddListener(() => MenuPressed?.Invoke());
            rerollButton.onClick.AddListener(Reroll);
        }

        private void EnsureBuilt()
        {
            if (built)
                return;
            built = true;

            cards.AddRange(armCards);
            cards.AddRange(armamentCards);
            cards.Add(crateCard);
            foreach (ShopCard card in cards)
                card.Clicked += OnCardClicked;
            foreach (ShopCard choice in crateChoiceCards)
                choice.Clicked += OnCrateChoiceClicked;

            // Circle leaves from anywhere on the screen (not while a crate choice is open).
            foreach (Selectable selectable in AllSelectables())
                if (selectable.TryGetComponent(out CancelRelay relay))
                    relay.Cancelled += OnCancel;
        }

        public void Show(RunState runState)
        {
            EnsureBuilt();
            GameServices services = GameServices.Ensure();
            run = services.Run;
            pool = services.Config.ShopPool;
            table = services.Config.RarityTable;
            tuning = services.Config.ShopTuning;

            state = runState;
            if (state.Shop == null)
                state.Shop = ShopVisit.Create(UnityEngine.Random.Range(1, int.MaxValue));
            visit = state.Shop;
            detailed = false;
            crateOpen = false;
            crateOverlay.SetActive(false);
            shownCurrency = int.MinValue;
            shownTooltipCard = null;
            tooltip.Hide();

            ScreenTransition.In(gameObject);
            titleLabel.text = "THE MERCATOR'S STALL";
            messageLabel.text = "";
            ApplyHints();
            BuildStock();
            BuildNavigation();

            if (input != null)
            {
                input.RerollPressed += Reroll;
                input.DetailsPressed += ToggleDetails;
            }
            UIFocusGuard.Focus(FirstFocus());
        }

        public void Hide()
        {
            if (input != null)
            {
                input.RerollPressed -= Reroll;
                input.DetailsPressed -= ToggleDetails;
            }
            tooltip.Hide();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (input != null)
            {
                input.RerollPressed -= Reroll;
                input.DetailsPressed -= ToggleDetails;
            }
        }

        // ---- stock

        private void BuildStock()
        {
            stock = ShopStock.Generate(visit.Seed, state.Round, visit.Rerolls, pool, table, tuning);
            for (int i = 0; i < armCards.Length; i++)
                SetCard(armCards[i], i < stock.Arms.Length ? i : -1);
            for (int i = 0; i < armamentCards.Length; i++)
                SetCard(armamentCards[i], i < stock.Armaments.Length ? stock.Arms.Length + i : -1);
            SetCard(crateCard, stock.CrateIndex);
            RefreshLabels();
        }

        // offer < 0: no item for this card (the pool has fewer candidates than the layout has cards): the card is hidden.
        private void SetCard(ShopCard card, int offer)
        {
            card.OfferIndex = offer;
            if (offer < 0)
            {
                card.gameObject.SetActive(false);
                return;
            }
            ShopEntry entry = stock.Get(offer);
            ArmamentRarity rarity = entry.Rarity(tuning);
            card.Set(entry, IconOf(entry), placeholderIcon, table.ColorOf(rarity), RarityName(entry, rarity),
                     visit.IsSold(offer), state.Currency >= entry.Price);
        }

        private static Sprite IconOf(in ShopEntry entry)
        {
            switch (entry.Kind)
            {
                case ShopItemKind.Arm: return entry.Arm.Sprite;
                case ShopItemKind.Armament: return entry.Armament.Icon;
                default: return UITheme.Current != null ? UITheme.Current.HarvestCrateIcon : null;
            }
        }

        private static string RarityName(in ShopEntry entry, ArmamentRarity rarity) =>
            entry.Kind == ShopItemKind.Crate ? "CRATE" : rarity.ToString().ToUpperInvariant();

        private void RefreshLabels()
        {
            shownCurrency = state.Currency;
            currencyLabel.text = state.Currency.ToString("N0");
            int rerollCost = ShopService.RerollCost(visit, tuning);
            rerollLabel.text = "Reroll \u00B7 " + rerollCost;
            if (rerollCost != shownRerollCost)
            {
                shownRerollCost = rerollCost;
                ApplyHints();
            }
            inventoryLabel.text = "Arms " + state.SpareArms.Count + "   Armaments " + state.Armaments.Count;
            foreach (ShopCard card in cards)
            {
                if (!card.gameObject.activeSelf || card.OfferIndex < 0)
                    continue;
                card.SetAffordable(state.Currency >= stock.Get(card.OfferIndex).Price);
            }
        }

        // ---- input

        private void Update()
        {
            if (state == null)
                return;
            if (state.Currency != shownCurrency)
                RefreshLabels();

            // The tooltip follows the focused card (controller focus or mouse hover).
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != lastSelected)
            {
                lastSelected = selected;
                shownTooltipCard = selected != null ? selected.GetComponent<ShopCard>() : null;
                UpdateTooltip();
            }
        }

        private void UpdateTooltip()
        {
            ShopCard card = shownTooltipCard;
            if (card == null || card.OfferIndex < 0 || !card.gameObject.activeInHierarchy)
            {
                tooltip.Hide();
                return;
            }

            if (crateOpen)
            {
                int index = Array.IndexOf(crateChoiceCards, card);
                if (index >= 0 && index < crateOptions.Count)
                    tooltip.Show((RectTransform)card.transform, ShopDescriber.ArmamentLines(crateOptions[index], state, detailed), true);
                else
                    tooltip.Hide();
                return;
            }

            if (Array.IndexOf(crateChoiceCards, card) >= 0 || card.OfferIndex >= stock.Count)
            {
                tooltip.Hide();
                return;
            }
            ShopEntry entry = stock.Get(card.OfferIndex);
            tooltip.Show((RectTransform)card.transform, ShopDescriber.Lines(entry, state, tuning, detailed));
        }

        private void ToggleDetails()
        {
            detailed = !detailed;
            UpdateTooltip();
        }

        private void OnCancel()
        {
            if (!crateOpen)
                Leave();
        }

        private void Leave()
        {
            if (!crateOpen)
                ContinuePressed?.Invoke();
        }

        // ---- buying

        private void OnCardClicked(ShopCard card)
        {
            if (crateOpen)
                return;
            int offer = card.OfferIndex;
            ShopEntry entry = stock.Get(offer);

            if (entry.Kind == ShopItemKind.Crate)
            {
                BuyCrate(card, offer, entry);
                return;
            }

            switch (ShopService.BuyOffer(state, visit, offer, entry))
            {
                case PurchaseResult.Bought:
                    Say("Bought " + entry.Name + ".");
                    card.PlayBuy(inventoryTarget);
                    RefreshLabels();
                    run.SaveRun();
                    UpdateTooltip();
                    break;
                case PurchaseResult.NotEnoughCurrency:
                    Say("Not enough currency for " + entry.Name + ".");
                    card.Shake();
                    break;
                case PurchaseResult.AlreadySold:
                    Say("Sold out.");
                    break;
                default:
                    Say("That item is not available.");
                    break;
            }
        }

        private void Reroll()
        {
            if (crateOpen || visit == null)
                return;
            if (ShopService.TryReroll(state, visit, tuning))
            {
                Say("New stock.");
                BuildStock();
                run.SaveRun();
                shownTooltipCard = null;
                lastSelected = null; // re-evaluate the focused card (its item changed)
            }
            else
            {
                Say("Not enough currency to reroll (" + ShopService.RerollCost(visit, tuning) + ").");
                UiSound.Play(UiSoundKind.Error);
                Tween.PunchLocalPosition(rerollButton.transform, new Vector3(12f, 0f, 0f), 0.3f, 10, useUnscaledTime: true);
            }
        }

        // ---- crate

        private void BuyCrate(ShopCard card, int offer, ShopEntry entry)
        {
            switch (ShopService.BuyCrate(state, visit, offer, entry))
            {
                case PurchaseResult.Bought:
                    crateOptions.Clear();
                    crateOptions.AddRange(ShopStock.CrateChoices(visit.Seed, state.Round, visit.Rerolls, pool, table, tuning));
                    OpenCrate();
                    break;
                case PurchaseResult.NotEnoughCurrency:
                    Say("Not enough currency for the crate.");
                    card.Shake();
                    break;
                default:
                    Say("Sold out.");
                    break;
            }
        }

        private void OpenCrate()
        {
            crateOpen = true;
            crateOverlay.SetActive(true);
            RefreshLabels();
            for (int i = 0; i < crateChoiceCards.Length; i++)
            {
                ShopCard choice = crateChoiceCards[i];
                choice.OfferIndex = 1000 + i;
                if (i >= crateOptions.Count)
                {
                    choice.gameObject.SetActive(false);
                    continue;
                }
                ArmamentData armament = crateOptions[i];
                var entry = new ShopEntry { Kind = ShopItemKind.Armament, Armament = armament, Price = 0 };
                choice.Set(entry, armament.Icon, placeholderIcon, table.ColorOf(armament.Rarity), armament.Rarity.ToString().ToUpperInvariant(), false, true);
            }
            Say("Pick 1 of " + crateOptions.Count + ".");
            UIFocusGuard.Focus(crateChoiceCards[0].gameObject);
            shownTooltipCard = null;
            lastSelected = null;
        }

        private void OnCrateChoiceClicked(ShopCard choice)
        {
            int index = Array.IndexOf(crateChoiceCards, choice);
            if (!crateOpen || index < 0 || index >= crateOptions.Count)
                return;

            ArmamentData chosen = crateOptions[index];
            int crateOffer = stock.CrateIndex;
            if (!ShopService.PickCrate(state, visit, crateOffer, chosen))
                return;

            crateOpen = false;
            crateOverlay.SetActive(false);
            crateCard.SetSold(true, true);
            Say("Took " + chosen.DisplayName + " from the crate.");
            RefreshLabels();
            run.SaveRun();
            tooltip.Hide();
            UIFocusGuard.Focus(crateCard.gameObject);
            lastSelected = null;
        }

        // ---- layout helpers

        private void Say(string text) => messageLabel.text = text;

        private GameObject FirstFocus()
        {
            foreach (ShopCard card in cards)
                if (card.gameObject.activeSelf && !visit.IsSold(card.OfferIndex))
                    return card.gameObject;
            return rerollButton.gameObject;
        }

        private IEnumerable<Selectable> AllSelectables()
        {
            foreach (ShopCard card in cards)
                yield return card.Button;
            yield return rerollButton;
            yield return leaveButton;
            yield return menuButton;
        }

        // Grid navigation: left/right along a row, up/down to the nearest item (by x position) of the next row.
        private void BuildNavigation()
        {
            var top = new List<Selectable>();
            foreach (ShopCard c in armCards)
                if (c.gameObject.activeSelf)
                    top.Add(c.Button);
            var middle = new List<Selectable>();
            foreach (ShopCard c in armamentCards)
                if (c.gameObject.activeSelf)
                    middle.Add(c.Button);
            if (crateCard.gameObject.activeSelf)
                middle.Add(crateCard.Button);
            var bottom = new List<Selectable> { rerollButton, leaveButton, menuButton };

            var rows = new List<List<Selectable>>();
            if (top.Count > 0)
                rows.Add(top);
            if (middle.Count > 0)
                rows.Add(middle);
            rows.Add(bottom);

            for (int r = 0; r < rows.Count; r++)
            {
                List<Selectable> row = rows[r];
                for (int i = 0; i < row.Count; i++)
                {
                    var nav = new Navigation
                    {
                        mode = Navigation.Mode.Explicit,
                        selectOnLeft = i > 0 ? row[i - 1] : null,
                        selectOnRight = i < row.Count - 1 ? row[i + 1] : null,
                        selectOnUp = r > 0 ? Nearest(rows[r - 1], row[i]) : null,
                        selectOnDown = r < rows.Count - 1 ? Nearest(rows[r + 1], row[i]) : null,
                    };
                    row[i].navigation = nav;
                }
            }
        }

        private static Selectable Nearest(List<Selectable> row, Selectable from)
        {
            float x = from.transform.position.x;
            Selectable best = row[0];
            float bestDistance = float.MaxValue;
            foreach (Selectable candidate in row)
            {
                float distance = Mathf.Abs(candidate.transform.position.x - x);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }
            return best;
        }

        private void ApplyHints()
        {
            PromptHint.Show(hintLabel,
                PromptHint.P(UiAction.Confirm, "Buy"), PromptHint.P(UiAction.Reroll, shownRerollCost >= 0 ? "Reroll \u00B7 " + shownRerollCost : "Reroll"),
                PromptHint.P(UiAction.Details, "Details"), PromptHint.P(UiAction.Back, "Leave"));
        }
    }
}
