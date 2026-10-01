using System;
using TMPro;
using System.Collections.Generic;
using System.Text;
using BulletHell.Core;
using BulletHell.Cosmetics;
using BulletHell.Input;
using BulletHell.Platform;
using BulletHell.Player;
using BulletHell.Shop;
using BulletHell.UI;
using BulletHell.Weapons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BulletHell.Armory
{
    /// <summary>
    /// The Armory (between the Shop and the next round): the gladiator with the 8 arm slots on the ring on the left, an
    /// inventory of cards (Arms / Armaments tabs) on the right.
    ///
    /// Flow: RING (focus a slot; stats of that arm) -> Cross on an arm opens its armament BUBBLES above it -> Cross on a bubble
    /// jumps to the inventory (PICKING) where focusing an armament previews the change (stats before -> after) and Cross
    /// equips it into the bubble; Triangle on a filled bubble returns the armament; Cross on an empty arm slot jumps to the
    /// Arms tab to place a spare arm; Triangle on an arm removes it (with its armaments) to the inventory. Circle backs out one
    /// level (picking -> bubbles -> ring). L1/R1 switch tabs. Every change saves the run; "Fight!" starts the next round.
    /// </summary>
    public sealed class ArmoryScreen : MonoBehaviour
    {
        private enum Level { Ring, Bubbles, Picking }

        [Header("Left: ring, bubbles, info")]
        [SerializeField] private ArmoryRing ring;
        [SerializeField] private RectTransform bubbleRoot;
        [SerializeField] private ArmoryBubble[] bubbles = new ArmoryBubble[ArmInstance.MaxArmamentSlots];
        [SerializeField] private ArmoryTether[] tethers = new ArmoryTether[ArmInstance.MaxArmamentSlots];
        [SerializeField, Min(50f)] private float bubbleRadius = 190f;
        [SerializeField] private TMP_Text infoLabel;

        [Header("Right: inventory")]
        [SerializeField] private ArmoryTabs tabs;
        [SerializeField] private ShopCard cardPrefab;
        [SerializeField] private RectTransform gridContent;
        [SerializeField] private ScrollRect gridScroll;
        [SerializeField] private TMP_Text emptyLabel;
        [SerializeField] private Sprite placeholderIcon;

        [Header("Chrome")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private Button removeButton;
        [SerializeField] private Button fightButton;
        [SerializeField] private Button menuButton;

        [Header("Input")]
        [SerializeField] private MenuInputReader input;
        [SerializeField] private ButtonGlyphLibrary glyphs;

        private static readonly string[] SlotNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        private const int GridColumns = 3;

        private RunState state;
        private RunManager run;
        private RarityTable table;
        private ProfileService profile;
        private bool showParts;
        private Level level = Level.Ring;
        private int selectedSlot = -1;
        private int bubbleIndex;
        private bool bubblesOpen;
        private bool built;
        private GameObject lastFocus;
        private readonly List<ShopCard> cards = new List<ShopCard>();
        private List<ArmamentStack> stacks = new List<ArmamentStack>();
        private List<ArmInstance> spares = new List<ArmInstance>();
        private readonly StringBuilder builder = new StringBuilder(256);

        public event Action ContinuePressed;
        public event Action MenuPressed;

        private bool PlacingArm => level == Level.Picking && selectedSlot >= 0 && state.Loadout[selectedSlot] == null;

        private void Awake()
        {
            fightButton.onClick.AddListener(() => ContinuePressed?.Invoke());
            menuButton.onClick.AddListener(() => MenuPressed?.Invoke());
            removeButton.onClick.AddListener(RemoveFocused);
        }

        private void Relay(GameObject target)
        {
            if (target.TryGetComponent(out CancelRelay relay))
                relay.Cancelled += OnCancel;
        }

        private void EnsureBuilt()
        {
            if (built)
                return;
            built = true;

            for (int i = 0; i < ring.Slots.Count; i++)
            {
                int slot = i;
                ring.Slots[i].Button.onClick.AddListener(() => OnSlotClicked(slot));
                Relay(ring.Slots[i].gameObject);
            }
            for (int i = 0; i < bubbles.Length; i++)
            {
                int index = i;
                bubbles[i].Slot = i;
                bubbles[i].Button.onClick.AddListener(() => OnBubbleClicked(index));
                Relay(bubbles[i].gameObject);
                bubbles[i].gameObject.SetActive(false);
                tethers[i].Unlink();
            }
            tabs.Picked += OnTabPicked;
            foreach (Selectable s in new Selectable[] { tabs.ArmsButton, tabs.ArmamentsButton, fightButton, menuButton, removeButton })
                Relay(s.gameObject);
        }

        // ---- showing and hiding

        public void Show(RunState runState)
        {
            ScreenTransition.In(gameObject);
            EnsureBuilt();
            GameServices services = GameServices.Ensure();
            run = services.Run;
            table = services.Config.RarityTable;
            profile = services.Profile;
            showParts = services.Config.ShowCustomizationInGame;
            state = runState;

            level = Level.Ring;
            selectedSlot = -1;
            bubblesOpen = false;
            lastFocus = null;
            foreach (ArmoryBubble b in bubbles)
                b.gameObject.SetActive(false);
            foreach (ArmoryTether t in tethers)
                t.Unlink();

            titleLabel.text = "ARMORY";
            messageLabel.text = "";
            tabs.SetActive(ArmoryTabs.Armaments);
            RefreshAll();
            BuildNavigation();

            if (input != null)
            {
                input.TabPrevPressed += PreviousTab;
                input.TabNextPressed += NextTab;
                input.RemovePressed += RemoveFocused;
            }
            UIFocusGuard.Focus(FirstSlotToFocus().gameObject);
        }

        public void Hide()
        {
            if (input != null)
            {
                input.TabPrevPressed -= PreviousTab;
                input.TabNextPressed -= NextTab;
                input.RemovePressed -= RemoveFocused;
            }
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (input != null)
            {
                input.TabPrevPressed -= PreviousTab;
                input.TabNextPressed -= NextTab;
                input.RemovePressed -= RemoveFocused;
            }
        }

        private ArmorySlotButton FirstSlotToFocus()
        {
            for (int i = 0; i < ring.Slots.Count; i++)
                if (state.Loadout[i] != null)
                    return ring.Slots[i];
            return ring.Slots[0];
        }

        // ---- refreshing

        private void RefreshAll()
        {
            ring.Refresh(state, profile, showParts);
            RebuildGrid();
            ApplyHints();
            RenderInfo();
        }

        private void RebuildGrid()
        {
            stacks = ArmoryInventoryView.Group(state.Armaments);
            spares = ArmoryInventoryView.SpareArms(state.SpareArms);
            tabs.SetCounts(spares.Count, stacks.Count);
            tabs.SetActive(tabs.Active);

            int items = tabs.Active == ArmoryTabs.Arms ? spares.Count : stacks.Count;
            while (cards.Count < items)
                AddCard();

            for (int i = 0; i < cards.Count; i++)
            {
                ShopCard card = cards[i];
                if (i >= items)
                {
                    card.gameObject.SetActive(false);
                    card.OfferIndex = -1;
                    continue;
                }

                card.OfferIndex = i;
                if (tabs.Active == ArmoryTabs.Arms)
                {
                    ArmInstance arm = spares[i];
                    var entry = new ShopEntry { Kind = ShopItemKind.Arm, Arm = arm.Data, Price = 0 };
                    card.Set(entry, arm.Data.Sprite, placeholderIcon, table.ColorOf(arm.Data.Rarity), arm.Data.Rarity.ToString().ToUpperInvariant(), false, true);
                    card.SetBadge(arm.ArmamentCount > 0 ? "+" + arm.ArmamentCount : "");
                }
                else
                {
                    ArmamentData armament = stacks[i].Armament;
                    var entry = new ShopEntry { Kind = ShopItemKind.Armament, Armament = armament, Price = 0 };
                    card.Set(entry, armament.Icon, placeholderIcon, table.ColorOf(armament.Rarity), armament.Rarity.ToString().ToUpperInvariant(), false, true);
                    card.SetBadge(stacks[i].Count > 1 ? "x" + stacks[i].Count : "");
                }
            }

            emptyLabel.gameObject.SetActive(items == 0);
            emptyLabel.text = tabs.Active == ArmoryTabs.Arms ? "No spare arms." : "No armaments in the inventory.";
            ApplyDimming();
            BuildNavigation();
        }

        private void AddCard()
        {
            ShopCard card = Instantiate(cardPrefab, gridContent);
            card.name = "InventoryCard" + cards.Count;
            card.RaiseOnFocus = false;
            card.Clicked += OnCardClicked;
            Relay(card.gameObject);
            cards.Add(card);
        }

        // Cards that do not fit the current selection are dimmed (they stay selectable so focus never gets stuck).
        private void ApplyDimming()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (!cards[i].gameObject.activeSelf)
                    continue;
                cards[i].SetDimmed(ShouldDim(i));
            }
        }

        private bool ShouldDim(int index)
        {
            if (level != Level.Picking || selectedSlot < 0)
                return false;
            if (PlacingArm)
                return tabs.Active != ArmoryTabs.Arms;
            if (tabs.Active != ArmoryTabs.Armaments)
                return true;   // arms are placed from an empty arm slot, not into a bubble
            ArmInstance arm = state.Loadout[selectedSlot];
            return arm == null || index >= stacks.Count || !ArmoryPreview.Equip(arm, bubbleIndex, stacks[index].Armament).CanEquip;
        }

        // ---- input events

        private void OnSlotClicked(int slot)
        {
            if (level == Level.Picking)
                return;
            if (level == Level.Bubbles && slot == selectedSlot)
                return;
            if (level == Level.Bubbles)
                CloseBubbles();

            selectedSlot = slot;
            if (state.Loadout[slot] == null)
                BeginPlaceArm(slot);
            else
                OpenBubbles(slot);
        }

        private void OnBubbleClicked(int index)
        {
            if (level != Level.Bubbles)
                return;
            bubbleIndex = index;
            BeginPickArmament(index);
        }

        private void OnCardClicked(ShopCard card)
        {
            int index = card.OfferIndex;
            if (index < 0)
                return;
            if (level != Level.Picking)
            {
                Say("Select an arm slot, then a bubble, to equip this.");
                return;
            }

            if (PlacingArm)
            {
                if (tabs.Active != ArmoryTabs.Arms)
                {
                    Say("Switch to the Arms tab to place an arm.");
                    card.Shake();
                    return;
                }
                PlaceSpare(card, spares[index]);
                return;
            }

            if (tabs.Active != ArmoryTabs.Armaments)
            {
                Say("Arms are placed from an empty arm slot.");
                card.Shake();
                return;
            }
            EquipArmament(card, stacks[index].Armament);
        }

        private void OnTabPicked(int tab)
        {
            SetTab(tab);
            if (level == Level.Picking)
                FocusFirstCard();
        }

        private void PreviousTab() => SwitchTab(-1);

        private void NextTab() => SwitchTab(1);

        private void SwitchTab(int direction)
        {
            SetTab(Mathf.Clamp(tabs.Active + direction, 0, 1));
            if (level == Level.Picking)
                FocusFirstCard();
        }

        private void SetTab(int tab)
        {
            if (tab == tabs.Active)
                return;
            tabs.SetActive(tab);
            RebuildGrid();
            RenderInfo();
            ApplyHints();
        }

        // Circle: one level back. At the ring it does nothing (Fight and the Main Menu button are for leaving).
        private void OnCancel()
        {
            switch (level)
            {
                case Level.Picking:
                    if (PlacingArm)
                    {
                        EndPicking(Level.Ring);
                        FocusSlot(selectedSlot);
                    }
                    else
                    {
                        EndPicking(Level.Bubbles);
                        UIFocusGuard.Focus(bubbles[bubbleIndex].gameObject);
                    }
                    break;
                case Level.Bubbles:
                    CloseBubbles();
                    int slot = selectedSlot;
                    level = Level.Ring;
                    ring.SetSelected(-1);
                    selectedSlot = -1;
                    ApplyHints();
                    FocusSlot(slot);
                    break;
            }
        }

        // ---- levels

        private void OpenBubbles(int slot)
        {
            level = Level.Bubbles;
            selectedSlot = slot;
            bubbleIndex = 0;
            ring.SetSelected(slot);
            bubblesOpen = false;
            ShowBubbles(true);
            ApplyHints();
            UIFocusGuard.Focus(bubbles[0].gameObject);
        }

        private void ShowBubbles(bool animate)
        {
            ArmInstance arm = state.Loadout[selectedSlot];
            if (arm == null)
                return;

            int count = arm.SlotCount;
            Vector2 origin = ring.SlotPosition(selectedSlot);
            float spread = count == 1 ? 0f : count == 2 ? 62f : 104f;
            for (int i = 0; i < bubbles.Length; i++)
            {
                ArmoryBubble bubble = bubbles[i];
                if (i >= count)
                {
                    bubble.Close();
                    tethers[i].Unlink();
                    continue;
                }

                ArmamentData armament = arm.GetArmament(i);
                bubble.Set(armament, armament != null ? table.ColorOf(armament.Rarity) : Color.white, placeholderIcon);
                if (animate || !bubblesOpen)
                {
                    float angle = count == 1 ? 0f : Mathf.Lerp(-spread * 0.5f, spread * 0.5f, i / (count - 1f)) * Mathf.Deg2Rad;
                    Vector2 position = origin + new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * bubbleRadius + new Vector2(0f, 30f);
                    bubble.Open(position, i * 0.06f);
                    tethers[i].Link(ring.Anchor(selectedSlot), bubble.Rect, arm.Data.IdColor);
                }
            }
            bubblesOpen = true;
            SetupBubbleNavigation(count);
        }

        private void CloseBubbles()
        {
            for (int i = 0; i < bubbles.Length; i++)
            {
                bubbles[i].Close(i * 0.03f);
                tethers[i].Unlink();
            }
            bubblesOpen = false;
        }

        private void BeginPickArmament(int index)
        {
            level = Level.Picking;
            bubbleIndex = index;
            tabs.SetActive(ArmoryTabs.Armaments);
            RebuildGrid();
            ApplyHints();
            FocusFirstCard();
            RenderInfo();
        }

        private void BeginPlaceArm(int slot)
        {
            level = Level.Picking;
            selectedSlot = slot;
            ring.SetSelected(-1);
            tabs.SetActive(ArmoryTabs.Arms);
            RebuildGrid();
            ApplyHints();
            if (spares.Count == 0)
                Say("No spare arms to place. Buy one in the Shop.");
            FocusFirstCard();
            RenderInfo();
        }

        private void EndPicking(Level next)
        {
            level = next;
            if (next == Level.Ring)
            {
                ring.SetSelected(-1);
                selectedSlot = selectedSlot; // kept: focus returns to that slot
            }
            RebuildGrid();
            ApplyHints();
            RenderInfo();
        }

        private void FocusFirstCard()
        {
            foreach (ShopCard card in cards)
            {
                if (!card.gameObject.activeSelf)
                    continue;
                // Prefer a card that fits, so Cross does something useful straight away.
                if (!ShouldDim(card.OfferIndex))
                {
                    UIFocusGuard.Focus(card.gameObject);
                    return;
                }
            }
            foreach (ShopCard card in cards)
                if (card.gameObject.activeSelf)
                {
                    UIFocusGuard.Focus(card.gameObject);
                    return;
                }
            UIFocusGuard.Focus(tabs.Active == ArmoryTabs.Arms ? tabs.ArmsButton.gameObject : tabs.ArmamentsButton.gameObject);
        }

        private void FocusSlot(int slot)
        {
            UIFocusGuard.Focus(ring.Slots[Mathf.Clamp(slot, 0, ring.Slots.Count - 1)].gameObject);
        }

        // ---- changes (each one saves the run)

        private void EquipArmament(ShopCard card, ArmamentData armament)
        {
            ArmInstance arm = state.Loadout[selectedSlot];
            ArmoryChange change = ArmoryPreview.Equip(arm, bubbleIndex, armament);
            if (!change.CanEquip)
            {
                Say("Cannot equip " + armament.DisplayName + ": " + change.Reason + ".");
                card.Shake();
                return;
            }

            int slot = bubbleIndex;
            if (!arm.TryEquipAt(slot, armament, state.Armaments))
                return;

            Say("Equipped " + armament.DisplayName + (change.Replaced != null ? " (" + change.Replaced.DisplayName + " returned to the inventory)." : "."));
            run.SaveRun();
            ArmoryBubble bubble = bubbles[slot];
            card.PlayFly(bubble.Rect, () =>
            {
                if (this == null || !gameObject.activeInHierarchy)
                    return;
                ShowBubbles(false);
                bubble.Pulse();
            });

            EndPicking(Level.Bubbles);
            UIFocusGuard.Focus(bubble.gameObject);
            ShowBubbles(false);
        }

        private void PlaceSpare(ShopCard card, ArmInstance arm)
        {
            int slot = selectedSlot;
            if (ArmoryActions.PlaceArm(state, slot, arm) != ArmoryResult.Done)
            {
                Say("Could not place that arm.");
                return;
            }

            Say("Placed " + arm.Data.DisplayName + " in slot " + SlotNames[slot] + ".");
            run.SaveRun();
            RectTransform target = (RectTransform)ring.Slots[slot].transform;
            card.PlayFly(target, () =>
            {
                if (this != null && gameObject.activeInHierarchy)
                    RefreshAll();
            });
            EndPicking(Level.Ring);
            RefreshAll();
            FocusSlot(slot);
        }

        // Triangle (or the Remove button): an arm at the ring, an armament on a bubble.
        private void RemoveFocused()
        {
            if (state == null)
                return;
            if (level == Level.Ring)
            {
                int slot = FocusedSlot();
                if (slot < 0 || state.Loadout[slot] == null)
                    return;
                string name = state.Loadout[slot].Data.DisplayName;
                if (ArmoryActions.RemoveArm(state, slot) == ArmoryResult.LastArm)
                {
                    UiSound.Play(UiSoundKind.Error);
                    Say("You must keep at least one arm equipped.");
                    return;
                }
                UiSound.Play(UiSoundKind.Equip);
                Say(name + " moved to the arm inventory with its armaments.");
                run.SaveRun();
                RefreshAll();
                FocusSlot(slot);
            }
            else if (level == Level.Bubbles)
            {
                ArmInstance arm = state.Loadout[selectedSlot];
                ArmamentData armament = arm.GetArmament(bubbleIndex);
                if (armament == null)
                    return;
                arm.Unequip(bubbleIndex, state.Armaments);
                UiSound.Play(UiSoundKind.Equip);
                Say(armament.DisplayName + " returned to the inventory.");
                run.SaveRun();
                ShowBubbles(false);
                bubbles[bubbleIndex].Pulse();
                RebuildGrid();
                RenderInfo();
            }
        }

        private int FocusedSlot()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.TryGetComponent(out ArmorySlotButton slotButton))
                return slotButton.Slot;
            return -1;
        }

        // ---- info panel (follows the focus)

        private void Update()
        {
            if (state == null)
                return;
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != lastFocus)
            {
                lastFocus = selected;
                if (selected != null && selected.TryGetComponent(out ArmoryBubble bubble))
                    bubbleIndex = bubble.Slot;
                RenderInfo();
                ApplyDimming();
            }
        }

        private void RenderInfo()
        {
            builder.Clear();
            GameObject focus = lastFocus;
            ArmorySlotButton slotButton = focus != null ? focus.GetComponent<ArmorySlotButton>() : null;
            ArmoryBubble bubble = focus != null ? focus.GetComponent<ArmoryBubble>() : null;
            ShopCard card = focus != null ? focus.GetComponent<ShopCard>() : null;

            if (card != null && card.OfferIndex >= 0 && level == Level.Picking && selectedSlot >= 0)
                DescribePick(card.OfferIndex);
            else if (card != null && card.OfferIndex >= 0)
                DescribeItem(card.OfferIndex);
            else if (bubble != null && selectedSlot >= 0 && state.Loadout[selectedSlot] != null)
                DescribeBubble(state.Loadout[selectedSlot], bubble.Slot);
            else if (slotButton != null)
                DescribeSlot(slotButton.Slot);
            else if (selectedSlot >= 0 && state.Loadout[selectedSlot] != null)
                DescribeArm(state.Loadout[selectedSlot], selectedSlot);
            else
                builder.Append("Select an arm slot.");

            infoLabel.text = builder.ToString();
            removeButton.gameObject.SetActive(CanRemoveNow());
        }

        private bool CanRemoveNow()
        {
            if (level == Level.Ring)
            {
                int slot = FocusedSlot();
                return slot >= 0 && state.Loadout[slot] != null;
            }
            if (level == Level.Bubbles && selectedSlot >= 0 && state.Loadout[selectedSlot] != null)
                return state.Loadout[selectedSlot].GetArmament(bubbleIndex) != null;
            return false;
        }

        private void DescribeSlot(int slot)
        {
            ArmInstance arm = state.Loadout[slot];
            if (arm == null)
            {
                builder.Append("<b>Slot ").Append(SlotNames[slot]).Append(" - empty</b>\nSelect it to place a spare arm (")
                       .Append(state.SpareArms.Count).Append(state.SpareArms.Count == 1 ? " spare)." : " spares).");
                return;
            }
            DescribeArm(arm, slot);
        }

        private void DescribeArm(ArmInstance arm, int slot)
        {
            builder.Append("<b>").Append(arm.Data.DisplayName).Append("</b>  (slot ").Append(SlotNames[slot]).Append(", ")
                   .Append(arm.SlotCount).Append(arm.SlotCount == 1 ? " armament slot)\n" : " armament slots)\n")
                   .Append(ItemDescriber.Stats(arm.Stats));
            string shot = ItemDescriber.ShotSummary(arm.Shot);
            if (shot.Length > 0)
                builder.Append('\n').Append(shot);
            builder.Append("\n[");
            for (int i = 0; i < arm.SlotCount; i++)
            {
                builder.Append(i > 0 ? " | " : "");
                ArmamentData a = arm.GetArmament(i);
                builder.Append(a != null ? a.DisplayName : "-");
            }
            builder.Append(']');
        }

        private void DescribeBubble(ArmInstance arm, int index)
        {
            ArmamentData armament = arm.GetArmament(index);
            builder.Append("<b>").Append(arm.Data.DisplayName).Append(" - slot ").Append(index + 1).Append("</b>\n");
            if (armament == null)
            {
                builder.Append("Empty. Select it to choose an armament.");
                return;
            }
            foreach (string line in ItemDescriber.ArmamentLines(armament))
                builder.Append(line).Append('\n');
            builder.Append("Select to replace it, or remove it to return it to the inventory.");
        }

        private void DescribeItem(int index)
        {
            if (tabs.Active == ArmoryTabs.Arms && index < spares.Count)
            {
                foreach (string line in ShopDescriber.ArmLines(spares[index].Data, false))
                    builder.Append(line).Append('\n');
                if (spares[index].ArmamentCount > 0)
                    builder.Append("Carries ").Append(spares[index].ArmamentCount).Append(" armament(s).");
            }
            else if (tabs.Active == ArmoryTabs.Armaments && index < stacks.Count)
            {
                foreach (string line in ItemDescriber.ArmamentLines(stacks[index].Armament))
                    builder.Append(line).Append('\n');
                builder.Append("You have ").Append(stacks[index].Count).Append('.');
            }
        }

        // The before -> after preview shown while an item is focused and a slot is chosen.
        private void DescribePick(int index)
        {
            if (PlacingArm)
            {
                if (tabs.Active != ArmoryTabs.Arms || index >= spares.Count)
                {
                    builder.Append("Slot ").Append(SlotNames[selectedSlot]).Append(" is empty: switch to the Arms tab (L1 / R1) to place a spare arm.");
                    return;
                }
                ArmInstance spare = spares[index];
                builder.Append("<b>Place ").Append(spare.Data.DisplayName).Append(" in slot ").Append(SlotNames[selectedSlot]).Append("</b>\n")
                       .Append(ItemDescriber.Stats(spare.Stats));
                if (spare.ArmamentCount > 0)
                    builder.Append("\nIt keeps its ").Append(spare.ArmamentCount).Append(" armament(s).");
                return;
            }

            ArmInstance arm = state.Loadout[selectedSlot];
            if (tabs.Active != ArmoryTabs.Armaments || index >= stacks.Count)
            {
                builder.Append("Arms are placed from an empty arm slot.");
                return;
            }

            ArmamentData armament = stacks[index].Armament;
            ArmoryChange change = ArmoryPreview.Equip(arm, bubbleIndex, armament);
            builder.Append("<b>").Append(armament.DisplayName).Append(" -> ").Append(arm.Data.DisplayName).Append(" slot ").Append(bubbleIndex + 1).Append("</b>\n");
            if (!change.CanEquip)
            {
                builder.Append("Cannot equip: ").Append(change.Reason).Append('.');
                return;
            }
            string stats = change.StatText;
            builder.Append(stats.Length > 0 ? stats : "No stat change").Append('\n');
            if (change.ShotBefore != change.ShotAfter)
                builder.Append("shot: ").Append(change.ShotBefore.Length > 0 ? change.ShotBefore : "plain").Append(" -> ")
                       .Append(change.ShotAfter.Length > 0 ? change.ShotAfter : "plain").Append('\n');
            if (change.Replaced != null)
                builder.Append("Replaces ").Append(change.Replaced.DisplayName).Append(" (returns to the inventory)\n");
            foreach (string line in ItemDescriber.ArmamentLines(armament))
                builder.Append(line).Append('\n');
        }

        private void Say(string text) => messageLabel.text = text;

        private void ApplyHints()
        {
            switch (level)
            {
                case Level.Ring:
                    PromptHint.Show(hintLabel, PromptHint.P(UiAction.Confirm, "Select arm"), PromptHint.P(UiAction.Remove, "Remove"));
                    break;
                case Level.Bubbles:
                    PromptHint.Show(hintLabel, PromptHint.P(UiAction.Confirm, "Choose armament"), PromptHint.P(UiAction.Remove, "Unequip"),
                        PromptHint.P(UiAction.Back, "Back"));
                    break;
                default:
                    PromptHint.Show(hintLabel, PromptHint.P(UiAction.Confirm, "Equip"), PromptHint.P(UiAction.Remove, "Remove"), PromptHint.P(UiAction.Back, "Back"));
                    break;
            }
        }

        // ---- navigation

        private void BuildNavigation()
        {
            if (ring == null || cards == null)
                return;

            // Ring: left/up = previous slot, right/down = next slot (the stick cycles around the ring).
            int n = ring.Slots.Count;
            for (int i = 0; i < n; i++)
            {
                Selectable prev = ring.Slots[(i + n - 1) % n].Button;
                Selectable next = ring.Slots[(i + 1) % n].Button;
                ring.Slots[i].Button.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = prev, selectOnUp = prev, selectOnRight = next, selectOnDown = next };
            }

            // The way from the ring to the right half (tabs, cards, Fight, Remove, Main Menu): Right from a slot on the right side of the ring (NE, E, SE)
            // crosses to the inventory; Left from the first column or the tabs comes back to the east slot. Up, Down and Left still cycle the ring.
            Selectable armsTabEntry = tabs.ArmsButton;
            Selectable ringReturn = ring.Slots[Mathf.Min(2, n - 1)].Button;
            for (int i = 1; i <= 3 && i < n; i++)
            {
                Navigation nav = ring.Slots[i].Button.navigation;
                nav.selectOnRight = tabs.Active == ArmoryTabs.Arms ? armsTabEntry : tabs.ArmamentsButton;
                ring.Slots[i].Button.navigation = nav;
            }

            // Inventory: tabs on top, a 3-column grid below, then Fight.
            Selectable armsTab = tabs.ArmsButton;
            Selectable armamentsTab = tabs.ArmamentsButton;
            var active = new List<Selectable>();
            foreach (ShopCard c in cards)
                if (c.gameObject.activeSelf)
                    active.Add(c.Button);

            armsTab.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = ringReturn, selectOnRight = armamentsTab, selectOnDown = active.Count > 0 ? active[0] : (Selectable)fightButton };
            armamentsTab.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = armsTab, selectOnDown = active.Count > 0 ? active[Mathf.Min(1, active.Count - 1)] : (Selectable)fightButton };
            Selectable tabForUp = tabs.Active == ArmoryTabs.Arms ? armsTab : armamentsTab;

            for (int i = 0; i < active.Count; i++)
            {
                int col = i % GridColumns;
                Selectable left = col > 0 ? active[i - 1] : ringReturn;
                Selectable right = col < GridColumns - 1 && i + 1 < active.Count ? active[i + 1] : null;
                Selectable up = i - GridColumns >= 0 ? active[i - GridColumns] : tabForUp;
                Selectable down = i + GridColumns < active.Count ? active[i + GridColumns] : (Selectable)fightButton;
                active[i].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = left, selectOnRight = right, selectOnUp = up, selectOnDown = down };
            }
            fightButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = active.Count > 0 ? active[active.Count - 1] : tabForUp,
                selectOnLeft = removeButton.gameObject.activeSelf ? (Selectable)removeButton : menuButton,
            };
            menuButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = fightButton, selectOnUp = ringReturn };
            removeButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = fightButton, selectOnUp = ringReturn };
        }

        private void SetupBubbleNavigation(int count)
        {
            for (int i = 0; i < count; i++)
            {
                bubbles[i].Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = i > 0 ? bubbles[i - 1].Button : null,
                    selectOnRight = i < count - 1 ? bubbles[i + 1].Button : null,
                };
            }
        }
    }
}
