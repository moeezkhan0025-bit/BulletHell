#if UNITY_EDITOR || DEVELOPMENT_BUILD
using BulletHell.Bosses;
using BulletHell.Core;
using BulletHell.Enemies;
using BulletHell.Input;
using BulletHell.Player;
using BulletHell.Projectiles;
using BulletHell.Weapons;
using System.Collections.Generic;
using UnityEngine;

namespace BulletHell.Capture
{
    /// <summary>
    /// R1 portfolio capture kit - the capture autopilot. Plays the round for the camera: dodges enemy bullets, keeps a comfortable
    /// distance from the Chasers (and strafes so it never stands still), selects, locks and aims an arm at the nearest enemy, fires
    /// in bursts, swaps ammo now and then, and jumps over the Pumpking's smash on its landing-ring telegraph.
    ///
    /// It drives the SAME paths a controller does, by the gameplay input reader's development hooks, not by faking devices:
    ///   move stick   -> GameplayInputReader.DebugOverride / DebugMove (also used by the stress test)
    ///   left stick   -> DebugAimOverride / DebugAim; the ArmSelector then soft-selects and aims exactly as for a real stick
    ///   lock (L3)    -> DebugRaiseLockToggle(); jump (R2) -> DebugRaiseJump(); ammo (face buttons) -> DebugRaiseAmmo(slot)
    ///   fire (R1)    -> DebugFireOverride / DebugFire
    /// What it reads: the live enemies from ProjectilePool.Enemies (the navigation registry), enemy bullets through
    /// <see cref="CaptureBulletScanner"/> (the pool's pooled children, velocities from position change), the boss through
    /// BossEvents.Active + its BossJump (landing point and progress = the landing-ring telegraph), the arena bounds, and the arm heat.
    /// Human feel: decisions every few frames, a reaction delay line, a smooth stick (limited acceleration), aim turn rate,
    /// select-then-lock with a short hold, and the trigger released briefly now and then. Every number is in <see cref="CaptureTuning"/>.
    /// No per-frame allocation, no FindObjectOfType after Bind. Only compiled in the Editor and development builds.
    /// </summary>
    [DefaultExecutionOrder(-50)] // before the player components read their input this frame
    public sealed class CaptureAutopilot : MonoBehaviour
    {
        private const float StickSnapEpsilon = 0.001f;

        private CaptureTuning tuning;
        private GameplayInputReader input;
        private PlayerHealth player;
        private ArmSelectionController arms;
        private ArmFireController fire;
        private AmmoSlots ammo;
        private JumpController jump;
        private ProjectilePool pool;
        private RunManager run;
        private readonly CaptureBulletScanner bullets = new CaptureBulletScanner();
        private readonly CaptureAutopilotMath.DelayLine moveDelay = new CaptureAutopilotMath.DelayLine(48);

        private bool active;

        // movement
        private Vector2 stick;
        private float nextDecision;
        private float strafeSign = 1f;
        private float nextStrafeSwitch;
        private float tieBreak = 1f;
        private Vector2 stuckAnchor;
        private float stuckCheckAt;
        private float unstickUntil;

        // aiming
        private Enemy target;
        private float targetDistance = -1f;
        private Vector2 targetLastPosition;
        private Vector2 targetVelocity;
        private float nextRetarget;
        private float aimAngle;          // compass degrees the left stick points at
        private float softSince = -1f;
        private float lockedSince = -1f;
        private ArmSelectionState lastArmState = ArmSelectionState.None;

        // fire / ammo / jump
        private bool triggerOn = true;
        private float triggerSwitchAt;
        private float nextAmmoSwap;
        private float nextHop;
        private BossController cachedBoss;
        private BossJump cachedBossJump;

        public bool IsActive => active;

        /// <summary>Collects the scene objects once. Returns false (with a warning) when this is not the Game scene.</summary>
        public bool Bind(CaptureTuning autopilotTuning, GameplayInputReader reader, PlayerHealth playerHealth, ArmSelectionController armController,
                         ArmFireController fireController, AmmoSlots ammoSlots, JumpController jumpController, ProjectilePool projectiles)
        {
            tuning = autopilotTuning;
            input = reader;
            player = playerHealth;
            arms = armController;
            fire = fireController;
            ammo = ammoSlots;
            jump = jumpController;
            pool = projectiles;
            if (tuning == null || input == null || player == null || arms == null || fire == null || ammo == null || jump == null || pool == null)
            {
                Debug.LogWarning("CaptureAutopilot: a scene object is missing (is this the Game scene?). The autopilot stays off.");
                return false;
            }
            run = GameServices.Ensure().Run;
            bullets.Bind(pool, tuning.BulletSpeedSanity);
            return true;
        }

        public void Begin()
        {
            if (!Debug.isDebugBuild || tuning == null)
                return;
            active = true;
            float now = Time.time;
            stick = Vector2.zero;
            moveDelay.Clear();
            nextDecision = now;
            nextStrafeSwitch = now + Random.Range(tuning.StrafeSwitchSeconds.x, tuning.StrafeSwitchSeconds.y);
            strafeSign = Random.value < 0.5f ? -1f : 1f;
            tieBreak = Random.value < 0.5f ? -1f : 1f;
            stuckAnchor = player.FeetPosition;
            stuckCheckAt = now + tuning.StuckSeconds;
            unstickUntil = 0f;
            target = null;
            targetDistance = -1f;
            nextRetarget = now;
            softSince = lockedSince = -1f;
            triggerOn = true;
            triggerSwitchAt = now + Random.Range(tuning.FireHoldSeconds.x, tuning.FireHoldSeconds.y);
            nextAmmoSwap = tuning.AmmoSwapSeconds.y > 0f ? now + Random.Range(tuning.AmmoSwapSeconds.x, tuning.AmmoSwapSeconds.y) : float.MaxValue;
            nextHop = now + 2f;
            input.DebugOverride = true;
            input.DebugMove = Vector2.zero;
            input.DebugAimOverride = true;
            input.DebugAim = Vector2.zero;
            input.DebugFireOverride = true;
            input.DebugFire = false;
        }

        public void End()
        {
            active = false;
            if (input != null)
            {
                input.DebugOverride = false;
                input.DebugMove = Vector2.zero;
                input.DebugAimOverride = false;
                input.DebugAim = Vector2.zero;
                input.DebugFireOverride = false;
                input.DebugFire = false;
            }
            bullets.Unbind();
        }

        private void OnDisable()
        {
            if (active)
                End();
        }

        private void Update()
        {
            if (!active)
                return;
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            bool fighting = run.Machine.Current == GameState.Combat && player.IsAlive;
            float now = Time.time;
            bullets.Update(dt);
            if (!fighting)
            {
                Rest(dt);
                return;
            }

            UpdateTarget(now, dt);
            UpdateMove(now, dt);
            UpdateAim(now, dt);
            UpdateJump(now);
            UpdateAmmo(now);
        }

        // Out of combat (intro, results): the sticks settle and the trigger is released.
        private void Rest(float dt)
        {
            stick = Vector2.MoveTowards(stick, Vector2.zero, tuning.StickAcceleration * dt);
            input.DebugMove = stick;
            input.DebugFire = false;
        }

        // ------------------------------------------------------------------ target

        private void UpdateTarget(float now, float dt)
        {
            if (target != null && !target.IsAlive)
            {
                target = null;
                targetDistance = -1f;
                nextRetarget = now;
            }

            if (target != null)
            {
                Vector2 position = target.HitCenter;
                Vector2 velocity = (position - targetLastPosition) / dt;
                targetVelocity = Vector2.Lerp(targetVelocity, velocity, Mathf.Clamp01(dt * 8f));
                targetLastPosition = position;
                targetDistance = Vector2.Distance(player.FeetPosition, position);
            }

            if (now < nextRetarget)
                return;
            nextRetarget = now + tuning.RetargetInterval;

            IReadOnlyList<Enemy> enemies = pool.Enemies;
            if (enemies == null)
                return;
            Vector2 feet = player.FeetPosition;
            Enemy best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive)
                    continue;
                float distance = Vector2.Distance(feet, enemy.HitCenter);
                if (distance < bestDistance && distance <= tuning.TargetRange)
                {
                    bestDistance = distance;
                    best = enemy;
                }
            }
            if (best == null)
                return;
            if (best != target && CaptureAutopilotMath.ShouldSwitchTarget(target != null ? targetDistance : -1f, bestDistance, tuning.RetargetBias))
            {
                target = best;
                targetDistance = bestDistance;
                targetLastPosition = best.HitCenter;
                targetVelocity = Vector2.zero;
            }
        }

        // ------------------------------------------------------------------ movement

        private void UpdateMove(float now, float dt)
        {
            if (now >= nextDecision)
            {
                nextDecision = now + tuning.DecisionInterval;
                moveDelay.Push(now, PlanMove(now));
            }

            Vector2 wanted = moveDelay.Sample(now - tuning.ReactionSeconds);
            stick = Vector2.MoveTowards(stick, wanted, tuning.StickAcceleration * dt);
            if (stick.sqrMagnitude < StickSnapEpsilon * StickSnapEpsilon)
                stick = Vector2.zero;
            input.DebugMove = stick;
        }

        private Vector2 PlanMove(float now)
        {
            Vector2 feet = player.FeetPosition;
            Vector2 core = player.Position;

            // 1. Bullets: sum of "step away from the path" vectors weighted by how soon and how deep each would hit.
            Vector2 dodge = Vector2.zero;
            int threats = 0;
            float scanSqr = tuning.BulletScanRadius * tuning.BulletScanRadius;
            for (int i = 0; i < bullets.Count && threats < tuning.MaxBulletsConsidered; i++)
            {
                if ((bullets.Position[i] - core).sqrMagnitude > scanSqr)
                    continue;
                float safe = bullets.Radius[i] + player.HitRadius + tuning.DodgeClearance;
                if (CaptureAutopilotMath.BulletThreat(bullets.Position[i], bullets.Velocity[i], core, safe, tuning.BulletHorizon, tieBreak,
                                                      out float urgency, out Vector2 away))
                {
                    dodge += away * urgency;
                    threats++;
                }
            }
            float danger = Mathf.Clamp01(dodge.magnitude);
            if (dodge.sqrMagnitude > 1f)
                dodge.Normalize();

            // 2. Calm behaviour: keep distance from melee enemies, hold range to shooters, circle the nearest enemy, stay off the walls.
            Vector2 calm = Vector2.zero;
            IReadOnlyList<Enemy> enemies = pool.Enemies;
            Enemy nearest = null;
            float nearestSqr = float.MaxValue;
            Vector2 rangedPush = Vector2.zero;
            if (enemies != null)
            {
                float meleeReach = tuning.MeleeKeepDistance;
                for (int i = 0; i < enemies.Count; i++)
                {
                    Enemy enemy = enemies[i];
                    if (enemy == null || !enemy.IsAlive)
                        continue;
                    Vector2 position = enemy.Position;
                    float sqr = (position - feet).sqrMagnitude;
                    if (sqr < nearestSqr)
                    {
                        nearestSqr = sqr;
                        nearest = enemy;
                    }
                    if (IsMelee(enemy))
                    {
                        if (sqr < meleeReach * meleeReach * 2.25f)
                            calm += CaptureAutopilotMath.KeepDistance(feet, position, meleeReach);
                    }
                    else if (sqr < tuning.RangedKeepDistance * tuning.RangedKeepDistance * 4f)
                    {
                        rangedPush += CaptureAutopilotMath.KeepDistance(feet, position, tuning.RangedKeepDistance) * 0.5f;
                    }
                }
            }
            calm = (calm + rangedPush) * tuning.KeepDistanceWeight;

            if (nearest != null)
            {
                if (now >= nextStrafeSwitch)
                {
                    strafeSign = -strafeSign;
                    nextStrafeSwitch = now + Random.Range(tuning.StrafeSwitchSeconds.x, tuning.StrafeSwitchSeconds.y);
                }
                calm += CaptureAutopilotMath.Strafe(feet, nearest.Position, strafeSign) * tuning.StrafeWeight;
            }

            ArenaBoundsInfo(out Rect bounds, out bool haveBounds);
            if (haveBounds)
            {
                calm += CaptureAutopilotMath.WallPush(feet, bounds, tuning.WallMargin) * tuning.WallWeight;
                Vector2 toCenter = bounds.center - feet;
                if (toCenter.sqrMagnitude > 1f)
                    calm += toCenter.normalized * tuning.CenterPull;
            }

            // 3. Stuck on a pillar or in a corner: walk to the middle for a moment.
            if (now >= stuckCheckAt)
            {
                if (stick.magnitude > 0.5f && Vector2.Distance(feet, stuckAnchor) < tuning.StuckDistance)
                    unstickUntil = now + tuning.UnstickSeconds;
                stuckAnchor = feet;
                stuckCheckAt = now + tuning.StuckSeconds;
            }
            if (now < unstickUntil && haveBounds)
                calm = (bounds.center - feet).normalized + new Vector2(-(bounds.center - feet).y, (bounds.center - feet).x).normalized * 0.3f * strafeSign;

            // Danger takes over from calm; calm walks at the cruise speed, danger at full speed.
            Vector2 wanted = dodge * tuning.DodgeWeight + calm * (1f - danger);
            float limit = Mathf.Lerp(tuning.CruiseSpeed, 1f, danger);
            return Vector2.ClampMagnitude(wanted, limit);
        }

        private void ArenaBoundsInfo(out Rect bounds, out bool have)
        {
            bounds = default;
            have = false;
            var arena = pool.Arena;
            if (arena == null || !arena.IsBuilt)
                return;
            bounds = arena.Bounds;
            have = true;
        }

        private static bool IsMelee(Enemy enemy)
        {
            EnemyBehavior behavior = enemy.Data.Behavior;
            return behavior == EnemyBehavior.Chaser || behavior == EnemyBehavior.Charger;
        }

        // ------------------------------------------------------------------ aiming and firing

        private void UpdateAim(float now, float dt)
        {
            ArmSelectionState state = arms.State;
            if (state != lastArmState)
            {
                softSince = state == ArmSelectionState.Soft ? now : -1f;
                lockedSince = state == ArmSelectionState.Locked ? now : -1f;
                lastArmState = state;
            }

            if (target == null)
            {
                // Nothing to shoot at: hold the aim (a locked arm stays locked), release the trigger.
                input.DebugFire = false;
                if (state != ArmSelectionState.Locked)
                    input.DebugAim = Vector2.zero;
                return;
            }

            float desired = DesiredAimAngle();
            switch (state)
            {
                case ArmSelectionState.None:
                    // Flick the left stick at the target: the selector picks the arm in that direction.
                    aimAngle = SelectAngle(desired);
                    input.DebugAim = CaptureAutopilotMath.StickFromCompass(aimAngle);
                    input.DebugFire = false;
                    break;

                case ArmSelectionState.Soft:
                    aimAngle = SelectAngle(desired);
                    input.DebugAim = CaptureAutopilotMath.StickFromCompass(aimAngle);
                    input.DebugFire = false;
                    if (softSince >= 0f && now - softSince >= tuning.SelectHoldSeconds)
                        input.DebugRaiseLockToggle();   // L3: commit to this arm and aim it freely
                    break;

                case ArmSelectionState.Locked:
                    aimAngle = Mathf.MoveTowardsAngle(aimAngle, desired, tuning.AimTurnDegreesPerSecond * dt);
                    if (aimAngle < 0f) aimAngle += 360f;
                    else if (aimAngle >= 360f) aimAngle -= 360f;
                    input.DebugAim = CaptureAutopilotMath.StickFromCompass(aimAngle);
                    UpdateTrigger(now, desired);
                    if (ShouldRehome(now, desired))
                        input.DebugRaiseLockToggle();   // unlock: the arm goes home and the stick (now at the target) picks a better placed one
                    break;
            }
        }

        // The compass angle from the selected arm's muzzle (or the feet) to the target, leading a moving target.
        private float DesiredAimAngle()
        {
            Vector2 origin = player.FeetPosition;
            ArmVisual shown = arms.SelectedArm >= 0 ? arms.GetArm(arms.SelectedArm) : null;
            if (shown != null)
                origin = arms.GroundMuzzle(shown);

            Vector2 aimPoint = target.HitCenter;
            ArmInstance instance = arms.SelectedInstance;
            AmmoTypeData active = ammo.Active;
            if (instance != null && active != null && active.Behavior == AmmoBehavior.Projectile)
                aimPoint = CaptureAutopilotMath.LeadPoint(origin, aimPoint, targetVelocity, instance.Stats.ProjectileSpeed * active.ProjectileSpeedMultiplier);
            return ArmSelector.CompassAngle(aimPoint - origin);
        }

        // While choosing an arm: aim the stick at the target, or at a cooler arm's slot when the best one is overheated.
        private float SelectAngle(float desired)
        {
            int best = -1;
            float bestDelta = float.MaxValue;
            for (int slot = 0; slot < ArmSelector.ArmCount; slot++)
            {
                if (arms.GetArm(slot) == null || fire.GetHeat(slot).IsOverheated)
                    continue;
                float delta = CaptureAutopilotMath.AngleBetween(ArmSelector.HomeAngle(slot), desired);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    best = slot;
                }
            }
            // The stick keeps pointing at the target while that arm is the nearest one anyway; otherwise at the cooler arm's own slot.
            return best >= 0 && !IsNearestOwned(desired, best) ? ArmSelector.HomeAngle(best) : desired;
        }

        private bool IsNearestOwned(float angle, int slot)
        {
            float own = CaptureAutopilotMath.AngleBetween(ArmSelector.HomeAngle(slot), angle);
            for (int other = 0; other < ArmSelector.ArmCount; other++)
                if (other != slot && arms.GetArm(other) != null &&
                    CaptureAutopilotMath.AngleBetween(ArmSelector.HomeAngle(other), angle) < own - 0.01f)
                    return false;
            return true;
        }

        private bool ShouldRehome(float now, float desired)
        {
            int selected = arms.SelectedArm;
            if (selected < 0 || lockedSince < 0f)
                return false;
            bool overheated = fire.GetHeat(selected).IsOverheated;
            if (!overheated && now - lockedSince < tuning.MinLockedSeconds)
                return false;
            float homeDelta = CaptureAutopilotMath.AngleBetween(ArmSelector.HomeAngle(selected), desired);
            if (!overheated && homeDelta <= tuning.RehomeAngle)
                return false;
            // Only worth it when another (cool) arm sits nearer the target than this one.
            for (int slot = 0; slot < ArmSelector.ArmCount; slot++)
            {
                if (slot == selected || arms.GetArm(slot) == null || fire.GetHeat(slot).IsOverheated)
                    continue;
                if (CaptureAutopilotMath.AngleBetween(ArmSelector.HomeAngle(slot), desired) < homeDelta - 20f)
                    return true;
            }
            return false;
        }

        // Trigger: held in bursts, only while the aim is on the target and in range.
        private void UpdateTrigger(float now, float desired)
        {
            if (now >= triggerSwitchAt)
            {
                triggerOn = !triggerOn;
                Vector2 range = triggerOn ? tuning.FireHoldSeconds : tuning.FirePauseSeconds;
                triggerSwitchAt = now + Random.Range(range.x, range.y);
            }

            int selected = arms.SelectedArm;
            bool onTarget = CaptureAutopilotMath.AngleBetween(aimAngle, desired) <= tuning.FireAngleTolerance;
            bool inRange = targetDistance >= 0f && targetDistance <= tuning.FireRange;
            bool cool = selected >= 0 && !fire.GetHeat(selected).IsOverheated;
            input.DebugFire = triggerOn && onTarget && inRange && cool;
        }

        // ------------------------------------------------------------------ jump and ammo

        private void UpdateJump(float now)
        {
            if (player.IsAirborne || jump.CooldownLeft > 0f)
                return;

            // The boss smash: jump when the landing ring is under us, timed so the landing happens mid-air.
            BossController boss = BossEvents.Active;
            if (boss != null && boss.IsAirborne && boss.Data != null)
            {
                if (boss != cachedBoss)
                {
                    cachedBoss = boss;
                    cachedBossJump = boss.GetComponent<BossJump>();
                }
                if (cachedBossJump != null)
                {
                    float airtime = boss.Data.JumpTuning != null ? boss.Data.JumpTuning.Airtime : tuning.SmashAirtimeFallback;
                    float remaining = (1f - cachedBossJump.Progress01) * airtime;
                    float reach = boss.Data.Smash.Radius + tuning.SmashMargin;
                    bool inside = (cachedBossJump.LandingPoint - player.FeetPosition).sqrMagnitude <= reach * reach;
                    if (CaptureAutopilotMath.ShouldJumpForSmash(remaining, tuning.SmashJumpLead, inside))
                    {
                        input.DebugRaiseJump();
                        return;
                    }
                }
            }

            // A melee enemy right on top of us: hop over it (a little flair, and it really does avoid contact damage).
            if (tuning.HopDistance > 0f && now >= nextHop && pool.Enemies != null)
            {
                IReadOnlyList<Enemy> enemies = pool.Enemies;
                float reach = tuning.HopDistance * tuning.HopDistance;
                Vector2 feet = player.FeetPosition;
                for (int i = 0; i < enemies.Count; i++)
                {
                    Enemy enemy = enemies[i];
                    if (enemy == null || !enemy.IsAlive || !IsMelee(enemy))
                        continue;
                    if ((enemy.Position - feet).sqrMagnitude <= reach)
                    {
                        input.DebugRaiseJump();
                        nextHop = now + tuning.HopCooldown;
                        return;
                    }
                }
            }
        }

        private void UpdateAmmo(float now)
        {
            if (now < nextAmmoSwap)
                return;
            nextAmmoSwap = now + Random.Range(tuning.AmmoSwapSeconds.x, tuning.AmmoSwapSeconds.y);

            int current = ammo.ActiveIndex;
            for (int step = 1; step < AmmoSlotSet.Count; step++)
            {
                int index = (Mathf.Max(current, 0) + step) % AmmoSlotSet.Count;
                if (ammo.Get(index) != null)
                {
                    input.DebugRaiseAmmo(index);
                    return;
                }
            }
        }
    }
}
#endif
