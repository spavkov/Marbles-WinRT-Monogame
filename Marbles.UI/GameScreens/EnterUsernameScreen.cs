using System.Threading.Tasks;
using Marbles.Core;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roboblob.XNA.WinRT;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.GameStateManagement;
using Roboblob.XNA.WinRT.Input;
using Roboblob.XNA.WinRT.Rendering;
using Roboblob.XNA.WinRT.ResolutionIndependence;
using Roboblob.XNA.WinRT.Scoreoid;
using Scoreoid;

namespace Marbles.UI.GameScreens
{
    public class EnterUsernameScreen : GameScreen
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
        private MarblesArcadeModeEpisodes _arcadeModeEpisodes;
        private VirtualKeyboard _virtualKeyboard;
        private Texture2D _blankTexture;
        private Rectangle _fullScreenRect;
        private Vector2 _highScoreTitlePosition = new Vector2(100,100);
        private string _currentPlayerName = string.Empty;
        private Vector2 _currentPlayerNamePosition = new Vector2(100,200);

        public EnterUsernameScreen(Game game) : base(game)
        {
            _inputHelper = Game.Services.GetService(typeof(InputHelper)) as InputHelper;
            _touchHelper = game.Services.GetService(typeof(MultitouchHelper)) as MultitouchHelper;
            _resolution = Game.Services.GetService(typeof(ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            _gameScreenManager = Game.Services.GetService(typeof(IGameScreenManager)) as IGameScreenManager;
            _camera = Game.Services.GetService(typeof(Camera2D)) as Camera2D;
            _fps = Game.Services.GetService(typeof(SimpleFPSCounter)) as SimpleFPSCounter;
            _gameScreenFactory = Game.Services.GetService(typeof(IMarblesGameScreensFactory)) as IMarblesGameScreensFactory;

            _world = game.Services.GetService(typeof (MarblesWorld)) as MarblesWorld;
            _highscoresSynchronizer =
                Game.Services.GetService(typeof (PlayersHighScoresSynchronizer)) as PlayersHighScoresSynchronizer;

            _gameInfo = _world.GetSystem<CurrentGameInformationTrackingSystem>();
            _currentPlayerName = _gameInfo.CurrentPlayerName;
            _soundSys = _world.GetSystem<MarbleSoundsSystem>();

            HasInTransition = true;
            TransitionInDesiredDurationInSeconds = 0.5f;

            RecalculateScreenElementSizes();
        }

        public override void OnScreenChanged(object sender, System.EventArgs e)
        {
            if (_gameScreenManager.CurrentTopmostScreen == this)
            {
                _currentPlayerName = _highscoresSynchronizer.CurrentPlayerName;
            }
        }

        public int HighScore = 0;
        private PlayersHighScoresSynchronizer _highscoresSynchronizer;
        private float _currentFadeAmount;
        private Color _currentColor;
        private bool _transitionInFinished;
        private AlignedText _congratulationsPlayerAlignedText;
        private AlignedText _currentPlayerNameAlignedText;
        private AlignedText _itIsYourNewHighScoreAlignedText;
        private AlignedText _editYourNameAlignedText;
        private AlignedText _toEnterHallOfFameAlignedText;
        private CurrentGameInformationTrackingSystem _gameInfo;
        private SpriteFont _kbfont;
        private ToggleImageButton _goBackButton;
        private MarbleSoundsSystem _soundSys;


        public override void OnShown()
        {
            base.OnShown();
            _soundSys.PlayLevelCompletedSound();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            _blankTexture = new Texture2D(Game.GraphicsDevice, 1, 1);
            var color = new Color[1];
            color[0] = Color.White;
            _blankTexture.SetData<Color>(color);

            _font = Game.Content.Load<SpriteFont>(GameConstants.GuiFontLarge);
            _kbfont = Game.Content.Load<SpriteFont>(GameConstants.VirtualKeyboardFont);

            _virtualKeyboard = new VirtualKeyboard(Game, _kbfont, _camera, _resolution, _touchHelper, _inputHelper);
            _virtualKeyboard.BackgroundColor = Color.Transparent;
            _virtualKeyboard.KeyboardSize = new Vector2(_resolution.VirtualWidth - 100, _resolution.VirtualHeight * 0.5f - 50);
            _virtualKeyboard.LoadContent();
            _virtualKeyboard.DrawBackground = false;

            _congratulationsPlayerAlignedText = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 100), 100);
            _congratulationsPlayerAlignedText.Text.AppendFormat("Congratulations");
            _congratulationsPlayerAlignedText.RecalculateDrawingPosition();

            _currentPlayerNameAlignedText = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 150), 100);

            _itIsYourNewHighScoreAlignedText = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 250), 100);
            _itIsYourNewHighScoreAlignedText.Text.AppendFormat("Your new High Score is {0}", _gameInfo.CurrentLevelScore);
            _itIsYourNewHighScoreAlignedText.RecalculateDrawingPosition();

            _editYourNameAlignedText = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 300), 100);
            _editYourNameAlignedText.Text.Append("Edit your name and press Enter");
            _editYourNameAlignedText.RecalculateDrawingPosition();

            _toEnterHallOfFameAlignedText = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 350), 100);
            _toEnterHallOfFameAlignedText.Text.Append("To save your score to Marbles Hall of Fame");
            _toEnterHallOfFameAlignedText.RecalculateDrawingPosition();

            _goBackButton = new ToggleImageButton(Game, new ToggleButtonSettings()
            {
                Position = new Vector2(50, 50),
                TextureSheetName = @"SpriteSheets\GameArt",
                NormalSubTextureName = "BackButton",
                PressedlSubTextureName = "BackButton"
            });

            _goBackButton.Clicked += OnGoBackClicked;

            _goBackButton.LoadContent();

            RefreshPlayerName();

            base.LoadContent();
        }

        private void RefreshPlayerName()
        {
            _currentPlayerNameAlignedText.Text.Length = 0;
            _currentPlayerNameAlignedText.Text.Append(_currentPlayerName);
            _currentPlayerNameAlignedText.RecalculateDrawingPosition();
        }

        private void OnGoBackClicked(object sender, ButtonClickedEventArgs e)
        {
            _gameScreenManager.PopScreen();
            var newScreen = _gameScreenFactory.CreateGameFinishedScreen();
            newScreen.ScreenUsageType = GameFinishedScreenType.AfterHighscoreScreen;
            newScreen.CompletedMessage = string.Format("Your High Score was not saved. Pitty.");
            _gameScreenManager.PushScreen(newScreen);
        }


        public override void OnScreenSizeChanged()
        {
            RecalculateScreenElementSizes();
            base.OnScreenSizeChanged();
        }
        public override void Update(GameTime gameTime)
        {
            _virtualKeyboard.Update(gameTime);
            _goBackButton.Update(gameTime);

            var virtualKeysPressed = _virtualKeyboard.GetState().GetPressedKeys();

            var userPressedEnterOnVirtualKey = ProcessVirtualKbInput(virtualKeysPressed);

            var userPressedEnterViaKb = ProcessStandardKeyboardInput();

            if (userPressedEnterOnVirtualKey || userPressedEnterViaKb)
            {
                if (_highscoresSynchronizer.CurrentPlayerName != _currentPlayerName)
                {
                    Task.Factory.StartNew(async () =>
                    {
                        await _highscoresSynchronizer.SetCurrentPlayerName(_currentPlayerName);
                        await _highscoresSynchronizer.SetNewHighestScoreForCurrentPlayer(HighScore);
                    });
                }
                else if (_highscoresSynchronizer.IsCurrentUsersNewHighScore(HighScore))
                {
                    Task.Factory.StartNew(
                        async () => { await _highscoresSynchronizer.SetNewHighestScoreForCurrentPlayer(HighScore); });
                }
            }

            bool userPressedEscape = _inputHelper.IsNewPress(Keys.Escape);

            if (userPressedEnterOnVirtualKey || userPressedEnterViaKb || userPressedEscape)
            {
                _gameScreenManager.PopScreen();

                if (userPressedEnterOnVirtualKey || userPressedEnterViaKb)
                {
                    var nextScreen = _gameScreenFactory.CreateGameFinishedScreen();
                    nextScreen.ScreenUsageType = GameFinishedScreenType.AfterHighscoreScreen;
                    nextScreen.CompletedMessage = string.Format("Your High Score {0} was saved.", _gameInfo.CurrentLevelScore);

                    _gameScreenManager.PushScreen(nextScreen);
                }
                return;
            }

            base.Update(gameTime);
        }

        public static bool IsKeyAChar(Keys key)
        {
            return key >= Keys.A && key <= Keys.Z;
        }

        public static bool IsKeyADigit(Keys key)
        {
            return (key >= Keys.D0 && key <= Keys.D9) || (key >= Keys.NumPad0 && key <= Keys.NumPad9);
        }

        private bool ProcessStandardKeyboardInput()
        {
            var keys = _inputHelper.CurrentKeyboardState.GetPressedKeys();
            var caps = _inputHelper.CurrentKeyboardState.IsKeyDown(Keys.CapsLock) ||
                       _inputHelper.CurrentKeyboardState.IsKeyDown(Keys.LeftShift) ||
                       _inputHelper.CurrentKeyboardState.IsKeyDown(Keys.RightShift);
            foreach (var keyse in keys)
            {
                if (_inputHelper.IsNewPress(keyse))
                {
                    _soundSys.PlayKeyboardClickSound();
                    if (keyse == Keys.Enter)
                    {
                        return true;
                    }

                    _soundSys.PlayKeyboardClickSound();
                    if (keyse == Keys.Back)
                    {
                        if (_currentPlayerName.Length > 0)
                        {
                            _currentPlayerName = _currentPlayerName.Substring(0, _currentPlayerName.Length - 1);
                        }
                    }
                    else if (keyse == Keys.Space)
                    {
                        if (_currentPlayerName.Length < GameConstants.MaxUsernameLenght)
                            _currentPlayerName += "_";
                    }
                    else
                    {
                        if (_currentPlayerName.Length < GameConstants.MaxUsernameLenght)
                        {
                            if ((IsKeyAChar(keyse)))
                            {
                                if (_currentPlayerName.Length < GameConstants.MaxUsernameLenght)
                                    _currentPlayerName += caps ? keyse.ToString().ToUpper() : keyse.ToString().ToLower();
                            }
                            else if (IsKeyADigit(keyse))
                            {
                                var digit = keyse.ToString();
                                _currentPlayerName += keyse.ToString().Substring(digit.Length-1, 1);
                            }
                        }
                    }

                    RefreshPlayerName();
                }
            }

            return false;
        }

        private bool ProcessVirtualKbInput(Keys[] virtualKeysPressed)
        {
            foreach (var key in virtualKeysPressed)
            {
                if (key == Keys.Enter)
                {
                    return true;
                }

                if (key == Keys.SelectMedia)
                {
                    //
                }
                else if (key == Keys.Back)
                {
                    if (_currentPlayerName.Length > 0)
                    {
                        _currentPlayerName = _currentPlayerName.Substring(0, _currentPlayerName.Length - 1);
                    }
                }
                else if (key == Keys.Space)
                {
                    if (_currentPlayerName.Length < GameConstants.MaxUsernameLenght)
                        _currentPlayerName += "_";
                }
                else
                {
                    if (_currentPlayerName.Length < GameConstants.MaxUsernameLenght)
                        _currentPlayerName += _virtualKeyboard.Caps ? key.ToString().ToUpper() : key.ToString().ToLower();
                }

                _soundSys.PlayKeyboardClickSound();

                RefreshPlayerName();
            }



            return false;
        }

        private void RecalculateScreenElementSizes()
        {
            _fullScreenRect = new Rectangle(0, 0, _resolution.ScreenWidth, _resolution.ScreenHeight);
        }


        public override void TransitionIn(float transitionPercentage)
        {
            _currentFadeAmount = MathHelper.SmoothStep(0.0f, 0.7f, transitionPercentage);
            _currentColor = Color.Black * _currentFadeAmount;

            _transitionInFinished = transitionPercentage >= 1;
        }

        public override void Draw(GameTime gameTime)
        {
            _resolution.SetupFullViewport();
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null);
            _spriteBatch.Draw(_blankTexture, _fullScreenRect, null, _currentColor);
            _spriteBatch.End();

            _resolution.SetupVirtualScreenViewport();

            _goBackButton.Draw(gameTime);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, _camera.GetViewTransformationMatrix());
            _congratulationsPlayerAlignedText.Draw(_spriteBatch);
            _currentPlayerNameAlignedText.Draw(_spriteBatch);
            _itIsYourNewHighScoreAlignedText.Draw(_spriteBatch);
            _editYourNameAlignedText.Draw(_spriteBatch);
            _toEnterHallOfFameAlignedText.Draw(_spriteBatch);
            _spriteBatch.End();

            _virtualKeyboard.Draw(gameTime);
            base.Draw(gameTime);
        }
    }
}