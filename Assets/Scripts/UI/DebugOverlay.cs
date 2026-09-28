using System.Text;
using BulletHell.Input;
using BulletHell.Player;
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
        [SerializeField] private Text label;

        private readonly StringBuilder builder = new StringBuilder(160);
        private static readonly string[] StateNames = { "none", "soft", "locked" };

        private int shownMoveX = int.MinValue, shownMoveY, shownMagnitude, shownAngle, shownArm, shownState, shownArmAim;

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

            if (moveX == shownMoveX && moveY == shownMoveY && magnitude == shownMagnitude &&
                angle == shownAngle && arm == shownArm && state == shownState && armAim == shownArmAim)
                return;

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
            builder.Append("ARM AIM ").Append(armAim < 0 ? "--" : armAim.ToString()).Append('°');
            label.text = builder.ToString();
        }
    }
}
