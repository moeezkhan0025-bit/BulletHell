using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// Debug-only: adds and removes upgrades on the selected arm during play. Next/Prev pick which test upgrade Add uses,
    /// Add fills the first empty upgrade slot, Remove empties the last filled one. Does nothing with no arm selected.
    /// </summary>
    public sealed class DebugUpgradeControls : MonoBehaviour
    {
        [SerializeField] private GameplayInputReader input;
        [SerializeField] private ArmSelectionController arms;
        [Tooltip("Upgrades that can be added while playing.")]
        [SerializeField] private UpgradeData[] testUpgrades;

        private int index;

        /// <summary>The upgrade Add will use, or null if none are set up.</summary>
        public UpgradeData Current => testUpgrades != null && testUpgrades.Length > 0 ? testUpgrades[index] : null;

        private void OnEnable()
        {
            input.DebugAddUpgradePressed += Add;
            input.DebugRemoveUpgradePressed += Remove;
            input.DebugNextUpgradePressed += Next;
            input.DebugPrevUpgradePressed += Prev;
        }

        private void OnDisable()
        {
            input.DebugAddUpgradePressed -= Add;
            input.DebugRemoveUpgradePressed -= Remove;
            input.DebugNextUpgradePressed -= Next;
            input.DebugPrevUpgradePressed -= Prev;
        }

        private void Add() => arms.SelectedInstance?.TryAdd(Current);

        private void Remove() => arms.SelectedInstance?.RemoveLast();

        private void Next() => Step(1);

        private void Prev() => Step(-1);

        private void Step(int direction)
        {
            if (testUpgrades == null || testUpgrades.Length == 0)
                return;
            index = (index + direction + testUpgrades.Length) % testUpgrades.Length;
        }
    }
}
