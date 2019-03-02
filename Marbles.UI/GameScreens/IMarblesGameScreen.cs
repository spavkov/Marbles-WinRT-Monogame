using Roboblob.XNA.WinRT.GameStateManagement;

namespace Marbles.UI.GameScreens
{
    public interface IMarblesGameScreen : IGameScreen
    {
        void TogglePauseAndResumedState();
        void RestartLevel();
        void PauseIfGameplayIsRunning();
        void StopCurrentGame();
    }
}