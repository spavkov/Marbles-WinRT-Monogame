using System;
using System.Diagnostics;
using Marbles.Core;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Levels;
using Marbles.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roboblob.XNA.WinRT;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.GameStateManagement;
using Roboblob.XNA.WinRT.Input;
using Roboblob.XNA.WinRT.ResolutionIndependence;

namespace Marbles.UI.GameScreens
{
    public class SurvivalModeGameScreen : RollingBackgroundTileScreen, IMarblesGameScreen
    {
        private SpriteBatch _spriteBatch;
        private InputHelper _inputHelper;
        private ResolutionIndependentRenderer _resolution;
        private Camera2D _camera;
        private MarblesWorld _world;
        private SpriteFont _font;
        private IGameScreenManager _gameScreenManager;
        private MultitouchHelper _touchHelper;
        private IMarblesGameScreensFactory _gameScreenFactory;
        private SimpleFPSCounter _fps;
        private MarbleGameLevelControllerSystem _controller;
        private MarblesSurvivalModeEpisode _episode;
        private CurrentGameInformationTrackingSystem _gameInformationTrackingSys;
        private PlayersHighScoresSynchronizer _highestScoresSynchronizer;
        private IExceptionPopupHelper _exceptionPopup;
        private MarbleSoundsSystem _soundSys;


        public SurvivalModeGameScreen(Game game)
            : base(game)
        {
            _inputHelper = Game.Services.GetService(typeof(InputHelper)) as InputHelper;
            _touchHelper = game.Services.GetService(typeof (MultitouchHelper)) as MultitouchHelper;
            _resolution = Game.Services.GetService(typeof(ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            _gameScreenManager = Game.Services.GetService(typeof (IGameScreenManager)) as IGameScreenManager;
            _camera = Game.Services.GetService(typeof(Camera2D)) as Camera2D;
            _fps = Game.Services.GetService(typeof (SimpleFPSCounter)) as SimpleFPSCounter;
            _gameScreenFactory = Game.Services.GetService(typeof(IMarblesGameScreensFactory)) as IMarblesGameScreensFactory;
            _world = Game.Services.GetService(typeof(MarblesWorld)) as MarblesWorld;
            _episode = new MarblesSurvivalModeEpisode();
            _exceptionPopup = Game.Services.GetService(typeof (IExceptionPopupHelper)) as IExceptionPopupHelper;
            _highestScoresSynchronizer =
                Game.Services.GetService(typeof (PlayersHighScoresSynchronizer)) as PlayersHighScoresSynchronizer;
        }

        public override void OnClosed()
        {
            if (_controller != null)
            {
                _controller.GameLevelStateChanged -= OnGameLevelStateChanged;
            }

            if (_soundSys != null)
            {
                _soundSys.StopPlayingBurnSound();
            }

            base.OnClosed();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            _font = Game.Content.Load<SpriteFont>(@"SpriteFonts\debugfont");

            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();
            _controller.GameLevelStateChanged += OnGameLevelStateChanged;

            _gameInformationTrackingSys = _world.GetSystem<CurrentGameInformationTrackingSystem>();

            _soundSys = _world.GetSystem<MarbleSoundsSystem>();
            _soundSys.StopPlayingMenuLoop();

            base.LoadContent();
        }


        private void OnGameLevelStateChanged(object sender, EventArgs e)
        {
/*            if (_gameScreenManager.CurrentTopmostScreen != this)
            {
                return;
            }*/

            if (_controller.LevelState == LevelState.Paused)
            {
                if (_gameScreenManager.CurrentTopmostScreen == this)
                {
                    _gameScreenManager.PushScreen(_gameScreenFactory.CreateGamePausedScreen());
                }               
            }
            else if (_controller.LevelState == LevelState.Completed || _controller.LevelState == LevelState.Failed)
            {
                PauseRollingBackground = true;
                var currentScore = _gameInformationTrackingSys.CurrentLevelScore;
                if (currentScore > 0 && _highestScoresSynchronizer.IsCurrentUsersNewHighScore(currentScore))
                {
                    var enterUsernameScreen = _gameScreenFactory.CreateEnterUsernameScreen();
                    enterUsernameScreen.HighScore = currentScore;
                    _gameScreenManager.PushScreen(enterUsernameScreen);
                    return;
                }

                var completedScreen = _gameScreenFactory.CreateGameFinishedScreen();
                completedScreen.ScreenUsageType = GameFinishedScreenType.GameFailed;
                _gameScreenManager.PushScreen(completedScreen);
            }
            else
            {
                PauseRollingBackground = false;
            }
        }


        public override void OnShown()
        {
            base.OnShown();
            Start();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (gameTime.IsRunningSlowly)
            {
                Debug.WriteLine("Slow from game screen" + DateTime.Now);
            }

            if (_resolution.ScreenWidth < 800)
            {
                return;
            }

            if (_gameScreenManager.CurrentTopmostScreen == this)
            {
                if (_controller.IsLevelLoaded)
                {

                }

                if (_inputHelper.IsNewPress(Keys.R))
                {
                    RestartLevel();
                }
                else if (_inputHelper.IsNewPress(Keys.P))
                {
                    TogglePauseAndResumedState();
                }
                else if (_inputHelper.IsNewPress(Keys.Escape))
                {
                    _gameScreenManager.ChangeScreen(_gameScreenFactory.CreateMainMenuScreen());
                }
                /*else if (_inputHelper.IsNewPress(Keys.D1))
                {
                    PauseRollingBackground = true;
                    var screen = _gameScreenFactory.CreateGameFinishedScreen();
                    screen.ScreenUsageType = GameFinishedScreenType.GameFailed;
                    _gameScreenManager.PushScreen(screen);
                }
                else if (_inputHelper.IsNewPress(Keys.D2))
                {
                    PauseRollingBackground = true;
                    var screen = _gameScreenFactory.CreateEnterUsernameScreen();
                    _gameScreenManager.PushScreen(screen);
                }
                else if (_inputHelper.IsNewPress(Keys.D3))
                {
                    PauseRollingBackground = true;
                    var screen = _gameScreenFactory.CreateGameFinishedScreen();
                    screen.ScreenUsageType = GameFinishedScreenType.AfterHighscoreScreen;
                    _gameScreenManager.PushScreen(screen);
                }
                else if (_inputHelper.IsNewPress(Keys.Subtract))
                {
                    if (_inputHelper.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                    {
                        _camera.Zoom = _camera.Zoom - 0.01f;
                        return;
                    }

                    if (_controller.IsLevelLoaded && _controller.LevelState == LevelState.Running)
                    {
                        _controller.DecreaseCurrentLevelTime(TimeSpan.FromSeconds(10));
                    }
                }
                else if (_inputHelper.IsNewPress(Keys.Add))
                {
                    if (_inputHelper.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                    {
                        _camera.Zoom = _camera.Zoom + 0.01f;
                        return;
                    }

                    if (_controller.IsLevelLoaded && _controller.LevelState == LevelState.Running)
                    {
                        _controller.IncreaseCurrentLevelTime(TimeSpan.FromSeconds(10));
                    }
                }
                else if (_inputHelper.IsNewPress(Keys.Add))
                {
                    if (_inputHelper.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
                    {
                        _camera.Zoom = _camera.Zoom + 0.01f;
                        return;
                    }
                }*/

                _world.Update(gameTime);
            }

        }

        public void Start()
        {
            try
            {
                PauseRollingBackground = false;
                _controller.LoadLevel(_episode.Levels[0]);
                _controller.StartLevel();
            }
            catch (Exception e)
            {
                _exceptionPopup.ShowExceptionPopup(e);
            }
        }

        public void RestartLevel()
        {
            Start();
        }

        public void TogglePauseAndResumedState()
        {
            if (!_controller.IsLevelLoaded)
            {
                return;
            }
            if (_controller.LevelState == LevelState.Paused)
            {
                _controller.ResumeCurrentLevel();
                PauseRollingBackground = false;
            }
            else if (_controller.LevelState == LevelState.Running)
            {
                _controller.PauseCurrentLevel();
                PauseRollingBackground = true;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            _resolution.SetupVirtualScreenViewport();

            if (_resolution.ScreenWidth > 800)
            {
                if (_world != null && _controller.IsLevelLoaded)
                {
                    _world.Render(gameTime);
                }
                else
                {
                }
            }
        }

        public void PauseIfGameplayIsRunning()
        {
            if (!_controller.IsLevelLoaded)
            {
                return;
            }
            if (_controller.LevelState == LevelState.Running)
            {
                _controller.PauseCurrentLevel();
            }
        }

        public void StopCurrentGame()
        {
            if (!_controller.IsLevelLoaded)
            {
                return;
            }
            if (_controller.LevelState == LevelState.Running || _controller.LevelState == LevelState.Paused)
            {
                _controller.StopLevel();
            }
        }

        public override void OnScreenChanged(object sender, EventArgs e)
        {
            base.OnScreenChanged(sender, e);
            PauseRollingBackground = _gameScreenManager.CurrentTopmostScreen != this;
        }
    }
}