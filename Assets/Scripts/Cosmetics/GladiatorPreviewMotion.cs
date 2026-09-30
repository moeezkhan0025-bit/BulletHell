using BulletHell.Core;
using BulletHell.Feedback;
using UnityEngine;

namespace BulletHell.Cosmetics
{
    /// <summary>
    /// Gives the character creation preview the player's idle motion (breathing) from the toolkit, so the assembled
    /// doll is seen moving exactly as in the game. Needs a ProceduralMotion on the same object.
    /// </summary>
    [RequireComponent(typeof(ProceduralMotion))]
    public sealed class GladiatorPreviewMotion : MonoBehaviour
    {
        private void Awake() => GetComponent<ProceduralMotion>().SetTuning(GameServices.Ensure().Config.Feedback.PlayerMotion);
    }
}
