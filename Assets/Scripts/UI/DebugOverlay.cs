using System.Text;
using BulletHell.Input;
using BulletHell.Pickups;
using BulletHell.Player;
using BulletHell.Projectiles;
using BulletHell.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// On-screen readout of move stick, aim magnitude/angle, selected arm (slot and name), selection state and arm aim.
    /// Rebuilds its text only when a displayed value changes, so it doesn't allocate every frame.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        private static readonly string[] ArmNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        [SerializeField] private GameplayInputReader input;
        [SerializeField] private ArmSelectionController arms;
        [SerializeField] private ProjectilePool projectiles;
        [SerializeField] private AmmoSlots ammoSlots;
        [SerializeField] private ArmFireController fireController;
        [SerializeField] private AmmoPickupCollector pickupCollector;
        [SerializeField] private DebugArmamentControls armamentControls;
        [SerializeField] private Text label;

        private readonly StringBuilder builder = new StringBuilder(400);
        private static readonly string[] StateNames = { "none", "soft", "locked" };

        private int shownMoveX = int.MinValue, shownMoveY, shownMagnitude, shownAngle, shownArm, shownState, shownArmAim;
        private int shownActive = -1, shownPooled = -1, shownCreated = -1, shownHits = -1;

        // Heat is stored as percent, -1 = no arm in that slot, 101 = overheated. Ammo/pickup state is compared as a signature.
        private readonly int[] shownHeat = new int[ArmLoadout.SlotCount];
        private readonly int[] heatNow = new int[ArmLoadout.SlotCount];
        private int shownAmmoActive = -2, shownPickupProgress = -1, shownPickupSlot = -2;
        private readonly AmmoTypeData[] shownAmmo = new AmmoTypeData[AmmoSlotSet.Count];
        private AmmoPickup shownPickup;
        private ArmInstance shownInstance;
        private int shownInstanceVersion = -1;
        private ArmamentData shownTestArmament;
        private int shownTestPosition = -1, shownArmamentInvVersion = -1, shownArmInvVersion = -1;

        private void Update()
        {
            Vector2 move = input.Move;
            Vector2 aim = arms.AimStick;
            int moveX = Mathf.RoundToInt(move.x * 100f);
            int moveY = Mathf.RoundToInt(move.y * 100f);
            int magnitude = Mathf.RoundToInt(aim.magnitude * 100f);
            int angle = magnitude > 0 ? Mathf.RoundToInt(ArmSelector.CompassAngle(aim)) % 360 : -1;
            int arm = arms.SelectedArm;
            int state = (int)arms.State;
            int armAim = arm == ArmSelector.None ? -1 : Mathf.RoundToInt(arms.AimAngle) % 360;

            int active = projectiles.CountActive;
            int pooled = projectiles.CountInactive;
            int created = projectiles.TotalCreated;
            int hits = projectiles.TotalHits;

            bool ammoChanged = ammoSlots.ActiveIndex != shownAmmoActive;
            for (int i = 0; i < AmmoSlotSet.Count; i++)
                ammoChanged |= ammoSlots.Get(i) != shownAmmo[i];

            bool heatChanged = false;
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                HeatComponent slotHeat = fireController.GetHeat(i);
                heatNow[i] = arms.GetArm(i) == null ? -1 : slotHeat.IsOverheated ? 101 : Mathf.RoundToInt(slotHeat.Heat01 * 100f);
                heatChanged |= heatNow[i] != shownHeat[i];
            }

            AmmoPickup nearPickup = pickupCollector.NearPickup;
            int pickupSlot = pickupCollector.HoldSlot;
            int pickupProgress = Mathf.RoundToInt(pickupCollector.HoldProgress01 * 100f);
            bool pickupChanged = nearPickup != shownPickup || pickupSlot != shownPickupSlot || pickupProgress != shownPickupProgress;

            ArmInstance instance = arms.SelectedInstance;
            ArmamentData testArmament = armamentControls.Current;
            int testPosition = armamentControls.CurrentPosition;
            PlayerInventory playerInventory = armamentControls.Inventory;
            int armamentInvVersion = playerInventory.Armaments.Version;
            int armInvVersion = playerInventory.Arms.Version;
            bool armamentsChanged = instance != shownInstance || testArmament != shownTestArmament || testPosition != shownTestPosition ||
                                   armamentInvVersion != shownArmamentInvVersion || armInvVersion != shownArmInvVersion ||
                                   (instance != null && instance.Version != shownInstanceVersion);

            if (moveX == shownMoveX && moveY == shownMoveY && magnitude == shownMagnitude &&
                angle == shownAngle && arm == shownArm && state == shownState && armAim == shownArmAim &&
                active == shownActive && pooled == shownPooled && created == shownCreated && hits == shownHits &&
                !ammoChanged && !heatChanged && !pickupChanged && !armamentsChanged)
                return;

            shownInstance = instance;
            shownInstanceVersion = instance != null ? instance.Version : -1;
            shownTestArmament = testArmament;
            shownTestPosition = testPosition;
            shownArmamentInvVersion = armamentInvVersion;
            shownArmInvVersion = armInvVersion;

            shownAmmoActive = ammoSlots.ActiveIndex;
            for (int i = 0; i < AmmoSlotSet.Count; i++)
                shownAmmo[i] = ammoSlots.Get(i);
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
                shownHeat[i] = heatNow[i];
            shownPickup = nearPickup;
            shownPickupSlot = pickupSlot;
            shownPickupProgress = pickupProgress;

            shownActive = active;
            shownPooled = pooled;
            shownCreated = created;
            shownHits = hits;

            shownMoveX = moveX;
            shownMoveY = moveY;
            shownMagnitude = magnitude;
            shownAngle = angle;
            shownArm = arm;
            shownState = state;
            shownArmAim = armAim;

            builder.Clear();
            builder.Append("MOVE   (").Append((moveX / 100f).ToString("0.00")).Append(", ")
                   .Append((moveY / 100f).ToString("0.00")).Append(")\n");
            builder.Append("AIM    mag ").Append((magnitude / 100f).ToString("0.00"));
            builder.Append("   angle ").Append(angle < 0 ? "--" : angle.ToString()).Append("°\n");
            builder.Append("ARM    ");
            if (arm == ArmSelector.None)
                builder.Append("none");
            else
                builder.Append(ArmNames[arm]).Append("  ").Append(arms.SelectedArmData.DisplayName);
            builder.Append('\n');
            builder.Append("STATE  ").Append(StateNames[state]).Append('\n');
            builder.Append("ARM AIM ").Append(armAim < 0 ? "--" : armAim.ToString()).Append("°\n");
            builder.Append("BULLETS active ").Append(active).Append("   pooled ").Append(pooled)
                   .Append("   created ").Append(created).Append('\n');
            builder.Append("HITS   ").Append(hits).Append('\n');

            builder.Append("AMMO  ");
            for (int i = 0; i < AmmoSlotSet.Count; i++)
            {
                AmmoTypeData slotAmmo = ammoSlots.Get(i);
                bool isActive = i == ammoSlots.ActiveIndex;
                builder.Append(isActive ? " [" : "  ").Append(i + 1).Append(' ')
                       .Append(slotAmmo != null ? slotAmmo.DisplayName : "-").Append(isActive ? "]" : "");
            }
            builder.Append('\n');

            builder.Append("HEAT  ");
            int listed = 0;
            for (int i = 0; i < ArmLoadout.SlotCount; i++)
            {
                if (heatNow[i] < 0)
                    continue;
                if (listed > 0 && listed % 4 == 0)
                    builder.Append("\n      ");
                builder.Append("  ").Append(ArmNames[i]).Append(' ');
                if (heatNow[i] > 100)
                    builder.Append("OVERHEAT");
                else
                    builder.Append(heatNow[i]).Append('%');
                listed++;
            }
            builder.Append('\n');

            builder.Append("ARMAMENTS ");
            if (instance == null)
            {
                builder.Append("--");
            }
            else
            {
                for (int i = 0; i < ArmInstance.ArmamentSlots; i++)
                {
                    ArmamentData armament = instance.GetArmament(i);
                    builder.Append(" [").Append(i + 1).Append(' ').Append(armament != null ? armament.DisplayName : "-").Append(']');
                }
            }
            builder.Append("\n         equip: ").Append(testArmament != null ? testArmament.DisplayName : "--");
            if (testPosition > 0)
                builder.Append(" (").Append(testPosition).Append('/').Append(playerInventory.Armaments.Count).Append(')');
            builder.Append("   (D-pad up equip, down unequip, left/right pick | F1-F4)\n");
            builder.Append("INVENTORY armaments ").Append(playerInventory.Armaments.Count)
                   .Append("   spare arms ").Append(playerInventory.Arms.Count).Append('\n');

            builder.Append("EFFECTS ");
            if (instance == null || instance.Effects.Count == 0)
            {
                builder.Append("--");
            }
            else
            {
                for (int i = 0; i < instance.Effects.Count; i++)
                    builder.Append(i > 0 ? ", " : "").Append(instance.Effects[i].DisplayName);
            }
            builder.Append('\n');

            builder.Append("STATS  ");
            if (instance == null)
            {
                builder.Append("--");
            }
            else
            {
                ArmStats baseStats = instance.BaseStats;
                ArmStats final = instance.Stats;
                AppendStat("dmg", baseStats.Damage, final.Damage);
                AppendStat("rate", baseStats.FireRate, final.FireRate);
                AppendStat("speed", baseStats.ProjectileSpeed, final.ProjectileSpeed);
                AppendStat("proj", baseStats.ProjectilesPerShot, final.ProjectilesPerShot);
                AppendStat("spread", baseStats.Spread, final.Spread);
            }
            builder.Append('\n');

            builder.Append("PICKUP ");
            if (nearPickup == null)
                builder.Append("--");
            else if (pickupSlot < 0)
                builder.Append(nearPickup.Ammo.DisplayName).Append(": hold an ammo button (1-4) to swap that slot");
            else
                builder.Append(nearPickup.Ammo.DisplayName).Append(" -> slot ").Append(pickupSlot + 1)
                       .Append("  ").Append(pickupProgress).Append('%');
            label.text = builder.ToString();
        }

        /// <summary>"name base>final" when modified, "name value" otherwise.</summary>
        private void AppendStat(string name, float baseValue, float finalValue)
        {
            builder.Append(name).Append(' ');
            if (Mathf.Approximately(baseValue, finalValue))
                builder.Append(finalValue.ToString("0.##"));
            else
                builder.Append(baseValue.ToString("0.##")).Append(">").Append(finalValue.ToString("0.##"));
            builder.Append("  ");
        }
    }
}
