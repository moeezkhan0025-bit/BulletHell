using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>
    /// Damped spring toward zero (semi-implicit Euler, sub-stepped so a big dt can't blow it up). Kick it with an
    /// impulse (Velocity) or an offset (Value) and it settles by itself. Pure maths, no Unity objects.
    /// </summary>
    public struct Spring
    {
        public float Value;
        public float Velocity;

        private const float MaxStep = 1f / 120f;

        public void Step(float dt, float frequency, float damping)
        {
            float omega = frequency * 2f * Mathf.PI;
            while (dt > 0f)
            {
                float h = Mathf.Min(dt, MaxStep);
                Velocity += (-omega * omega * Value - 2f * damping * omega * Velocity) * h;
                Value += Velocity * h;
                dt -= h;
            }
        }

        public bool IsAtRest => Mathf.Abs(Value) < 0.0005f && Mathf.Abs(Velocity) < 0.0005f;

        public void Clear()
        {
            Value = 0f;
            Velocity = 0f;
        }
    }

    /// <summary>Two springs for a 2D offset.</summary>
    public struct Spring2
    {
        public Spring X;
        public Spring Y;

        public Vector2 Value => new Vector2(X.Value, Y.Value);
        public bool IsAtRest => X.IsAtRest && Y.IsAtRest;

        public void Step(float dt, float frequency, float damping)
        {
            X.Step(dt, frequency, damping);
            Y.Step(dt, frequency, damping);
        }

        public void Kick(Vector2 velocity)
        {
            X.Velocity += velocity.x;
            Y.Velocity += velocity.y;
        }

        public void Clear()
        {
            X.Clear();
            Y.Clear();
        }
    }
}
