using BulletHell.Input;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// Pure selection logic for the 8 weapon arms. Arm 0 = N, then clockwise in 45 degree steps.
    /// Knows nothing about input devices; feed it a stick vector each frame.
    /// </summary>
    public sealed class ArmSelector
    {
        public const int ArmCount = 8;
        public const int None = -1;
        public const float SliceDegrees = 360f / ArmCount;

        private readonly InputTuning tuning;
        private readonly bool[] owned = new bool[ArmCount];

        public int Selected { get; private set; } = None;
        public bool Locked { get; private set; }

        public ArmSelector(InputTuning tuning)
        {
            this.tuning = tuning;
            for (int i = 0; i < ArmCount; i++)
                owned[i] = true;
        }

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

        /// <summary>Compass angle of a vector: 0 = up (N), increasing clockwise, in [0, 360).</summary>
        public static float CompassAngle(Vector2 v)
        {
            float angle = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
            return angle < 0f ? angle + 360f : angle;
        }

        public static int NearestArm(float compassAngle) =>
            Mathf.RoundToInt(compassAngle / SliceDegrees) % ArmCount;

        /// <summary>Advances selection for this frame's stick. Returns true if the selected arm changed.</summary>
        public bool Update(Vector2 stick)
        {
            if (Locked)
                return false;

            float magnitude = stick.magnitude;
            int next = Selected;

            if (Selected == None)
            {
                if (magnitude >= tuning.SelectThreshold)
                    next = OwnedOrNone(NearestArm(CompassAngle(stick)));
            }
            else if (magnitude < tuning.DeselectThreshold)
            {
                next = None;
            }
            else if (magnitude >= tuning.SelectThreshold)
            {
                // At the edge: only switch once the stick is clearly past the current slice's boundary.
                float angle = CompassAngle(stick);
                float offCenter = Mathf.Abs(Mathf.DeltaAngle(angle, Selected * SliceDegrees));
                if (offCenter > SliceDegrees * 0.5f + tuning.AngleHysteresisDegrees)
                    next = OwnedOrNone(NearestArm(angle));
            }

            if (next == Selected)
                return false;

            Selected = next;
            return true;
        }

        /// <summary>Toggles lock on the selected arm. Does nothing with no arm selected. Returns true if lock changed.</summary>
        public bool ToggleLock()
        {
            if (Selected == None)
                return false;

            Locked = !Locked;
            return true;
        }

        private int OwnedOrNone(int arm) => owned[arm] ? arm : None;
    }
}
