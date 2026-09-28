using System.Text;
using BulletHell.Armory;
using BulletHell.Core;
using BulletHell.Shop;
using UnityEngine;

namespace BulletHell.UI
{
    /// <summary>
    /// Shows the screen for the current run state (Round Results, Shop, Armory) and hides them during Combat.
    /// Continue advances the run; Menu returns to the Main Menu (the save stays).
    /// </summary>
    public sealed class GameFlowUI : MonoBehaviour
    {
        [SerializeField] private GameSceneController scene;
        [SerializeField] private FlowPanel roundResults;
        [SerializeField] private FlowPanel pause;
        [SerializeField] private FlowPanel gameOver;
        [SerializeField] private ShopScreen shop;
        [SerializeField] private ArmoryScreen armory;

        private readonly StringBuilder builder = new StringBuilder(128);
        private RunManager run;
        private ShopPool shopPool;

        private void Awake()
        {
            GameServices services = GameServices.Ensure();
            run = services.Run;
            shopPool = services.Config.ShopPool;

            roundResults.ContinuePressed += run.Advance;
            roundResults.MenuPressed += scene.QuitToMenu;
            pause.ContinuePressed += Resume;
            pause.MenuPressed += scene.QuitToMenu;
            gameOver.MenuPressed += scene.QuitToMenu;
            shop.ContinuePressed += run.Advance;
            shop.MenuPressed += scene.QuitToMenu;
            armory.ContinuePressed += run.Advance;
            armory.MenuPressed += scene.QuitToMenu;

            HideAll();
        }

        private void OnEnable() => run.Machine.StateChanged += OnStateChanged;

        private void OnDisable() => run.Machine.StateChanged -= OnStateChanged;

        private void OnStateChanged(GameState from, GameState to)
        {
            HideAll();
            RunState state = run.State;

            switch (to)
            {
                case GameState.RoundResults:
                    builder.Clear();
                    builder.Append("Currency collected: +").Append(run.LastReward)
                           .Append("\nTotal currency: ").Append(state.Currency);
                    roundResults.Show($"Round {state.Round} cleared", builder.ToString());
                    break;
                case GameState.Shop:
                    shop.Show(state, shopPool);
                    break;
                case GameState.Armory:
                    armory.Show(state);
                    break;
                case GameState.Pause:
                    pause.Show("Paused", $"Round {state.Round}");
                    break;
                case GameState.GameOver:
                    gameOver.Show("GAME OVER", $"You reached round {state.Round}.\nThe run has ended and its save was deleted.", false);
                    break;
            }
        }

        private void Resume() => run.SetPaused(false);

        private void HideAll()
        {
            roundResults.Hide();
            pause.Hide();
            gameOver.Hide();
            shop.Hide();
            armory.Hide();
        }
    }
}
