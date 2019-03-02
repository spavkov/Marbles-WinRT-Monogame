using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Marbles.Core;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Repositories;
using Marbles.Core.Systems;
using Marbles.UI.GameScreens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roboblob.Core.WinRT.IO.Serialization;
using Roboblob.Core.WinRT.Threading;
using Roboblob.XNA.WinRT;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.Content;
using Roboblob.XNA.WinRT.GameStateManagement;
using Roboblob.XNA.WinRT.Input;
using Roboblob.XNA.WinRT.ResolutionIndependence;
using Roboblob.XNA.WinRT.Scoreoid;

namespace Marbles.UI
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class MarblesGame : Game
    {
        GraphicsDeviceManager _graphics;
        SpriteBatch _spriteBatch;
        private IMarblesGameScreensFactory _gameScreenFactory;
        private GameScreenManager _gameScreenManager;
        private SimpleFPSCounter _fpsCounter;
        private SpriteFont _font;
        private Vector2 _fpsPosition = new Vector2(10,10);
        private InputHelper _inputHelper;
        private Camera2D _camera;
        //private Vector2 _scaledMouseWorldPosition;
        private MultitouchHelper _multitouchHelper;
        private TextureSheetLoader _textureSheetsLoader;
        private MarblesWorld _marblesWorld;
        private JsonSerializer _serializer;
        private PlayersLocalDataMaintainer _playersLocalDataMaintainer;
        private ScoreoidClient _scoreoidClient;
        private PlayersHighScoresSynchronizer _playersHighScoresSynchronizer;
        private MarbleGameLevelControllerSystem _controller;
        private HighScoresRetriever _highScoresRetriever;
        private UiThreadDispatcher _uiThreadDispatcher;
        private IExceptionPopupHelper _errorsPopup;
        private bool _weAlreadyNotifiedOfGameReadyToStart;
        private bool _weAreInSnappedMode;
        private Texture2D _snappedLogo;
        private Vector2 _snappedLogoPos;
        private string _pausedText = "Paused";
        private Vector2 _pausedPos;
        private GameSettingsRepository _settingsRepository;

        public IExceptionPopupHelper ExceptionsPopupHelper
        {
            get { return _errorsPopup; }
            set
            {
                _errorsPopup = value;
                Services.AddService(typeof(IExceptionPopupHelper), value);
            }
        }

        public MarblesGame()
        {
            Windows.UI.Input.PointerVisualizationSettings.GetForCurrentView().IsContactFeedbackEnabled = false;
            Windows.UI.Input.PointerVisualizationSettings.GetForCurrentView().IsBarrelButtonFeedbackEnabled = false;
            Content.RootDirectory = "Content";
            _graphics = new GraphicsDeviceManager(this);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            try
            {
                //TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 50.0);
                IsFixedTimeStep = false;
                _graphics.SynchronizeWithVerticalRetrace = true;
                _graphics.ApplyChanges();

                _uiThreadDispatcher = new UiThreadDispatcher();
                Services.AddService(typeof(UiThreadDispatcher), _uiThreadDispatcher);

                _serializer = new JsonSerializer();
                Services.AddService(typeof(ISerializer), _serializer);

                _settingsRepository = new GameSettingsRepository(this);
                Services.AddService(typeof(GameSettingsRepository), _settingsRepository);
                LoadSettings();

                // TODO: Add your initialization logic here
                Resolution = new ResolutionIndependentRenderer(this)
                    {
                        ScreenWidth = _graphics.GraphicsDevice.Viewport.Width,
                        ScreenHeight = _graphics.GraphicsDevice.Viewport.Height,
                        VirtualWidth = 1366,
                        VirtualHeight = 768,
                        BackgroundColor = Color.FromNonPremultiplied(244, 209, 50, 255) // new Color(57,28,10)                               
                    };
                Resolution.Initialize();

                Services.AddService(typeof(ResolutionIndependentRenderer), Resolution);

                _gameScreenFactory = new MarblesGameScreensFactory(this);
                Services.AddService(typeof(IMarblesGameScreensFactory), _gameScreenFactory);

                _gameScreenManager = new GameScreenManager(this);
                Services.AddService(typeof(IGameScreenManager), _gameScreenManager);

                _fpsCounter = new SimpleFPSCounter();
                Services.AddService(typeof(SimpleFPSCounter), _fpsCounter);

                _camera = new Camera2D(Resolution) { Zoom = 1f };
                Services.AddService(typeof(Camera2D), _camera);

                _camera.SetPosition(new Vector2(Resolution.VirtualWidth / 2, Resolution.VirtualHeight / 2));

                _inputHelper = new InputHelper();
                Services.AddService(typeof(InputHelper), _inputHelper);

                _multitouchHelper = new MultitouchHelper(_inputHelper);
                Services.AddService(typeof(MultitouchHelper), _multitouchHelper);

                _textureSheetsLoader = new TextureSheetLoader(this);
                Services.AddService(typeof(ITextureSheetLoader), _textureSheetsLoader);

                _marblesWorld = new MarblesWorld(this);
                Services.AddService(typeof(MarblesWorld), _marblesWorld);

                _playersLocalDataMaintainer = new PlayersLocalDataMaintainer(_serializer, _uiThreadDispatcher);

                _scoreoidClient = new ScoreoidClient(GameConstants.ScoreoidApiKey, GameConstants.ScoreoidGameId);

                _highScoresRetriever = new HighScoresRetriever(_scoreoidClient);
                Services.AddService(typeof(HighScoresRetriever), _highScoresRetriever);

                _playersHighScoresSynchronizer = new PlayersHighScoresSynchronizer(_scoreoidClient, _playersLocalDataMaintainer, _uiThreadDispatcher);
            
                _playersHighScoresSynchronizer.CurrentUserHighscoresDataChanged += OnHighScoreDataChanged;

                Services.AddService(typeof(PlayersHighScoresSynchronizer), _playersHighScoresSynchronizer);

                base.Initialize();
            }
            catch (Exception e)
            {
                if (_errorsPopup != null && !_errorsPopup.IsOpen)
                {
                    _errorsPopup.ShowExceptionPopup(e);
                }
            }
        }

        private async void LoadSettings()
        {
            await _settingsRepository.Load();
        }

        private void OnHighScoreDataChanged(object sender, EventArgs e)
        {
            if (_controller == null)
            {
                return;
            }

            _uiThreadDispatcher.InvokeOnUiThread(() => _controller.SetCurrentPlayerAndHighscoreData(_playersHighScoresSynchronizer.CurrentPlayerName,
                                                                                                    _playersHighScoresSynchronizer.GetCurrentPlayerHighScore()));
        }

        protected ResolutionIndependentRenderer Resolution { get; set; }

        public bool IsGameplayCurrentlyRunning
        {
            get { return _gameScreenManager.CurrentTopmostScreen is SurvivalModeGameScreen; }
        }

        public void PauseGameplay()
        {
            if (_controller != null)
            {
                _controller.PauseCurrentLevel();
            }

/*
            var currentScreen = _gameScreenManager.CurrentTopmostScreen as IMarblesGameScreen;
            if (currentScreen == null)
            {
                return;
            }

            currentScreen.PauseIfGameplayIsRunning();
*/

        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            try
            {
                // Create a new SpriteBatch, which can be used to draw textures.
                _spriteBatch = new SpriteBatch(GraphicsDevice);

                _font = Content.Load<SpriteFont>(@"SpriteFonts\debugfont");

                _snappedLogo = Content.Load<Texture2D>(@"Backgrounds\LogoForSnapped");

                // TODO: use this.Content to load your game content here
                base.LoadContent();
            }
            catch (Exception e)
            {
                if (_errorsPopup != null && !_errorsPopup.IsOpen)
                {
                    _errorsPopup.ShowExceptionPopup(e);
                }
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            base.UnloadContent();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            try
            {
                if (gameTime.IsRunningSlowly)
                {
                    Debug.WriteLine("Slow from main THE GAME" + DateTime.Now);
                }

                // TODO: Add your update logic here
                _fpsCounter.Update(gameTime);
                _multitouchHelper.Update();
                _gameScreenManager.Update(gameTime);

                base.Update(gameTime);
            }
            catch (Exception e)
            {
                if (_errorsPopup != null && !_errorsPopup.IsOpen)
                {
                    _errorsPopup.ShowExceptionPopup(e);
                }
            }
        }

        public void InitWorld()
        {
            try
            {
                _marblesWorld.Initialize();
                _marblesWorld.Start();

                _controller = _marblesWorld.GetSystem<MarbleGameLevelControllerSystem>();
                _controller.UserRequestedToGoToMainMenu += OnUserWantsToGoToMainMenu;

                Task.Factory.StartNew(() => _playersHighScoresSynchronizer.Initialize());
            }
            catch (Exception e)
            {
                if (_errorsPopup != null && !_errorsPopup.IsOpen)
                {
                    _errorsPopup.ShowExceptionPopup(e);
                }
            }
        }

        private void OnUserWantsToGoToMainMenu(object sender, EventArgs e)
        {
            if (!(_gameScreenManager.CurrentTopmostScreen is MainMenuScreen))
            {
                _gameScreenManager.ChangeScreen(_gameScreenFactory.CreateMainMenuScreen());
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            try
            {
                Resolution.BeginDraw();

                if (!_weAreInSnappedMode)
                    base.Draw(gameTime);
                else
                {
                    _spriteBatch.Begin();
                    //_spriteBatch.DrawString(_font, _fpsCounter.FramesPerSecondText, _fpsPosition, Color.Red);

                    _spriteBatch.DrawString(_font, _pausedText, _pausedPos, Color.Red);
                    _spriteBatch.Draw(_snappedLogo, _snappedLogoPos, Color.White);

                    //_spriteBatch.DrawString(_font, string.Format("Screen Scaled Mouse:{0}:{1}", (int)_scaledMouseScreenPosition.X, (int)_scaledMouseScreenPosition.Y), new Vector2(0, 20), Color.Red);
                    _spriteBatch.End();
                }
                _fpsCounter.MarkFrameAsDrawn();
            }
            catch (Exception e)
            {
                if (_errorsPopup != null && !_errorsPopup.IsOpen)
                {
                    _errorsPopup.ShowExceptionPopup(e);
                }
            }
        }

        public void ScreenSizeChanged(double width, double height)
        {
            _snappedLogoPos = new Vector2((float) (width - 300) / 2, 10f);
            var pausedTextSize = _font.MeasureString(_pausedText);
            _pausedPos = new Vector2((float) (width - pausedTextSize.X) / 2, _snappedLogo.Height + 10 + 30f);

            SetupResolution((int) width, (int) height);
            var screens = _gameScreenManager.GetLiveScreens();

            foreach (var gameScreen in screens)
            {
                gameScreen.OnScreenSizeChanged();
            }

            if (width <= 321)
            {
                _weAreInSnappedMode = true;
                PauseGameplay();
            }
            else
            {
                _weAreInSnappedMode = false;
            }
        }

        private void SetupResolution(int width, int height)
        {
            Resolution.ScreenWidth = width;
            Resolution.ScreenHeight = height;

            Resolution.Initialize();
            _camera.RecalculateTransformationMatrices();
        }

        public void Resume()
        {

        }

        public void Suspend()
        {
           // var susp = _gameScreenFactory.CreateSuspendedGameScreen();
           // _gameScreenManager.PushScreen(susp);
            var screenAsIgameplay = _gameScreenManager.GetCurrentScreen<IMarblesGameScreen>();
            if (screenAsIgameplay != null)
            {
                screenAsIgameplay.PauseIfGameplayIsRunning();
            }
        }

        public void NotifyThatGameIsReady()
        {
            if (!_weAlreadyNotifiedOfGameReadyToStart)
            {
                _weAlreadyNotifiedOfGameReadyToStart = true;
                OnGameReadyToStart();
            }
        }

        public event EventHandler GameReadyToStart;

        private void OnGameReadyToStart()
        {
            var h = GameReadyToStart;

            if (h != null)
            {
                h(this, EventArgs.Empty);
            }
        }

        public void GoToMainMenuScreen()
        {
            _gameScreenManager.ChangeScreen(_gameScreenFactory.CreateMainMenuScreen());
        }
    }
}
