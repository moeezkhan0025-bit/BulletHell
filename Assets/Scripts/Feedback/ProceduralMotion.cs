using BulletHell.Player;
using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>
    /// The single writer of a character's Motion transform (the child that holds its body sprites): idle breathing,
    /// hop-walk bob and tilt, lean into movement, squash/stretch on start/stop, facing flip, plus the one-shot layers other
    /// feedback pushes into it (scale punch, visual knockback nudge, windup inflate/tremble, spawn pop-in). Because it is the
    /// only writer, nothing fights over the transform; the parent (JumpController's Visuals, the enemy Rig) keeps its own job.
    /// Velocity comes from the root's position change, so it needs no hooks in the mover or the AI. Runs on game time:
    /// it holds still during a hitstop and pause. Values come from a MotionTuning asset.
    /// </summary>
    public sealed class ProceduralMotion : MonoBehaviour
    {
        [Tooltip("The transform that is animated (holds the body sprites).")]
        [SerializeField] private Transform motion;
        [SerializeField] private MotionTuning tuning;
        [Tooltip("Player only: no hop-walk bob while airborne. Optional.")]
        [SerializeField] private JumpController jump;

        private Spring squash;
        private Spring punch;
        private Spring2 knock;
        private Vector3 lastRoot;
        private bool hasLast;
        private bool wasMoving;
        private float hopPhase;
        private float lean;
        private float flip = 1f;
        private float facing = 1f;
        private float breathPhase;
        private float windup;
        private float trembleClock;
        private LifeCycleTuning spawn;
        private float spawnTime;

        public Transform Motion => motion;

        public void SetTuning(MotionTuning motionTuning) => tuning = motionTuning;

        /// <summary>Fresh start (spawn from the pool, round start): forgets all motion state.</summary>
        public void ResetState()
        {
            squash.Clear();
            punch.Clear();
            knock.Clear();
            hasLast = false;
            wasMoving = false;
            hopPhase = 0f;
            lean = 0f;
            windup = 0f;
            spawn = null;
            flip = facing;
            breathPhase = Random.value * Mathf.PI * 2f;
            Write(Vector3.zero, Vector3.one, 0f);
        }

        /// <summary>Uniform scale kick: swells by about `amount` (0.15 = 15%) and settles.</summary>
        public void Punch(float amount) => punch.Velocity += amount * Omega;

        /// <summary>Pushes the visual away along a world direction by about `distance`, then it springs back. Never moves the collider.</summary>
        public void Knock(Vector2 worldDirection, float distance)
        {
            if (distance <= 0f || worldDirection == Vector2.zero)
                return;
            knock.Kick(worldDirection.normalized * (distance * Omega));
        }

        /// <summary>Squash (positive) or stretch (negative) by about `amount` of scale.</summary>
        public void Squash(float amount) => squash.Velocity += amount * Omega;

        /// <summary>Windup 0..1: inflates and trembles. 0 = off.</summary>
        public void SetWindup(float progress01) => windup = Mathf.Clamp01(progress01);

        /// <summary>Pops in from nothing with the life-cycle's spawn curve.</summary>
        public void PlaySpawn(LifeCycleTuning lifeCycle)
        {
            spawn = lifeCycle;
            spawnTime = 0f;
        }

        private float Omega => (tuning != null ? tuning.SpringFrequency : 3f) * 2f * Mathf.PI;

        private void OnEnable() => ResetState();

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (tuning == null || motion == null || dt <= 0f)
                return;

            // Velocity from how far the root moved this frame.
            Vector3 root = transform.position;
            Vector2 velocity = hasLast ? (Vector2)(root - lastRoot) / dt : Vector2.zero;
            lastRoot = root;
            hasLast = true;

            float speed = velocity.magnitude;
            bool airborne = jump != null && jump.IsAirborne;
            float move01 = Mathf.Clamp01(speed / tuning.FullSpeed);
            bool moving = speed > tuning.MoveThreshold;

            if (moving != wasMoving)
            {
                Squash(moving ? -tuning.StartStretch : tuning.StopSquash);
                wasMoving = moving;
            }

            squash.Step(dt, tuning.SpringFrequency, tuning.SpringDamping);
            punch.Step(dt, tuning.SpringFrequency, tuning.SpringDamping);
            knock.Step(dt, tuning.SpringFrequency * 1.3f, 0.5f);

            // Breathing fades out while moving.
            breathPhase += dt * tuning.BreathSpeed;
            float breath = Mathf.Sin(breathPhase) * tuning.BreathAmount * (1f - move01);

            // Hop-walk: two bobs per cycle, tilt rocks left/right.
            float bob = 0f;
            float tilt = 0f;
            if (moving && !airborne)
            {
                float before = hopPhase;
                hopPhase += dt * tuning.HopsPerSecond * move01;
                if (tuning.FootDust && Mathf.FloorToInt(hopPhase) != Mathf.FloorToInt(before))
                    FeedbackHub.Play(VfxKind.Dust, root, 3);
                bob = Mathf.Abs(Mathf.Sin(hopPhase * Mathf.PI)) * tuning.HopHeight * move01;
                tilt = Mathf.Sin(hopPhase * Mathf.PI) * tuning.TiltDegrees * move01;
            }

            // Lean into the horizontal direction of travel.
            float leanTarget = Mathf.Clamp(velocity.x / tuning.FullSpeed, -1f, 1f) * -tuning.LeanDegrees;
            lean = Mathf.Lerp(lean, leanTarget, 1f - Mathf.Exp(-tuning.LeanFollow * dt));

            // Facing flip: mirror through a quick squash.
            if (tuning.FlipToFace)
            {
                if (Mathf.Abs(velocity.x) > tuning.MoveThreshold)
                    facing = Mathf.Sign(velocity.x);
                flip = Mathf.MoveTowards(flip, facing, dt * 2f / tuning.FlipSeconds);
            }
            else
            {
                flip = 1f;
            }

            float sx = 1f - breath * 0.5f - squash.Value * 0.5f;
            float sy = 1f + breath + squash.Value;
            float uniform = 1f + punch.Value + windup * tuning.WindupInflate;

            if (spawn != null)
            {
                spawnTime += dt;
                uniform *= spawn.SpawnScaleAt(spawnTime / spawn.SpawnSeconds);
                if (spawnTime >= spawn.SpawnSeconds)
                    spawn = null;
            }

            Vector2 offset = new Vector2(0f, bob);
            if (windup > 0f)
            {
                trembleClock += dt * tuning.TrembleSpeed;
                offset.x += Mathf.Sin(trembleClock) * tuning.WindupTremble * windup;
            }
            Vector2 knockLocal = motion.parent != null
                ? (Vector2)motion.parent.InverseTransformVector(knock.Value)
                : knock.Value;
            offset += knockLocal;

            Write(offset, new Vector3(sx * uniform * flip, sy * uniform, 1f), tilt + lean);
        }

        private void Write(Vector3 localOffset, Vector3 scale, float zDegrees)
        {
            if (motion == null)
                return;
            motion.localPosition = localOffset;
            motion.localScale = scale;
            motion.localRotation = Quaternion.Euler(0f, 0f, zDegrees);
        }
    }
}
