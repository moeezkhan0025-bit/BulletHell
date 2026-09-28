using BulletHell.Core;
using BulletHell.Weapons;
using UnityEngine;

namespace BulletHell.Player
{
    /// <summary>
    /// Player-side access to the run's inventories (armaments and spare arms that aren't equipped). The data lives in
    /// the RunState, so it is saved and survives between rounds; the starting stock comes from the GameConfig.
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        private RunState state;

        public ArmamentInventory Armaments => State.Armaments;
        public ArmInventory Arms => State.SpareArms;

        private RunState State
        {
            get
            {
                if (state == null)
                {
                    RunManager run = GameServices.Ensure().Run;
                    run.EnsureRun();
                    state = run.State;
                }
                return state;
            }
        }
    }
}
