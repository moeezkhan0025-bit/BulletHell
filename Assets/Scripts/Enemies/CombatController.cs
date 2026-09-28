using BulletHell.Core;
using BulletHell.Player;
using BulletHell.Projectiles;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>
    /// Runs a round's combat. Stub until the M5b wave spawner: the round's enemies are the ones listed here; every time
    /// Combat starts they are reset to full health, bound to the bullet pool and the player, and started shooting.
    /// When all of them are dead the round is cleared. Any bullets left over from earlier are cleared at round start.
    /// </summary>
    public sealed class CombatController : MonoBehaviour
    {
        [SerializeField] private Enemy[] enemies;
        [SerializeField] private ProjectilePool pool;
        [SerializeField] private PlayerHealth player;

        private RunManager run;
        private int alive;

        private void OnEnable()
        {
            run = GameServices.Ensure().Run;
            run.Machine.StateChanged += OnStateChanged;
            foreach (Enemy enemy in enemies)
            {
                enemy.AutoRespawn = false;
                enemy.Bind(pool, player);
                enemy.Defeated += OnEnemyDefeated;
            }
        }

        private void OnDisable()
        {
            run.Machine.StateChanged -= OnStateChanged;
            foreach (Enemy enemy in enemies)
            {
                enemy.Defeated -= OnEnemyDefeated;
                enemy.AutoRespawn = true;
            }
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to != GameState.Combat || from == GameState.Pause)
                return;

            pool.ReleaseAll();
            foreach (Enemy enemy in enemies)
                enemy.ResetForRound();
            alive = enemies.Length;
            if (alive == 0)
                Debug.LogWarning("CombatController has no enemies: the round can never be cleared.", this);
        }

        private void OnEnemyDefeated(Enemy _)
        {
            alive = Mathf.Max(0, alive - 1);
            if (alive == 0)
                run.CombatCleared();
        }
    }
}
