using BulletHell.AI;
using BulletHell.Enemies;
using BulletHell.Player;
using UnityEngine;

namespace BulletHell.Arena
{
    /// <summary>The "is something behind this obstacle" test, plain math.</summary>
    public static class FadeRule
    {
        /// <summary>
        /// True when a character standing at <paramref name="ground"/> (its feet) is hidden by an obstacle's art: it is
        /// higher on screen than the obstacle's footprint centre (so the obstacle draws in front of it), inside the
        /// art's width (plus a margin) and not above the top of the art.
        /// </summary>
        public static bool IsBehind(Vector2 footprintCenter, Vector2 footprint, Vector2 artSize, Vector2 ground, float sideMargin)
        {
            if (ground.y <= footprintCenter.y)
                return false;
            float artTop = footprintCenter.y - footprint.y * 0.5f + artSize.y;
            if (ground.y > artTop)
                return false;
            return Mathf.Abs(ground.x - footprintCenter.x) <= artSize.x * 0.5f + sideMargin;
        }
    }

    /// <summary>
    /// Fades every standing Tall obstacle to ~40% while the player or an enemy stands behind it, so nothing is hidden.
    /// Low obstacles never fade. Sorting still uses the feet; this only changes the art's opacity. Runs every frame
    /// over the few Tall obstacles and the live enemies, without allocating.
    /// </summary>
    public sealed class TallObstacleFader : MonoBehaviour
    {
        [SerializeField] private ArenaController arena;
        [SerializeField] private PlayerHealth player;
        [SerializeField] private NavigationService navigation;
        [SerializeField] private PerspectiveTuning tuning;

        private PerspectiveTuning Tuning => tuning != null ? tuning : PerspectiveTuning.Fallback;

        private void LateUpdate()
        {
            using var _ = BulletHell.Perf.PerfMarkers.ArenaFader.Auto();
            if (arena == null || !arena.IsBuilt)
                return;

            PerspectiveTuning t = Tuning;
            float speed = (1f - t.TallFadeAlpha) / t.TallFadeSeconds;
            var obstacles = arena.Obstacles;
            for (int i = 0; i < obstacles.Count; i++)
            {
                Obstacle obstacle = obstacles[i];
                if (obstacle.IsLow || obstacle.IsBroken)
                    continue;

                float target = SomethingBehind(obstacle, t.TallFadeSideMargin) ? t.TallFadeAlpha : 1f;
                obstacle.SetFade(Mathf.MoveTowards(obstacle.Fade, target, speed * Time.deltaTime));
            }
        }

        private bool SomethingBehind(Obstacle obstacle, float margin)
        {
            Vector2 center = obstacle.transform.position;
            if (player != null && FadeRule.IsBehind(center, obstacle.Footprint, obstacle.ArtSize, player.transform.position, margin))
                return true;
            if (navigation == null)
                return false;

            var enemies = navigation.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive &&
                    FadeRule.IsBehind(center, obstacle.Footprint, obstacle.ArtSize, enemies[i].Position, margin))
                    return true;
            }
            return false;
        }
    }
}
