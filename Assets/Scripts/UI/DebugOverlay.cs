using System.Text;
using BulletHell.Input;
using BulletHell.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BulletHell.UI
{
    /// <summary>
    /// On-screen readout of move stick, aim magnitude/angle, selected arm and lock state.
    /// Rebuilds its text only when a displayed value changes, so it doesn't allocate every frame.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        private static readonly string[] ArmNames = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        [SerializeField] private GameplayInputReader input;
        [SerializeField] private ArmSelectionController arms;
        [SerializeField] private Text label;

        private readonly StringBuilder builder = new StringBuilder(160);
        private int shownMoveX = int.MinValue, shownMoveY, shownMagnitude, shownAngle, shownArm, shownLocked;

        private void Update()
        {
            Vector2 move = input.Move;
            Vector2 aim = arms.AimStick;
            int moveX = Mathf.RoundToInt(move.x * 100f);
            int moveY = Mathf.RoundToInt(move.y * 100f);
            int magnitude = Mathf.RoundToInt(aim.magnitude * 100f);
            int angle = magnitude > 0 ? Mathf.RoundToInt(ArmSelector.CompassAngle(aim)) % 360 : -1;
            int arm = arms.SelectedArm;
            int locked = arms.IsLocked ? 1 : 0;

            if (moveX == shownMoveX && moveY == shownMoveY && magnitude == shownMagnitude &&
                angle == shownAngle && arm == shownArm && locked == shownLocked)
                return;

            shownMoveX = moveX;
            shownMoveY = moveY;
            shownMagnitude = magnitude;
            shownAngle = angle;
            shownArm = arm;
            shownLocked = locked;

            builder.Clear();
            builder.Append("MOVE   (").Append((moveX / 100f).ToString("0.00")).Append(", ")
                   .Append((moveY / 100f).ToString("0.00")).Append(")\n");
            builder.Append("AIM    mag ").Append((magnitude / 100f).ToString("0.00"));
            builder.Append("   angle ").Append(angle < 0 ? "--" : angle.ToString()).Append("°\n");
            builder.Append("ARM    ").Append(arm == ArmSelector.None ? "none" : ArmNames[arm]).Append('\n');
            builder.Append("LOCKED ").Append(locked == 1 ? "YES" : "no");
            label.text = builder.ToString();
        }
    }
}
