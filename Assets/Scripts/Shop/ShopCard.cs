using System;
using BulletHell.UI;
using BulletHell.Weapons;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.Shop
{
    /// <summary>
    /// One card in the Shop (also used for the crate choices): icon, name, rarity frame and price tag. The focused card
    /// lifts; buying flies its icon to the inventory and stamps SOLD on it; an unaffordable price shows red. All the
    /// visuals are child objects with swappable sprites, so final art drops in without code changes.
    /// </summary>
    public sealed class ShopCard : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
    {
        [SerializeField] private Button button;
        [Tooltip("Everything that is drawn; it is what lifts, so the clickable rectangle stays put.")]
        [SerializeField] private RectTransform body;
        [Tooltip("Card frame, tinted by rarity.")]
        [SerializeField] private Image frame;
        [SerializeField] private Image focusRing;
        [SerializeField] private Image icon;
        [Tooltip("A letter drawn over placeholder icons (an item with no icon yet).")]
        [SerializeField] private Text iconLetter;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text rarityLabel;
        [SerializeField] private Image priceTag;
        [SerializeField] private Text priceLabel;
        [SerializeField] private GameObject soldStamp;
        [SerializeField] private CanvasGroup soldGroup;
        [Header("Feel")]
        [SerializeField, Min(1f)] private float liftScale = 1.08f;
        [SerializeField, Min(0f)] private float liftHeight = 18f;
        [SerializeField, Min(0.01f)] private float liftSeconds = 0.12f;
        [SerializeField, Min(0.05f)] private float flySeconds = 0.45f;
        [SerializeField] private Color priceColor = new Color(1f, 0.97f, 0.88f);
        [SerializeField] private Color unaffordableColor = new Color(1f, 0.3f, 0.3f);

        private Vector2 restPosition;
        private bool restCached;
        private bool sold;

        public Button Button => button;
        public bool IsSold => sold;
        /// <summary>Offer number in the current stock (arms, then armaments, then the crate).</summary>
        public int OfferIndex { get; set; } = -1;

        /// <summary>The focused card moves to the front of its siblings. Off for cards inside a layout group (it would reorder the grid).</summary>
        public bool RaiseOnFocus { get; set; } = true;

        public event Action<ShopCard> Clicked;

        private void Awake()
        {
            button.onClick.AddListener(() => Clicked?.Invoke(this));
            UITheme theme = UITheme.Current;
            if (focusRing != null)
            {
                if (theme != null && theme.HudFrameFocused != null)
                {
                    focusRing.sprite = theme.HudFrameFocused;
                    focusRing.type = Image.Type.Sliced;
                    focusRing.pixelsPerUnitMultiplier = theme.BorderMultiplier;
                }
                focusRing.enabled = false;
            }
        }

        private void CacheRest()
        {
            if (restCached)
                return;
            restPosition = body.anchoredPosition;
            restCached = true;
        }

        /// <summary>Fills the card. iconSprite may be null (a placeholder is drawn with the item's first letter).</summary>
        public void Set(in ShopEntry entry, Sprite iconSprite, Sprite placeholder, Color rarityColor, string rarityName, bool isSold, bool affordable)
        {
            CacheRest();
            Tween.StopAll(body);
            body.localScale = Vector3.one;
            body.anchoredPosition = restPosition;
            if (focusRing != null)
                focusRing.enabled = false;

            gameObject.SetActive(true);
            nameLabel.text = entry.Name;
            rarityLabel.text = rarityName;
            rarityLabel.color = Color.Lerp(rarityColor, Color.black, 0.4f); // darker than the frame tint: it sits on the cream card body
            frame.color = Color.Lerp(Color.white, rarityColor, 0.85f);

            bool hasIcon = iconSprite != null;
            icon.sprite = hasIcon ? iconSprite : placeholder;
            icon.color = hasIcon ? Color.white : rarityColor;
            icon.enabled = icon.sprite != null;
            if (iconLetter != null)
            {
                iconLetter.enabled = !hasIcon;
                iconLetter.text = entry.Name.Length > 0 ? entry.Name.Substring(0, 1) : "";
            }

            priceLabel.text = entry.Price > 0 ? entry.Price.ToString() : "";
            if (priceTag != null)
                priceTag.gameObject.SetActive(entry.Price > 0);
            SetAffordable(affordable);
            SetSold(isSold, false);
        }

        /// <summary>Price tag text turns red when the player cannot pay.</summary>
        public void SetAffordable(bool affordable) => priceLabel.color = affordable ? priceColor : unaffordableColor;

        public void SetSold(bool isSold, bool animate)
        {
            sold = isSold;
            button.interactable = true; // stays selectable so controller focus never gets stuck
            if (soldStamp == null)
                return;
            soldStamp.SetActive(isSold);
            if (!isSold)
                return;
            Tween.StopAll(soldStamp.transform);
            soldStamp.transform.localScale = Vector3.one;
            if (soldGroup != null)
                soldGroup.alpha = 1f;
            if (animate)
            {
                // The SOLD stamp slams down: big and faint, settling to size.
                soldStamp.transform.localScale = Vector3.one * 2.4f;
                if (soldGroup != null)
                    soldGroup.alpha = 0f;
                Tween.Scale(soldStamp.transform, 1f, 0.28f, Ease.OutBack, useUnscaledTime: true);
                if (soldGroup != null)
                    Tween.Alpha(soldGroup, 1f, 0.15f, Ease.OutQuad, useUnscaledTime: true);
            }
        }

        /// <summary>The card icon flies to the inventory target, then the SOLD stamp lands.</summary>
        public void PlayBuy(RectTransform target)
        {
            UiSound.Play(UiSoundKind.Buy);
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || target == null || !icon.enabled)
            {
                SetSold(true, true);
                return;
            }

            // A copy of the icon that leaves the card and shrinks into the inventory icon.
            var ghost = new GameObject("BuyGhost", typeof(RectTransform), typeof(Image));
            ghost.transform.SetParent(canvas.rootCanvas.transform, false);
            var ghostRect = (RectTransform)ghost.transform;
            ghostRect.position = icon.transform.position;
            ghostRect.sizeDelta = ((RectTransform)icon.transform).rect.size;
            var ghostImage = ghost.GetComponent<Image>();
            ghostImage.sprite = icon.sprite;
            ghostImage.color = icon.color;
            ghostImage.preserveAspect = true;
            ghostImage.raycastTarget = false;
            ghostRect.SetAsLastSibling();

            Tween.Position(ghostRect, target.position, flySeconds, Ease.InBack, useUnscaledTime: true);
            Tween.Scale(ghostRect, 0.25f, flySeconds, Ease.InQuad, useUnscaledTime: true)
                 .OnComplete(() =>
                 {
                     if (ghost != null)
                         Destroy(ghost);
                     if (this != null)
                         SetSold(true, true);
                 });
        }

        /// <summary>
        /// The icon flies to a target (a bubble, a slot) and shrinks into it, then onDone runs. No SOLD stamp: used by the
        /// Armory when an item moves from the inventory grid onto an arm.
        /// </summary>
        public void PlayFly(RectTransform target, Action onDone)
        {
            UiSound.Play(UiSoundKind.Equip);
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || target == null || !icon.enabled)
            {
                onDone?.Invoke();
                return;
            }

            var ghost = new GameObject("FlyGhost", typeof(RectTransform), typeof(Image));
            ghost.transform.SetParent(canvas.rootCanvas.transform, false);
            var ghostRect = (RectTransform)ghost.transform;
            ghostRect.position = icon.transform.position;
            ghostRect.sizeDelta = ((RectTransform)icon.transform).rect.size;
            var ghostImage = ghost.GetComponent<Image>();
            ghostImage.sprite = icon.sprite;
            ghostImage.color = icon.color;
            ghostImage.preserveAspect = true;
            ghostImage.raycastTarget = false;
            ghostRect.SetAsLastSibling();

            Tween.Position(ghostRect, target.position, flySeconds, Ease.InBack, useUnscaledTime: true);
            Tween.Scale(ghostRect, 0.3f, flySeconds, Ease.InQuad, useUnscaledTime: true)
                 .OnComplete(() =>
                 {
                     if (ghost != null)
                         Destroy(ghost);
                     onDone?.Invoke();
                 });
        }

        /// <summary>Greys the card out (an item that does not fit the current selection). It stays selectable so focus never gets stuck.</summary>
        public void SetDimmed(bool dimmed)
        {
            var group = body.GetComponent<CanvasGroup>();
            if (group == null)
                group = body.gameObject.AddComponent<CanvasGroup>();
            group.alpha = dimmed ? 0.4f : 1f;
        }

        /// <summary>Shows a small text (a count like "x3") in the price tag; empty hides the tag when the card has no price.</summary>
        public void SetBadge(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            if (priceTag != null)
                priceTag.gameObject.SetActive(show);
            priceLabel.text = show ? text : "";
            priceLabel.color = priceColor;
        }

        /// <summary>A short shake: "you cannot buy this".</summary>
        public void Shake()
        {
            UiSound.Play(UiSoundKind.Error);
            CacheRest();
            Tween.StopAll(body);
            body.anchoredPosition = restPosition;
            Tween.PunchLocalPosition(body, new Vector3(14f, 0f, 0f), 0.3f, 10, useUnscaledTime: true);
        }

        public void OnSelect(BaseEventData eventData) => Lift(true);

        public void OnDeselect(BaseEventData eventData) => Lift(false);

        // Mouse: hovering a card focuses it, like moving the stick onto it.
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        private void OnDisable()
        {
            if (body != null)
            {
                Tween.StopAll(body);
                if (restCached)
                    body.anchoredPosition = restPosition;
                body.localScale = Vector3.one;
            }
            if (focusRing != null)
                focusRing.enabled = false;
        }

        private void Lift(bool up)
        {
            CacheRest();
            Tween.StopAll(body);
            if (focusRing != null)
                focusRing.enabled = up;
            if (up && RaiseOnFocus)
                transform.SetAsLastSibling(); // the lifted card draws over its neighbours
            Tween.Scale(body, up ? liftScale : 1f, liftSeconds, Ease.OutQuad, useUnscaledTime: true);
            Tween.UIAnchoredPosition(body, restPosition + (up ? new Vector2(0f, liftHeight) : Vector2.zero), liftSeconds, Ease.OutQuad, useUnscaledTime: true);
        }
    }
}
