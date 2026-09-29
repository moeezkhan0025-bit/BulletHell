namespace BulletHell.Player
{
    /// <summary>
    /// The timing of a jump, without any engine objects: ground, airborne for the airtime, cooldown, ground.
    /// Progress runs 0..1 over the airtime. Tick reports the frame the jump lands.
    /// </summary>
    public sealed class JumpTimeline
    {
        public bool IsAirborne { get; private set; }
        /// <summary>0 at takeoff, 1 at landing; 0 on the ground.</summary>
        public float Progress01 { get; private set; }
        public float CooldownLeft { get; private set; }

        public bool CanStart => !IsAirborne && CooldownLeft <= 0f;

        public bool TryStart()
        {
            if (!CanStart)
                return false;
            IsAirborne = true;
            Progress01 = 0f;
            return true;
        }

        /// <summary>Advances time. Returns true on the frame the jump lands.</summary>
        public bool Tick(float deltaTime, float airtime, float cooldownAfterLanding)
        {
            if (!IsAirborne)
            {
                if (CooldownLeft > 0f)
                    CooldownLeft -= deltaTime;
                return false;
            }

            Progress01 += deltaTime / airtime;
            if (Progress01 < 1f)
                return false;

            IsAirborne = false;
            Progress01 = 0f;
            CooldownLeft = cooldownAfterLanding;
            return true;
        }

        /// <summary>Back on the ground at once, no cooldown (a new round, death).</summary>
        public void Cancel()
        {
            IsAirborne = false;
            Progress01 = 0f;
            CooldownLeft = 0f;
        }
    }
}
