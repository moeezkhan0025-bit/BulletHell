using System.Collections.Generic;
using BulletHell.Projectiles;
using UnityEngine;

namespace BulletHell.Capture
{
    /// <summary>
    /// R1 portfolio capture kit - reads the enemy bullets in flight for the autopilot without touching the gameplay classes.
    /// The ProjectilePool parents every projectile under itself and switches inactive ones off, and a hostile bullet is the one
    /// whose "Fill" layer is on (Projectile.LaunchHostile), so the scanner walks the pool's children once per frame, estimates
    /// each hostile bullet's velocity from its position change, and fills reusable arrays: no allocation per frame, no
    /// FindObjectOfType. If the pool ever grows past its prewarm size the arrays grow with it (the pool logs a warning then too).
    /// </summary>
    public sealed class CaptureBulletScanner
    {
        private static readonly List<SpriteRenderer> Scratch = new List<SpriteRenderer>(8);

        private ProjectilePool pool;
        private Transform[] roots = new Transform[0];
        private SpriteRenderer[] fills = new SpriteRenderer[0];
        private Transform[] bodies = new Transform[0];
        private Vector2[] previous = new Vector2[0];
        private bool[] seen = new bool[0];
        private float speedSanity = 40f;

        /// <summary>Enemy bullets with a usable velocity this frame (valid up to Count).</summary>
        public Vector2[] Position { get; private set; } = new Vector2[0];
        public Vector2[] Velocity { get; private set; } = new Vector2[0];
        /// <summary>Hit radius of each bullet (half its drawn size).</summary>
        public float[] Radius { get; private set; } = new float[0];
        public int Count { get; private set; }

        public bool IsBound => pool != null;

        public void Bind(ProjectilePool projectilePool, float maxSpeedSanity)
        {
            pool = projectilePool;
            speedSanity = maxSpeedSanity;
            Count = 0;
            Rebuild();
        }

        public void Unbind()
        {
            pool = null;
            Count = 0;
        }

        private void Rebuild()
        {
            int n = pool.transform.childCount;
            roots = new Transform[n];
            fills = new SpriteRenderer[n];
            bodies = new Transform[n];
            previous = new Vector2[n];
            seen = new bool[n];
            Position = new Vector2[n];
            Velocity = new Vector2[n];
            Radius = new float[n];
            for (int i = 0; i < n; i++)
            {
                Transform child = pool.transform.GetChild(i);
                roots[i] = child;
                child.GetComponentsInChildren(true, Scratch);
                for (int s = 0; s < Scratch.Count; s++)
                {
                    if (Scratch[s].name != "Fill")
                        continue;
                    fills[i] = Scratch[s];
                    bodies[i] = Scratch[s].transform.parent;
                    break;
                }
            }
        }

        /// <summary>Refreshes the hostile bullet list. Call once per frame, before the bullets move (they move in their own Update).</summary>
        public void Update(float deltaTime)
        {
            Count = 0;
            if (pool == null)
                return;
            if (pool.transform.childCount != roots.Length)
                Rebuild();
            if (deltaTime <= 0f)
                return;

            float maxStepSqr = speedSanity * speedSanity * deltaTime * deltaTime;
            for (int i = 0; i < roots.Length; i++)
            {
                Transform root = roots[i];
                SpriteRenderer fill = fills[i];
                if (root == null || fill == null || !root.gameObject.activeSelf || !fill.enabled)
                {
                    seen[i] = false;
                    continue;
                }

                Vector2 now = root.position;
                if (seen[i])
                {
                    Vector2 step = now - previous[i];
                    if (step.sqrMagnitude <= maxStepSqr && step.sqrMagnitude > 0f)
                    {
                        Position[Count] = now;
                        Velocity[Count] = step / deltaTime;
                        Radius[Count] = bodies[i] != null ? bodies[i].localScale.x * 0.5f : 0.15f;
                        Count++;
                    }
                }
                previous[i] = now;
                seen[i] = true;
            }
        }
    }
}
