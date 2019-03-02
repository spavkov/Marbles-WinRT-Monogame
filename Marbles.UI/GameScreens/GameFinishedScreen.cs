using System.Diagnostics;
using System.Text;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Levels;
using Marbles.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.GameStateManagement;
using Roboblob.XNA.WinRT.Input;
using Roboblob.XNA.WinRT.Mathematics;
using Roboblob.XNA.WinRT.MenuSystem;
using Roboblob.XNA.WinRT.Rendering;
using Roboblob.XNA.WinRT.ResolutionIndependence;
using Scoreoid;

namespace Marbles.UI.GameScreens
{
    public class GameFinishedScreen : GameScreen
    {
        private const float MaximumFadeAmount = 0.7f;
        private Camera2D _camera;
        private ResolutionIndependentRenderer _resolution;
        private MultitouchHelper _touchHelper;
        private IGameScreenManager _gameScreenManager;
        private IMarblesGameScreensFactory _gameScreenFactory;
        private SpriteBatch _spriteBatch;

        private SpriteFont _font;
        private TextMenu _levelFailedMenu;
        private InputHelper _inputHelper;
        private string _notBadTextFormat = "Not Bad {0}";
        private string _youScoredTextFormat = "You Scored {0} but your High Score is {1}";

        public GameFinishedScreen(Game game) : base(game)
        {
            _camera = game.Services.GetService(typeof(Camera2D)) as Camera2D;
            _resolution = Game.Services.GetService(typeof(ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            _camera.SetPosition(new Vector2(_resolution.VirtualWidth / 2, _resolution.VirtualHeight / 2));
            _touchHelper = game.Services.GetService(typeof(MultitouchHelper)) as MultitouchHelper;
            _gameScreenManager = Game.Services.GetService(typeof(IGameScreenManager)) as IGameScreenManager;
            _gameScreenFactory = Game.Services.GetService(typeof(IMarblesGameScreensFactory)) as IMarblesGameScreensFactory;
            _inputHelper = Game.Services.GetService(typeof(InputHelper)) as InputHelper;
            _pleayerScoreData = game.Services.GetService(typeof(PlayersHighScoresSynchronizer)) as PlayersHighScoresSynchronizer;

            _world = game.Services.GetService(typeof (MarblesWorld)) as MarblesWorld;
            _gameInfoSys = _world.GetSystem<CurrentGameInformationTrackingSystem>();
            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();
            
            _soundSys = _world.GetSystem<MarbleSoundsSystem>();

            HasInTransition = true;
            TransitionInDesiredDurationInSeconds = 0.5f;
        }

        public override void OnShown()
        {
            base.OnShown();

            if (ScreenUsageType == GameFinishedScreenType.GameFailed)
                _soundSys.PlayLevelFailedSound();
        }

        public GameFinishedScreenType ScreenUsageType;
        private TextMenu _currentMenu;
        private Texture2D _blankTexture;
        private Rectangle _fullScreenRect;
        private float _currentFadeAmount;
        private AlignedText _notBadAlignedText;
        private AlignedText _youScoredAlignedText;
        private MarblesWorld _world;
        private CurrentGameInformationTrackingSystem _gameInfoSys;
        private bool _transitionInFinished;
        private Color _currentColor;
        private MarbleGameLevelControllerSystem _controller;
        private string _completedMessage = "Success!";
        private AlignedText _completedMessageAlignedText;
        private MarbleSoundsSystem _soundSys;
        private PlayersHighScoresSynchronizer _pleayerScoreData;
        private AlignedText _tipsLine1;
        private AlignedText _tipsLine2;
        private AlignedText _tipsLine3;
        private AlignedText _tipsLine4;
        private AlignedText _tipsLine5;
        private AlignedText _tipsLine6;

        public string CompletedMessage
        {
            get { return _completedMessage; }
            set
            {
                _completedMessage = value;
            }
        }

        public override void TransitionIn(float transitionPercentage)
        {
            _currentFadeAmount = MathHelper.SmoothStep(0.0f, MaximumFadeAmount, transitionPercentage);

            if (_currentFadeAmount > MaximumFadeAmount)
            {
                _currentFadeAmount = MaximumFadeAmount;
            }

            _currentColor = Color.Black * _currentFadeAmount;

            _transitionInFinished = transitionPercentage >= 1;
        }

        public override void OnClosed()
        {
            base.OnClosed();
        }

        public override void OnScreenChanged(object sender, System.EventArgs e)
        {
            base.OnScreenChanged(sender, e);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            _font = Game.Content.Load<SpriteFont>(GameConstants.MenuFont);
            _levelFailedMenu = new TextMenu(Game, _camera, _font, new Vector2(_resolution.VirtualWidth / 2f, _resolution.VirtualHeight / 2f + 200), _touchHelper, _resolution, _inputHelper);
            _levelFailedMenu.AddItem(new MenuItem()
            {
                Text = new StringBuilder("Try Again"),
                OnItemChosen = () =>
                {                 
                    RestartCurrentLevel();
                }
            });
            _levelFailedMenu.AddItem(new MenuItem() { Text = new StringBuilder("High Scores"), OnItemChosen = () =>
                {
                    var screen = _gameScreenFactory.CreateHighScoresScreen();
                    _soundSys.StartPlayingMenuLoop();
                    screen.PreviousScreen = PreviousScreen.GameScreen;
                    _gameScreenManager.ChangeScreen(screen);
                }
            });

            _levelFailedMenu.LoadContent();

            _currentMenu = _levelFailedMenu;

            _blankTexture = new Texture2D(Game.GraphicsDevice, 1, 1);
            var color = new Color[1];
            color[0] = Color.White;
            _blankTexture.SetData<Color>(color);

            _notBadAlignedText = new AlignedText(_font, new Vector2(_resolution.VirtualWidth/2, 50), 100);
            _youScoredAlignedText = new AlignedText(_font, new Vector2(_resolution.VirtualWidth/2, 100), 100);

            _tipsLine1 = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 200), 100);
            _tipsLine2 = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 250), 100);
            _tipsLine3 = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 300), 100);
            _tipsLine4 = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 350), 100);
            _tipsLine5 = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 400), 100);
            _tipsLine6 = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 450), 100);

            _tipsLine1.Text.AppendFormat("Tips for higher score:");
            _tipsLine1.RecalculateDrawingPosition();

            _tipsLine2.Text.AppendFormat("Try to connect marbles of same color ");
            _tipsLine2.RecalculateDrawingPosition();

            _tipsLine3.Text.AppendFormat("That form basic geometric shapes");
            _tipsLine3.RecalculateDrawingPosition();

            _tipsLine4.Text.AppendFormat("Like triangles, Squares and Rectangles");
            _tipsLine4.RecalculateDrawingPosition();

            _tipsLine5.Text.AppendFormat("To get powerups you need to close the shape");
            _tipsLine5.RecalculateDrawingPosition();

            _tipsLine6.Text.AppendFormat("With same marble that started it");
            _tipsLine6.RecalculateDrawingPosition();

            _completedMessageAlignedText = new AlignedText(_font, new Vector2(_resolution.VirtualWidth / 2, 200), 100);
            _completedMessageAlignedText.Text.AppendFormat(_completedMessage);
            _completedMessageAlignedText.RecalculateDrawingPosition();

            _notBadAlignedText.Text.Length = 0;
            _notBadAlignedText.Text.AppendFormat(_notBadTextFormat, _gameInfoSys.CurrentPlayerName);
            _notBadAlignedText.RecalculateDrawingPosition();

            _youScoredAlignedText.Text.Length = 0;
            _youScoredAlignedText.Text.AppendFormat(_youScoredTextFormat, _gameInfoSys.CurrentLevelScore, _pleayerScoreData.GetCurrentPlayerHighScore());
            _youScoredAlignedText.RecalculateDrawingPosition();

            base.LoadContent();

            RecalculateScreenElementSizes();
        }

        private void RestartCurrentLevel()
        {
            _gameScreenManager.PopScreen();
            _controller.RestartCurrentLevel();

        }

        public override void Update(GameTime gameTime)
        {
            if (_currentMenu == null)
            {
                return;
            }

            _currentMenu.Update(gameTime);
            base.Update(gameTime);
        }

        private void RecalculateScreenElementSizes()
        {
            _fullScreenRect = new Rectangle(0, 0, _resolution.ScreenWidth, _resolution.ScreenHeight);
        }

        public override void OnScreenSizeChanged()
        {
            RecalculateScreenElementSizes();
            base.OnScreenSizeChanged();
        }

        public override void Draw(GameTime gameTime)
        {
            if (_currentMenu == null)
            {
                return;
            }

            _resolution.SetupFullViewport();
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null);
            _spriteBatch.Draw(_blankTexture, _fullScreenRect, null, _currentColor);
            _spriteBatch.End();

            _resolution.SetupVirtualScreenViewport();

            if (ScreenUsageType == GameFinishedScreenType.GameFailed)
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, _camera.GetViewTransformationMatrix());

                _notBadAlignedText.Draw(_spriteBatch);
                _youScoredAlignedText.Draw(_spriteBatch);

                _tipsLine1.Draw(_spriteBatch);
                _tipsLine2.Draw(_spriteBatch);
                _tipsLine3.Draw(_spriteBatch);
                _tipsLine4.Draw(_spriteBatch);
                _tipsLine5.Draw(_spriteBatch);
                _tipsLine6.Draw(_spriteBatch);

                _spriteBatch.End();
            }
            else
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, _camera.GetViewTransformationMatrix());
                _completedMessageAlignedText.Draw(_spriteBatch);
                _spriteBatch.End();               
            }

            _currentMenu.Draw(gameTime);
            base.Draw(gameTime);
        }
    }

    public enum GameFinishedScreenType
    {
        GameFailed,
        AfterHighscoreScreen,
    }
}