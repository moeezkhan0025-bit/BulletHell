using BulletHell.Core;
using UnityEngine;

namespace BulletHell.Enemies
{
    /// <summary>
    /// Runs a round's combat. Stub for M4: the round's enemies are the ones listed here; every time Combat starts they
    /// are reset to full health, and when all of them are dead the round is cleared. Real waves replace this in M5.
    /// </summary>
    public sealed class CombatController : MonoBehaviour
    {
        [SerializeField] private Enemy[] enemies;

        private RunManager run;
        private int alive;

        private void OnEnable()
        {
            run = GameServices.Ensure().Run;
            run.Machine.StateChanged += OnStateChanged;
            foreach (Enemy enemy in enemies)
            {
                enemy.AutoRespawn = false;
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
