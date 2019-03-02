using System.Diagnostics;
using System.Text;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.Content;
using Roboblob.XNA.WinRT.GameStateManagement;
using Roboblob.XNA.WinRT.Input;
using Roboblob.XNA.WinRT.Mathematics;
using Roboblob.XNA.WinRT.MenuSystem;
using Roboblob.XNA.WinRT.Rendering;
using Roboblob.XNA.WinRT.ResolutionIndependence;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Marbles.UI.GameScreens
{
    public class GamePausedScreen : GameScreen
    {
        private Camera2D _camera;
        private ResolutionIndependentRenderer _resolution;
        private MultitouchHelper _touchHelper;
        private IGameScreenManager _gameScreenManager;
        private IMarblesGameScreensFactory _gameScreenFactory;
        private SpriteBatch _spriteBatch;

        private SpriteFont _font;
        private TextMenu _menu;
        private InputHelper _inputHelper;
        private Rectangle _fullScreenRect;
        private ITextureSheetLoader _textureSheetLoader;
        private TextureSheet _gameArt;
        private Texture2D _blankTexture;
        private static int _menuWidth = 600;
        private static int _menuHeight = 337;
        private Rectangle _menuBackgroundRect = new Rectangle(5000,5000, 600, 337);
        private Texture2D _menuBgTileTexture;
        private Rectangle _menuBgRect;
        private Vector2 _menuPos;
        private float _currentFadeAmount = 0.0f;
        private bool _transitionInStarted;
        private float _transitionTimeElapsedSoFarInSeconds;
        private float _desiredTransitionInDuration = 0.5f;
        private int _startMenuX;
        private int _endMenuX;
        private bool _transitionInFinished;
        private Texture2D _grayBlankTexture;
        private ColoredBackgroundRenderer _coloredBackgroundRenderer;
        private int _menuPosY;
        private Color _currentColor;
        private MarblesWorld _world;
        private MarbleGameLevelControllerSystem _controller;

        public GamePausedScreen(Game game) : base(game)
        {
            _camera = game.Services.GetService(typeof(Camera2D)) as Camera2D;
            _resolution = Game.Services.GetService(typeof(ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            _camera.SetPosition(new Vector2(_resolution.VirtualWidth / 2, _resolution.VirtualHeight / 2));
            _touchHelper = game.Services.GetService(typeof(MultitouchHelper)) as MultitouchHelper;
            _gameScreenManager = Game.Services.GetService(typeof(IGameScreenManager)) as IGameScreenManager;
            _gameScreenFactory = Game.Services.GetService(typeof(IMarblesGameScreensFactory)) as IMarblesGameScreensFactory;
            _inputHelper = Game.Services.GetService(typeof(InputHelper)) as InputHelper;
            _textureSheetLoader = game.Services.GetService(typeof(ITextureSheetLoader)) as ITextureSheetLoader;
            _world = game.Services.GetService(typeof (MarblesWorld)) as MarblesWorld;
            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();

            _coloredBackgroundRenderer = new ColoredBackgroundRenderer(game);
            _coloredBackgroundRenderer.Color = Color.FromNonPremultiplied(252, 109, 38, 200);
            _coloredBackgroundRenderer.BorderColor = Color.FromNonPremultiplied(235, 78, 35, 200);
            _coloredBackgroundRenderer.BorderWidth = 2;

            RecalculateScreenElementSizes();

            _blankTexture = new Texture2D(game.GraphicsDevice, 1, 1);
            var color = new Color[1];
            color[0] = Color.White;
            _blankTexture.SetData<Color>(color);
            HasInTransition = true;
            TransitionInDesiredDurationInSeconds = 1f;

            _grayBlankTexture = new Texture2D(game.GraphicsDevice, 1, 1);
            color[0] = Color.Gray;
            _grayBlankTexture.SetData<Color>(color);
        }

        public override void TransitionIn(float transitionPercentage)
        {
            _currentFadeAmount = MathHelper.SmoothStep(0.0f, 0.5f, transitionPercentage);
            _currentColor = Color.Black*_currentFadeAmount;
            var currentMenuX = TweenHelper.Calculate(_startMenuX, _endMenuX, transitionPercentage, ScaleFuncs.QuinticEaseOut);

            _coloredBackgroundRenderer.Position = new Vector2(currentMenuX, _menuPosY);
            //_menuBackgroundRect = new Rectangle(currentMenuX, , _menuWidth, _menuHeight);

            _transitionInFinished = transitionPercentage >= 1;
        }

        public override void OnClosed()
        {
            base.OnClosed();
            _currentFadeAmount = 0.0f;
            _menuBackgroundRect = new Rectangle(_startMenuX, (_resolution.VirtualHeight - _menuHeight) / 2, _menuWidth, _menuHeight);
        }

        private void RecalculateScreenElementSizes()
        {
            _fullScreenRect = new Rectangle(0, 0, _resolution.ScreenWidth, _resolution.ScreenHeight);
            _menuWidth = _resolution.VirtualWidth / 3;
            _menuHeight = _resolution.VirtualHeight / 3;

            _startMenuX = _resolution.VirtualWidth + 10;
            _endMenuX = (_resolution.VirtualWidth - _menuWidth) / 2;
            _menuPosY = (_resolution.VirtualHeight - _menuHeight) / 2;

            _coloredBackgroundRenderer.Position = new Vector2(_resolution.VirtualWidth + 10, 95);
            _coloredBackgroundRenderer.Width = _menuWidth;
            _coloredBackgroundRenderer.Height = _menuHeight;
        }

        public override void OnScreenSizeChanged()
        {
            RecalculateScreenElementSizes();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            _font = Game.Content.Load<SpriteFont>(GameConstants.MenuFont);
            _menu = new TextMenu(Game, _camera, _font, new Vector2(_resolution.VirtualWidth / 2f, _resolution.VirtualHeight / 2f), _touchHelper, _resolution, _inputHelper);
            _menu.AddItem(new MenuItem()
            {
                Text = new StringBuilder("Resume"),
                OnItemChosen = () =>
                                   {
                                       Debug.WriteLine("Resume");
                                       ReturnToGameAndUnpause();
                                   }
            });

            _menu.AddItem(new MenuItem()
            {
                Text = new StringBuilder("Restart"),
                OnItemChosen = () =>
                {
                    Debug.WriteLine("Restart");
                    ReturnToGameAndRestartLevel();
                }
            });

            _menu.AddItem(new MenuItem() { Text = new StringBuilder("Main Menu"), OnItemChosen = () => StopCurrentGameGoToMainMenuScreen() });

            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);

            _gameArt = _textureSheetLoader.Load(@"SpriteSheets\GameArt");

            _menu.LoadContent();

            _menuBgTileTexture = Game.Content.Load<Texture2D>(GameConstants.RollingBackgroundTileName);
            _menuBgRect = new Rectangle(0, 0, _menuBgTileTexture.Width, _menuBgTileTexture.Height);

            _menuPos = new Vector2(_resolution.ScreenWidth - (_resolution.ScreenWidth / 2), 100);

            base.LoadContent();
        }

        private void StopCurrentGameGoToMainMenuScreen()
        {
            _gameScreenManager.PopScreen();
/*
            var currentGameScreen = _gameScreenManager.GetCurrentScreen<IMarblesGameScreen>();
            if (currentGameScreen != null)
            {
                currentGameScreen.StopCurrentGame();
            }
*/

            if (_controller != null)
            {
                _controller.StopLevel();
            }

            _gameScreenManager.ChangeScreen(_gameScreenFactory.CreateMainMenuScreen());
        }

        private void ReturnToGameAndUnpause()
        {
            _gameScreenManager.PopScreen();

            if (_controller != null)
            {
                _controller.ResumeCurrentLevel();
            }
        }


        private void ReturnToGameAndRestartLevel()
        {
            _gameScreenManager.PopScreen();
/*            var currentGameScreen = _gameScreenManager.GetCurrentScreen<IMarblesGameScreen>();
            if (currentGameScreen != null)
            {
                currentGameScreen.RestartLevel();
            }*/

            if (_controller != null)
            {
                _controller.RestartCurrentLevel();
            }
        }

        public override void Update(GameTime gameTime)
        {
            _menu.Update(gameTime);
            if (_gameScreenManager.CurrentTopmostScreen == this)
            {
                if (_inputHelper.IsNewPress(Keys.P) || _inputHelper.IsNewPress(Keys.Escape))
                {
                    ReturnToGameAndUnpause();
                }
            }
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            _resolution.SetupFullViewport();
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);
            _spriteBatch.Draw(_blankTexture, Vector2.Zero, _fullScreenRect, _currentColor);
            _spriteBatch.End();

            _resolution.SetupVirtualScreenViewport();
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
                                DepthStencilState.None, RasterizerState.CullNone, null,
                                _camera.GetViewTransformationMatrix());

            _coloredBackgroundRenderer.Draw(gameTime, _spriteBatch);

            _spriteBatch.End();

            if (_transitionInFinished)
                _menu.Draw(gameTime);
        }
    }
}