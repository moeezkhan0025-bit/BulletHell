using BulletHell.Input;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Player
{
    public enum ArmSelectionState { None, Soft, Locked }

    /// <summary>
    /// Pure selection logic for the 8 weapon arm slots. Slot 0 = N, then clockwise in 45 degree steps.
    /// The stick picks the nearest owned arm, so owned arms split the circle like a pie
    /// (one arm = the whole circle). Knows nothing about input devices; feed it a stick vector each frame.
    /// </summary>
    public sealed class ArmSelector
    {
        public const int ArmCount = 8;
        public const int None = -1;
        public const float SliceDegrees = 360f / ArmCount;

        private readonly InputTuning tuning;
        private readonly bool[] owned = new bool[ArmCount];

        /// <summary>Player aim sensitivity setting (1 = the InputTuning values as authored).</summary>
        public float Sensitivity { get; set; } = 1f;

        public int Selected { get; private set; } = None;
        public bool Locked { get; private set; }

        /// <summary>Compass direction the selected arm points: its home slot when soft, the free aim when locked.</summary>
        public float AimAngle { get; private set; }

        public ArmSelectionState State =>
            Selected == None ? ArmSelectionState.None : Locked ? ArmSelectionState.Locked : ArmSelectionState.Soft;

        public ArmSelector(InputTuning tuning) => this.tuning = tuning;

        public bool IsOwned(int arm) => owned[arm];

        public void SetOwned(int arm, bool isOwned)
        {
            owned[arm] = isOwned;
            if (!isOwned && Selected == arm)
            {
                Selected = None;
                Locked = false;
            }
        }

        /// <summary>Owns exactly the loadout's filled slots.</summary>
        public void SetOwnedFromLoadout(ArmLoadout loadout)
        {
            for (int i = 0; i < ArmCount; i++)
                SetOwned(i, loadout.IsFilled(i));
        }

        public static float HomeAngle(int arm) => arm * SliceDegrees;

        /// <summary>Compass angle of a vector: 0 = up (N), increasing clockwise, in [0, 360).</summary>
        public static float CompassAngle(Vector2 v)
        {
            float angle = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
            return angle < 0f ? angle + 360f : angle;
        }

        /// <summary>The owned arm whose home slot is closest to the angle, or None if no arms are owned.</summary>
        public int NearestOwnedArm(float compassAngle)
        {
            int nearest = None;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < ArmCount; i++)
            {
                if (!owned[i])
                    continue;
                float distance = AngleTo(compassAngle, i);
                if (distance < nearestDistance)
                {
                    nearest = i;
                    nearestDistance = distance;
                }
            }
            return nearest;
        }

        /// <summary>
        /// Advances selection (soft) or aim (locked) for this frame's stick.
        /// Returns true if the selected arm or its aim changed.
        /// </summary>
        public bool Update(Vector2 stick)
        {
            if (Locked)
            {
                if (stick.magnitude < AimThresholds.LockedDeadzone(tuning, Sensitivity))
                    return false;

                float angle = CompassAngle(stick);
                if (Mathf.Approximately(angle, AimAngle))
                    return false;

                AimAngle = angle;
                return true;
            }

            return UpdateSoft(stick);
        }

        /// <summary>
        /// Locks the soft-selected arm (aim snaps to the stick), or unlocks it (arm returns home and
        /// soft select re-evaluates the stick). Does nothing with no arm selected. Returns true if lock changed.
        /// </summary>
        public bool ToggleLock(Vector2 stick)
        {
            if (Selected == None)
                return false;

            if (!Locked)
            {
                Locked = true;
                if (stick.magnitude >= AimThresholds.LockedDeadzone(tuning, Sensitivity))
                    AimAngle = CompassAngle(stick);
                return true;
            }

            Locked = false;
            Select(None);
            UpdateSoft(stick);
            return true;
        }

        private bool UpdateSoft(Vector2 stick)
        {
            float magnitude = stick.magnitude;
            int next = Selected;

            if (Selected == None)
            {
                if (magnitude >= AimThresholds.Select(tuning, Sensitivity))
                    next = NearestOwnedArm(CompassAngle(stick));
            }
            else if (magnitude < AimThresholds.Deselect(tuning, Sensitivity))
            {
                next = None;
            }
            else
            {
                // Held: only switch once the stick is clearly past the boundary between the two arms.
                // The boundary sits halfway between them, so being h degrees past it means the
                // distances differ by 2h.
                float angle = CompassAngle(stick);
                int candidate = NearestOwnedArm(angle);
                if (candidate != Selected &&
                    AngleTo(angle, Selected) - AngleTo(angle, candidate) > 2f * tuning.AngleHysteresisDegrees)
                    next = candidate;
            }

            if (next == Selected)
                return false;

            Select(next);
            return true;
        }

        private void Select(int arm)
        {
            Selected = arm;
            AimAngle = arm == None ? 0f : HomeAngle(arm);
        }

        private static float AngleTo(float compassAngle, int arm) =>
            Mathf.Abs(Mathf.DeltaAngle(compassAngle, HomeAngle(arm)));
    }
}
