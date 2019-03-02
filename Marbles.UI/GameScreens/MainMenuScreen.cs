using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Repositories;
using Marbles.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.Content;
using Roboblob.XNA.WinRT.GameStateManagement;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems.ParticleEmitters;
using Roboblob.XNA.WinRT.Input;
using Roboblob.XNA.WinRT.MenuSystem;
using Roboblob.XNA.WinRT.ResolutionIndependence;

namespace Marbles.UI.GameScreens
{
    public class MainMenuScreen : FloatingMarblesScreen
    {
        private readonly Game _game;
        private Camera2D _camera;
        private SpriteFont _font;
        private TextMenu _menu;
        private ResolutionIndependentRenderer _resolution;
        private MultitouchHelper _touchHelper;
        private IGameScreenManager _gameScreenManager;
        private IMarblesGameScreensFactory _gameScreenFactory;
        private InputHelper _inputHelper;
        private SpriteBatch _spriteBatch;
        private Rectangle _fullScreenRectangle;
        private MarblesWorld _world;
        private string _worldStatusString = string.Empty;
        private Vector2 _worldStatusPosition = new Vector2(100,700);
        private string _runningText = "Ready";
        private string _notRunningText = "Loading...";
        private ParticleSystem _marblesParticleSystem;
        private Rectangle _rectangleForNewParticles;
        private Rectangle _virtualScreenRectangle;
        private ITextureSheetLoader _textureSheetLoader;
        private TextureSheet _gameArtTextureSheet;
        private Rectangle _menuBgRect;
        private Texture2D _menuBgTileTexture;
        private Texture2D _logoTexture;
        private Vector2 _logoPos;
        private RectangleConstantEmitter _emitter;
        private bool _weStartedRendering;
        private bool _weDidNotInitializeWorld = true;
        private bool _worldIsInitialized;
        private IExceptionPopupHelper _exceptionPopup;
        private Vector2 _loadingTextPos = new Vector2(1000, 600);
        private string _loadingString = "Loading...";
        private bool _worldContentIsLoaded;
        private bool _weStartedMenuLoopMusic;
        private MarbleSoundsSystem _soundSys;
        private GameSettingsRepository _settings;
        private ToggleImageButton _soundOnOffImageButton;
        private bool _isSoundEnabled = true;
        private bool _settingsIsLoaded;

        private bool GameIsReadyToPlay 
        {
            get { return _worldIsInitialized && _settingsIsLoaded && _worldContentIsLoaded; }
        }

        public MainMenuScreen(Game game) : base(game)
        {
            _game = game;
            _spriteBatch = new SpriteBatch(_game.GraphicsDevice);
            _camera = Game.Services.GetService(typeof (Camera2D)) as Camera2D;
            _inputHelper = Game.Services.GetService(typeof(InputHelper)) as InputHelper;
            _resolution = Game.Services.GetService(typeof(ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            _touchHelper = game.Services.GetService(typeof(MultitouchHelper)) as MultitouchHelper;
            _gameScreenManager = Game.Services.GetService(typeof(IGameScreenManager)) as IGameScreenManager;
            _gameScreenFactory = Game.Services.GetService(typeof(IMarblesGameScreensFactory)) as IMarblesGameScreensFactory;
            _world = Game.Services.GetService(typeof(MarblesWorld)) as MarblesWorld;
            _exceptionPopup = Game.Services.GetService(typeof(IExceptionPopupHelper)) as IExceptionPopupHelper;
            
            _settings = game.Services.GetService(typeof (GameSettingsRepository)) as GameSettingsRepository;
            if (!_settings.IsLoaded)
            {
                _settings.Loaded += OnSettingsLoaded;
            }
            else
            {
                OnSettingsLoaded(this, EventArgs.Empty);               
            }

            CalucalteScreenElementSizes();

            _textureSheetLoader = game.Services.GetService(typeof(ITextureSheetLoader)) as ITextureSheetLoader;

            if (!_world.IsInitialized)
            {
                _worldIsInitialized = false;
                _worldContentIsLoaded = false;
                (_game as MarblesGame).InitWorld();
                _worldIsInitialized = true;
            }
            else
            {
                _worldIsInitialized = true;
                _worldContentIsLoaded = true;
            }
        }

        private void OnSettingsLoaded(object sender, EventArgs e)
        {
            if (_soundOnOffImageButton != null)
            {
                _isSoundEnabled = _settings.Settings.SoundsEffectsEnabled;
            }
            _settingsIsLoaded = true;
        }

        protected override void UnloadContent()
        {
            Debug.WriteLine("Unloading content");
            base.UnloadContent();
        }

        private void CalucalteScreenElementSizes()
        {
            if (_menuBgTileTexture != null)
            {
                _menuBgRect = new Rectangle(0, 0, _menuBgTileTexture.Width, _menuBgTileTexture.Height);
            }

            if (_logoTexture != null)
                _logoPos = new Vector2((_resolution.ScreenWidth - _logoTexture.Width) / 2, 0f);

        }

        protected override void LoadContent()
        {
            base.LoadContent();

            _font = Game.Content.Load<SpriteFont>(@"SpriteFonts\WhiteRabbitDropshadow");

            _menu = new TextMenu(_game, _camera, _font, new Vector2(_resolution.VirtualWidth / 2f, _resolution.VirtualHeight / 2f), _touchHelper, _resolution, _inputHelper);
            _menu.MenuTextColor = GameConstants.GuiTextColor;
            _menu.AddItem(new MenuItem() { Text = new StringBuilder("Play"), OnItemChosen = () => {
                if (!GameIsReadyToPlay)
                    {
                        return;
                    }
                    GoToSurvivalModeGameScreen();
                }});
//            _menu.AddItem(new MenuItem() { Text = new StringBuilder("How To Play"), OnItemChosen = () => Debug.WriteLine("item3") });

            _menu.AddItem(new MenuItem() { Text = new StringBuilder("High Scores"), OnItemChosen = () =>
                                                                                                       {
                                                                                                           GoToHighScoresGameScreen();
                                                                                                           Debug.WriteLine("item3");
                                                                                                       }
                                         });


            _menu.LoadContent();

            _logoTexture = _game.Content.Load<Texture2D>(@"Backgrounds\LogoOriginal");

            _textureSheetLoader.Load(@"SpriteSheets\GameArt");

            CalucalteScreenElementSizes();

            if (!_world.ContentIsLoaded)
            {
                _worldContentIsLoaded = false;
                try
                {
                    _world.LoadContent();
                    _worldContentIsLoaded = true;
                }
                catch (Exception e)
                {
                    _worldContentIsLoaded = false;
                    _exceptionPopup.ShowExceptionPopup(e);
                }
            }

            _soundOnOffImageButton = new ToggleImageButton(_game, new ToggleButtonSettings()
            {
                Position = new Vector2(_resolution.VirtualWidth*0.1f, _resolution.VirtualHeight * 0.9f),
                TextureSheetName = @"SpriteSheets\GameArt",
                NormalSubTextureName = "SoundsButton-On",
                PressedlSubTextureName = "SoundsButton-Off"
            });
            _soundOnOffImageButton.LoadContent();
            _soundOnOffImageButton.Clicked += OnSoundBtnClicked;
        }

        private async void OnSoundBtnClicked(object sender, ButtonClickedEventArgs e)
        {
            if (_settingsIsLoaded)
            {
                _settings.Settings.SoundsEffectsEnabled = !_settings.Settings.SoundsEffectsEnabled;
                await _settings.Save();
                _soundOnOffImageButton.IsPressed = !_settings.Settings.SoundsEffectsEnabled;
                if (_settings.Settings.SoundsEffectsEnabled)
                {
                    _soundSys.StartPlayingMenuLoop();
                }
                else
                {
                    _soundSys.StopPlayingMenuLoop();
                }
            }
        }

        private void GoToSurvivalModeGameScreen()
        {
            _soundSys.StopPlayingMenuLoop();
            var arcadeModeGameScreen = _gameScreenFactory.CreateSurvivalModeGameScreen();
            _gameScreenManager.ChangeScreen(arcadeModeGameScreen);
        }

        private void GoToHighScoresGameScreen()
        {
            var screen = _gameScreenFactory.CreateHighScoresScreen();
            _gameScreenManager.ChangeScreen(screen);
        }

        public override void OnClosed()
        {
            base.OnClosed();
            if (_settings != null)
            {
                _settings.Loaded -= OnSettingsLoaded;
            }
        }

        public override void Update(GameTime gameTime)
        {            
            if (GameIsReadyToPlay)
            {
                _worldStatusString = _runningText;
                if (_soundOnOffImageButton != null)
                {
                    _soundOnOffImageButton.IsPressed = !_settings.Settings.SoundsEffectsEnabled;
                }

                if (!_weStartedMenuLoopMusic)
                {
                    _weStartedMenuLoopMusic = true;
                    _soundSys = _world.GetSystem<MarbleSoundsSystem>();
                    _soundSys.StartPlayingMenuLoop();                    
                }
            }
            else
            {
                _worldStatusString = _notRunningText;
            }

            if (gameTime.IsRunningSlowly)
            {
                Debug.WriteLine("Slow from main menu screen" + DateTime.Now);
            }

            if (_menu == null)
            {
                return;
            }

            _menu.Update(gameTime);
            if (_soundOnOffImageButton != null)
                _soundOnOffImageButton.Update(gameTime);
            base.Update(gameTime);

        }

        public override void Draw(GameTime gameTime)
        {
            _weStartedRendering = true;
            base.Draw(gameTime); 

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.Default, RasterizerState.CullNone);

            _spriteBatch.Draw(_logoTexture, _logoPos, Color.White);

            _spriteBatch.DrawString(_font, GameIsReadyToPlay ? string.Empty : _loadingString, _loadingTextPos, Color.White);

            _spriteBatch.End();

            _resolution.SetupVirtualScreenViewport();
            _camera.RecalculateTransformationMatrices();

            if (_menu == null)
            {
                return;
            }

            _menu.Draw(gameTime);         
            _soundOnOffImageButton.Draw(gameTime);
        }

        public override void OnScreenSizeChanged()
        {
            CalucalteScreenElementSizes();
            base.OnScreenSizeChanged();
        }
    }
}