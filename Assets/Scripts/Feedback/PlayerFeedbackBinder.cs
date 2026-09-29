using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Feedback
{
    /// <summary>Gives the player's motion and hit-feedback components their tuning from the FeedbackTuning in GameConfig (enemies get theirs in Enemy.Initialize).</summary>
    public sealed class PlayerFeedbackBinder : MonoBehaviour
    {
        [SerializeField] private ProceduralMotion motion;
        [SerializeField] private HitFeedback hit;

        private void Awake()
        {
            FeedbackTuning tuning = GameServices.Ensure().Config.Feedback;
            motion.SetTuning(tuning.PlayerMotion);
            hit.Configure(tuning.PlayerHit);
        }
    }
}
