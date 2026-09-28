using System;
using System.Collections.Generic;
using System.Text;
using BulletHell.Core;
using BulletHell.UI;
using BulletHell.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.Armory
{
    /// <summary>
    /// Skeleton Armory. One list at a time, so it works with a stick / D-pad and A / B only:
    ///   Slots        the 8 arm slots. An arm opens its menu, an empty slot opens the spare-arm picker.
    ///   Arm          the arm's 3 armament slots, and "Remove arm".
    ///   Armament     equip an armament from the inventory into the chosen slot, or remove the current one.
    ///   ArmPicker    place a spare arm in an empty slot.
    /// B / Circle goes back one list. Changes apply to the RunState immediately; the player's arms are rebuilt when
    /// the next round starts, and the run is saved when the Armory is left.
    /// </summary>
    public sealed class ArmoryScreen : MonoBehaviour
    {
        private static readonly string[] SlotNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        private enum Mode { Slots, Arm, Armament, ArmPicker }

        [SerializeField] private Text title;
        [SerializeField] private Text info;
        [SerializeField] private UIList list;
        [SerializeField] private GameObject footer;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button menuButton;

        private readonly StringBuilder builder = new StringBuilder(200);
        private readonly List<ArmamentData> distinct = new List<ArmamentData>();
        private readonly List<int> distinctCounts = new List<int>();
        private RunState state;
        private Mode mode;
        private int armSlot;
        private int armamentSlot;
        private string message = "";

        public event Action ContinuePressed;
        public event Action MenuPressed;

        private void Awake()
        {
            continueButton.onClick.AddListener(() => ContinuePressed?.Invoke());
            menuButton.onClick.AddListener(() => MenuPressed?.Invoke());
        }

        public void Show(RunState runState)
        {
            state = runState;
            mode = Mode.Slots;
            message = "";
            gameObject.SetActive(true);
            Refresh(0);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Refresh(int focus)
        {
            footer.SetActive(mode == Mode.Slots);
            list.Begin();
            switch (mode)
            {
                case Mode.Slots: BuildSlots(); break;
                case Mode.Arm: BuildArm(); break;
                case Mode.Armament: BuildArmament(); break;
                case Mode.ArmPicker: BuildArmPicker(); break;
            }
            list.End();
            list.Focus(focus);
        }

        // ---------------------------------------------------------------- Slots

        private void BuildSlots()
        {
            title.text = $"Armory - round {state.Round}";
            SetInfo("Select an arm to edit its armaments, or an empty slot to place a spare arm.");
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                int slot = i;
                ArmInstance arm = state.Loadout[i];
                string text = arm != null ? ItemDescriber.ArmWithArmaments(arm) : "(empty)";
                list.Add($"{SlotNames[i],-3} {text}", () => { if (arm != null) OpenArm(slot); else OpenArmPicker(slot); });
            }
        }

        // ---------------------------------------------------------------- Arm

        private void OpenArm(int slot)
        {
            armSlot = slot;
            mode = Mode.Arm;
            message = "";
            Refresh(0);
        }

        private void BuildArm()
        {
            ArmInstance arm = state.Loadout[armSlot];
            title.text = $"Arm {SlotNames[armSlot]} - {arm.Data.DisplayName}";
            builder.Clear();
            builder.Append(ItemDescriber.Stats(arm.Stats));
            if (arm.Effects.Count > 0)
            {
                builder.Append("\nEffects: ");
                for (int i = 0; i < arm.Effects.Count; i++)
                    builder.Append(i > 0 ? ", " : "").Append(arm.Effects[i].DisplayName);
            }
            SetInfo(builder.ToString());

            Action back = () => BackToSlots(armSlot);
            for (int k = 0; k < ArmInstance.ArmamentSlots; k++)
            {
                int slot = k;
                list.Add($"Slot {k + 1}:  {ItemDescriber.Armament(arm.GetArmament(k))}", () => OpenArmament(slot), back);
            }
            list.Add("Remove this arm (it goes to the arm inventory with its armaments)", RemoveArm, back);
            list.Add("Back", back, back);
        }

        private void RemoveArm()
        {
            ArmInstance arm = state.Loadout[armSlot];
            string name = arm.Data.DisplayName;
            if (ArmoryActions.RemoveArm(state, armSlot) == ArmoryResult.LastArm)
            {
                message = "You must keep at least one arm equipped.";
                Refresh(ArmInstance.ArmamentSlots);
                return;
            }
            BackToSlots(armSlot, $"{name} moved to the arm inventory.");
        }

        // ---------------------------------------------------------------- Armament picker

        private void OpenArmament(int slot)
        {
            armamentSlot = slot;
            mode = Mode.Armament;
            message = "";
            Refresh(0);
        }

        private void BuildArmament()
        {
            ArmInstance arm = state.Loadout[armSlot];
            title.text = $"Armament for slot {armamentSlot + 1} - arm {SlotNames[armSlot]}";
            SetInfo(state.Armaments.Count == 0
                ? "No armaments in the inventory. Buy some in the Shop."
                : "Equipping replaces what is in the slot (it goes back to the inventory).");

            Action back = () => BackToArm(armamentSlot);
            ArmamentData current = arm.GetArmament(armamentSlot);
            if (current != null)
                list.Add($"Remove:  {ItemDescriber.Armament(current)}", UnequipCurrent, back);

            CollectDistinctArmaments();
            for (int i = 0; i < distinct.Count; i++)
            {
                ArmamentData armament = distinct[i];
                string count = distinctCounts[i] > 1 ? $"   x{distinctCounts[i]}" : "";
                list.Add($"{ItemDescriber.Armament(armament)}{count}", () => Equip(armament), back);
            }
            list.Add("Back", back, back);
        }

        private void CollectDistinctArmaments()
        {
            distinct.Clear();
            distinctCounts.Clear();
            for (int i = 0; i < state.Armaments.Count; i++)
            {
                ArmamentData armament = state.Armaments.Get(i);
                int found = distinct.IndexOf(armament);
                if (found < 0)
                {
                    distinct.Add(armament);
                    distinctCounts.Add(1);
                }
                else
                {
                    distinctCounts[found]++;
                }
            }
        }

        private void Equip(ArmamentData armament)
        {
            if (state.Loadout[armSlot].TryEquipAt(armamentSlot, armament, state.Armaments))
                BackToArm(armamentSlot);
        }

        private void UnequipCurrent()
        {
            state.Loadout[armSlot].Unequip(armamentSlot, state.Armaments);
            BackToArm(armamentSlot);
        }

        // ---------------------------------------------------------------- Spare arm picker

        private void OpenArmPicker(int slot)
        {
            armSlot = slot;
            mode = Mode.ArmPicker;
            message = "";
            Refresh(0);
        }

        private void BuildArmPicker()
        {
            title.text = $"Place an arm in slot {SlotNames[armSlot]}";
            SetInfo(state.SpareArms.Count == 0 ? "No spare arms. Buy some in the Shop." : "Choose a spare arm to place here.");

            Action back = () => BackToSlots(armSlot);
            for (int i = 0; i < state.SpareArms.Count; i++)
            {
                ArmInstance spare = state.SpareArms.Get(i);
                list.Add(ItemDescriber.ArmWithArmaments(spare), () => PlaceArm(spare), back);
            }
            list.Add("Back", back, back);
        }

        private void PlaceArm(ArmInstance spare)
        {
            string name = spare.Data.DisplayName;
            if (ArmoryActions.PlaceArm(state, armSlot, spare) == ArmoryResult.Done)
                BackToSlots(armSlot, $"{name} placed in {SlotNames[armSlot]}.");
        }

        // ---------------------------------------------------------------- Navigation

        private void BackToSlots(int focusSlot, string result = "")
        {
            mode = Mode.Slots;
            message = result;
            Refresh(focusSlot);
        }

        private void BackToArm(int focusSlot)
        {
            mode = Mode.Arm;
            message = "";
            Refresh(focusSlot);
        }

        private void SetInfo(string text)
        {
            builder.Clear();
            builder.Append(text);
            if (mode == Mode.Slots)
                builder.Append("\nSpare arms: ").Append(state.SpareArms.Count)
                       .Append("     Armaments in inventory: ").Append(state.Armaments.Count);
            if (message.Length > 0)
                builder.Append('\n').Append(message);
            info.text = builder.ToString();
        }
    }
}
