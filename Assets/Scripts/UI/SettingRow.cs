using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>What a settings row looks like and how it reads its value.</summary>
    public enum SettingKind
    {
        /// <summary>Arrows around a value name (resolution, cosmetic part): the row adjusts by left / right.</summary>
        Choice,
        /// <summary>A slider track with a handle and a number (volumes, sensitivity).</summary>
        Slider,
        /// <summary>A pill switch with an ON / OFF word.</summary>
        Toggle,
        /// <summary>A button-like row: a caption on the right (a button name, "Reset"); Submit / click runs it, left / right do nothing.</summary>
        Action,
    }

    /// <summary>
    /// One row of the Settings screen (and of the Character Creation part list): a label and a value. Left and Right change
    /// the value, a click steps it forward (a toggle flips). The row shows its value as a slider, a toggle or arrows,
    /// depending on <see cref="SettingKind"/>, using the VoxVegetallis row look: marble when idle, corn with a gold ring
    /// while focused. The visual pieces are plain child objects wired on the prefab.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class SettingRow : MonoBehaviour, IMoveHandler, ICancelHandler, ISelectHandler, IDeselectHandler, IPointerEnterHandler,
                                      IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private GameObject focusRing;
        [SerializeField] private TMP_Text label;
        [Tooltip("Small text after the label (for example the controller families a setting applies to).")]
        [SerializeField] private TMP_Text subLabel;
        [SerializeField] private TMP_Text value;
        [Tooltip("Second line under the value of a Choice row (\"2 / 3\").")]
        [SerializeField] private TMP_Text valueSub;
        [Tooltip("Value text inside the Choice group (Settings rows). Empty = the row Value text.")]
        [SerializeField] private TMP_Text choiceValue;
        [Header("Slider")]
        [SerializeField] private GameObject sliderGroup;
        [SerializeField] private RectTransform sliderTrack;
        [SerializeField] private RectTransform sliderFill;
        [SerializeField] private Image sliderHandle;
        [Header("Toggle")]
        [SerializeField] private GameObject toggleGroup;
        [SerializeField] private Image toggleTrack;
        [SerializeField] private RectTransform toggleKnob;
        [SerializeField] private TMP_Text toggleWord;
        [Header("Choice")]
        [SerializeField] private GameObject choiceGroup;
        [SerializeField] private Button arrowLeft;
        [SerializeField] private Button arrowRight;

        private const float SliderEdge = 14f;          // handle travel inset at each end of the track
        private const float SliderHitPadding = 24f;    // how far past the track ends a press still counts

        private SettingKind kind = SettingKind.Choice;
        private Func<string> valueText;
        private Func<string> subText;
        private Func<float> fraction;
        private Action<int> adjust;
        private Action<float> setFraction;
        private bool sliderPressed;
        private int suppressClickFrame = -1;
        private bool focused;

        public event Action Cancelled;

        public Button Button { get; private set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
            Button.onClick.AddListener(() =>
            {
                // A click that set a slider by position must not also step it.
                if (Time.frameCount > suppressClickFrame)
                    Adjust(1);
            });
            if (arrowLeft != null)
                arrowLeft.onClick.AddListener(() => Adjust(-1));
            if (arrowRight != null)
                arrowRight.onClick.AddListener(() => Adjust(1));
        }

        /// <summary>
        /// valueSource is the value as text ("80", "On", "1280 x 720"). For a slider, fractionSource gives 0..1. subSource is
        /// the optional second line of a Choice row.
        /// </summary>
        public void Bind(string labelText, Func<string> valueSource, Action<int> onAdjust,
                         SettingKind rowKind = SettingKind.Choice, Func<float> fractionSource = null, Func<string> subSource = null,
                         Action<float> fractionSetter = null)
        {
            label.text = labelText;
            valueText = valueSource;
            adjust = onAdjust;
            kind = rowKind;
            fraction = fractionSource;
            setFraction = fractionSetter;
            subText = subSource;
            if (sliderGroup != null) sliderGroup.SetActive(kind == SettingKind.Slider);
            if (toggleGroup != null) toggleGroup.SetActive(kind == SettingKind.Toggle);
            if (choiceGroup != null) choiceGroup.SetActive(kind == SettingKind.Choice || kind == SettingKind.Action);
            if (arrowLeft != null) arrowLeft.gameObject.SetActive(kind == SettingKind.Choice);
            if (arrowRight != null) arrowRight.gameObject.SetActive(kind == SettingKind.Choice);
            ApplyLook();
            Refresh();
        }

        public void Refresh()
        {
            string text = valueText != null ? valueText() : "";
            if (subLabel != null)
                subLabel.text = kind != SettingKind.Choice && subText != null ? subText() : "";
            switch (kind)
            {
                case SettingKind.Slider:
                    value.text = text;
                    SetFraction(fraction != null ? fraction() : 0f);
                    break;
                case SettingKind.Toggle:
                    bool on = string.Equals(text, "On", StringComparison.OrdinalIgnoreCase);
                    if (toggleWord != null) toggleWord.text = on ? "ON" : "OFF";
                    UITheme theme = UITheme.Current;
                    if (theme != null && toggleTrack != null)
                        toggleTrack.sprite = on ? theme.ToggleOn : theme.ToggleOff;
                    if (toggleKnob != null)
                    {
                        // Knob sits 9 px (2x art) inset from the left (off) or right (on) end of the track.
                        float travel = (((RectTransform)toggleTrack.transform).rect.width - toggleKnob.rect.width) * 0.5f - 4.5f;
                        toggleKnob.anchoredPosition = new Vector2(on ? travel : -travel, 0f);
                    }
                    value.text = "";
                    break;
                default:
                    if (choiceValue != null)
                    {
                        choiceValue.text = text;
                        value.text = "";
                    }
                    else
                    {
                        value.text = text;
                    }
                    if (valueSub != null)
                        valueSub.text = subText != null ? subText() : "";
                    break;
            }
        }

        private void SetFraction(float f)
        {
            f = Mathf.Clamp01(f);
            if (sliderFill != null)
            {
                sliderFill.anchorMax = new Vector2(f, sliderFill.anchorMax.y);
                sliderFill.gameObject.SetActive(f > 0.02f);
            }
            if (sliderHandle != null && sliderTrack != null)
            {
                RectTransform handle = sliderHandle.rectTransform;
                float inner = sliderTrack.rect.width - 2f * SliderEdge;
                handle.anchoredPosition = new Vector2(-sliderTrack.rect.width * 0.5f + SliderEdge + inner * f, 0f);
            }
        }

        private void ApplyLook()
        {
            UITheme theme = UITheme.Current;
            if (theme == null)
                return;
            SetFocused(focused);
            if (sliderTrack != null && sliderTrack.TryGetComponent(out Image track) && theme.SliderTrack != null)
            {
                track.sprite = theme.SliderTrack;
                track.type = Image.Type.Sliced;
                track.pixelsPerUnitMultiplier = theme.BorderMultiplier;
            }
            if (sliderFill != null && sliderFill.TryGetComponent(out Image fillImage) && theme.SliderFill != null)
            {
                fillImage.sprite = theme.SliderFill;
                fillImage.type = Image.Type.Sliced;
                fillImage.pixelsPerUnitMultiplier = theme.BorderMultiplier;
                fillImage.color = theme.Leaf;
            }
            if (toggleKnob != null && toggleKnob.TryGetComponent(out Image knob) && theme.ToggleKnob != null)
                knob.sprite = theme.ToggleKnob;
            if (arrowLeft != null && arrowLeft.targetGraphic is Image left && theme.ButtonArrowLeft != null)
                left.sprite = theme.ButtonArrowLeft;
            if (arrowRight != null && arrowRight.targetGraphic is Image right && theme.ButtonArrowRight != null)
                right.sprite = theme.ButtonArrowRight;
        }

        private void SetFocused(bool on)
        {
            focused = on;
            UITheme theme = UITheme.Current;
            if (theme == null)
                return;
            if (background != null)
            {
                Sprite sprite = on ? theme.PanelCorn : theme.Button.normal;
                if (sprite != null)
                {
                    background.sprite = sprite;
                    background.type = Image.Type.Sliced;
                    background.pixelsPerUnitMultiplier = theme.BorderMultiplier;
                }
            }
            if (focusRing != null)
                focusRing.SetActive(on);
            if (sliderHandle != null)
            {
                Sprite handle = on && theme.SliderHandleFocused != null ? theme.SliderHandleFocused : theme.SliderHandle;
                if (handle != null)
                    sliderHandle.sprite = handle;
            }
            if (subLabel != null)
                subLabel.color = theme.MutedText;
        }

        public void OnSelect(BaseEventData eventData) => SetFocused(true);

        public void OnDeselect(BaseEventData eventData) => SetFocused(false);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }

        private void OnEnable() => SetFocused(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject);

        public void OnMove(AxisEventData eventData)
        {
            if (kind == SettingKind.Action)
                return;   // an action row runs on Submit, not on left / right
            if (eventData.moveDir == MoveDirection.Left)
                Adjust(-1);
            else if (eventData.moveDir == MoveDirection.Right)
                Adjust(1);
        }

        public void OnCancel(BaseEventData eventData) => Cancelled?.Invoke();

        // Slider rows: pressing or dragging on the track sets the value where the pointer is (steps stay on left / right).
        public void OnPointerDown(PointerEventData eventData)
        {
            if (kind != SettingKind.Slider || setFraction == null || sliderTrack == null || eventData.button != PointerEventData.InputButton.Left)
                return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderTrack, eventData.position, eventData.pressEventCamera, out Vector2 local);
            Rect bounds = sliderTrack.rect;   // local space: the pivot is not necessarily the centre
            if (local.x < bounds.xMin - SliderHitPadding || local.x > bounds.xMax + SliderHitPadding)
                return;
            sliderPressed = true;
            suppressClickFrame = Time.frameCount;
            SetFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (sliderPressed)
                SetFromPointer(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (sliderPressed)
                suppressClickFrame = Time.frameCount;   // the click event follows the release in the same frame
            sliderPressed = false;
        }

        private void SetFromPointer(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(sliderTrack, eventData.position, eventData.pressEventCamera, out Vector2 local);
            float inner = sliderTrack.rect.width - 2f * SliderEdge;
            setFraction(Mathf.Clamp01((local.x - sliderTrack.rect.xMin - SliderEdge) / inner));
            Refresh();
        }

        private void Adjust(int direction)
        {
            adjust?.Invoke(direction);
            Refresh();
        }
    }
}
