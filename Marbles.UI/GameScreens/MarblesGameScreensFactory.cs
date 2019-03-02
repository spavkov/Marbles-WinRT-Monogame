using Microsoft.Xna.Framework;
using Roboblob.XNA.WinRT.GameStateManagement;

namespace Marbles.UI.GameScreens
{
    public interface IMarblesGameScreensFactory
    {
        SuspendedGameScreen CreateSuspendedGameScreen();
        MainMenuScreen CreateMainMenuScreen();
        GamePausedScreen CreateGamePausedScreen();
        GameFinishedScreen CreateGameFinishedScreen();
        SurvivalModeGameScreen CreateSurvivalModeGameScreen();
        EnterUsernameScreen CreateEnterUsernameScreen();
        HighScoresScreen CreateHighScoresScreen();
    }

    public class MarblesGameScreensFactory : IMarblesGameScreensFactory
    {
        private readonly Game _game;

        public MarblesGameScreensFactory(Game game)
        {
            _game = game;
        }

        public EnterUsernameScreen CreateEnterUsernameScreen()
        {
            return new EnterUsernameScreen(_game);
        }

        public HighScoresScreen CreateHighScoresScreen()
        {
            return new HighScoresScreen(_game);
        }


        public SuspendedGameScreen CreateSuspendedGameScreen()
        {
            return new SuspendedGameScreen(_game);
        }

        public MainMenuScreen CreateMainMenuScreen()
        {
            return new MainMenuScreen(_game);
        }

        public GamePausedScreen CreateGamePausedScreen()
        {
            return new GamePausedScreen(_game);
        }

        public GameFinishedScreen CreateGameFinishedScreen()
        {
            return new GameFinishedScreen(_game);
        }

        public SurvivalModeGameScreen CreateSurvivalModeGameScreen()
        {
            return new SurvivalModeGameScreen(_game);
        }
    }
}