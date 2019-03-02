using System;
using System.Collections.Generic;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
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
using Roboblob.XNA.WinRT.Rendering;
using Roboblob.XNA.WinRT.ResolutionIndependence;
using Roboblob.XNA.WinRT.Scoreoid;
using System.Linq;

namespace Marbles.UI.GameScreens
{
    public class HighScoresScreen : FloatingMarblesScreen
    {
        private readonly Game _game;
        private Camera2D _camera;
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
        private Vector2 _worldStatusPosition = new Vector2(100, 700);
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
        private SimpleButton _todayButton;
        private SimpleButton _weekButton;
        private SimpleButton _monthButton;
        private SimpleButton _overallButton;
        private SpriteFont _highScoreTitleFont;
        private AlignedText _highScoresAlignedText;
        private int _buttonsXOffset = 400;
        private int _buttonsYOffset = 150;
        private int _buttonsWidth = 150;
        private int _buttonsHeight = 50;
        private int _buttonsMargin = 5;

        private List<String> _loadingMessages = new List<String>()
            {
                "Loading",
                "Loading.",
                "Loading..",
                "Loading...",
                "Loading..",
                "Loading..",
                "Loading.."
            };
        
        private ToggleImageButton _goBackButton;
        private static List<ScoreoidScore> _todaysScores = new List<ScoreoidScore>();
        private HighScoresRetriever _highScoresRetiriever;

        public HighScoresScreen(Game game) : base(game)
        {
            _game = game;
            _camera = Game.Services.GetService(typeof(Camera2D)) as Camera2D;
            _inputHelper = Game.Services.GetService(typeof(InputHelper)) as InputHelper;
            _resolution = Game.Services.GetService(typeof(ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            _touchHelper = game.Services.GetService(typeof(MultitouchHelper)) as MultitouchHelper;
            _gameScreenManager = Game.Services.GetService(typeof(IGameScreenManager)) as IGameScreenManager;
            _gameScreenFactory = Game.Services.GetService(typeof(IMarblesGameScreensFactory)) as IMarblesGameScreensFactory;
            _world = Game.Services.GetService(typeof(MarblesWorld)) as MarblesWorld;
            _spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            _highScoresRetiriever = Game.Services.GetService(typeof (HighScoresRetriever)) as HighScoresRetriever;
           _loadingTextTracker = new CyclicTextTracker(_loadingMessages);
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            _scoresFont = Game.Content.Load<SpriteFont>(GameConstants.HighScoresListFont);

            _highScoreTitleFont = Game.Content.Load<SpriteFont>(GameConstants.GuiFontLarge);
            _highScoresAlignedText = new AlignedText(_highScoreTitleFont, new Vector2(_resolution.VirtualWidth/2, 100), 20);
            _highScoresAlignedText.Color = GameConstants.GuiTextColor;
            _highScoresAlignedText.Text.Append("HIGH SCORES");
            _highScoresAlignedText.RecalculateDrawingPosition();

            _buttonFont = Game.Content.Load<SpriteFont>(GameConstants.VirtualKeyboardFont);

            _goBackButton = new ToggleImageButton(_game, new ToggleButtonSettings()
            {
                Position = new Vector2(100, 100),
                TextureSheetName = @"SpriteSheets\GameArt",
                NormalSubTextureName = "BackButton",
                PressedlSubTextureName = "BackButton"
            });

            _goBackButton.LoadContent();

            _goBackButton.Clicked += OnBackButtonClicked;

            _todayButton = new SimpleButton(Game, _buttonFont, _camera, _resolution, _touchHelper, _inputHelper);
            _todayButton.Text = "TODAY";
            _todayButton.Tag = "DAY";
            _todayButton.HoverColor = _hoverColor;
            _todayButton.IsPushed = true;
            _todayButton.PushedColor = _PushedButtonColor;
            _todayButton.BackgroundColor = GameConstants.GuiTextColor;
            _todayButton.IsPushButton = true;
            _todayButton.SetBackgroundRectangle(new Rectangle(_buttonsXOffset, _buttonsYOffset, _buttonsWidth, _buttonsHeight));
            _todayButton.Clicked += OnTimePeriodButtonClicked;

            _weekButton = new SimpleButton(Game, _buttonFont, _camera, _resolution, _touchHelper, _inputHelper);
            _weekButton.Text = "THIS WEEK";
            _weekButton.HoverColor = _hoverColor;
            _weekButton.Tag = "WEEK";
            _weekButton.PushedColor = _PushedButtonColor;
            _weekButton.BackgroundColor = GameConstants.GuiTextColor;
            _weekButton.IsPushButton = true;
            _weekButton.SetBackgroundRectangle(new Rectangle(_todayButton.BackgroundRectangle.X + _buttonsWidth + _buttonsMargin, _buttonsYOffset, _buttonsWidth, _buttonsHeight));
            _weekButton.Clicked += OnTimePeriodButtonClicked;

            _monthButton = new SimpleButton(Game, _buttonFont, _camera, _resolution, _touchHelper, _inputHelper);
            _monthButton.Text = "THIS MONTH";
            _monthButton.HoverColor = _hoverColor;
            _monthButton.BackgroundColor = GameConstants.GuiTextColor;
            _monthButton.Tag = "MONTH";
            _monthButton.PushedColor = _PushedButtonColor;
            _monthButton.IsPushButton = true;
            _monthButton.SetBackgroundRectangle(new Rectangle(_weekButton.BackgroundRectangle.X + _buttonsWidth + _buttonsMargin, _buttonsYOffset, _buttonsWidth, _buttonsHeight));
            _monthButton.Clicked += OnTimePeriodButtonClicked;

            _overallButton = new SimpleButton(Game, _buttonFont, _camera, _resolution, _touchHelper, _inputHelper);
            _overallButton.Text = "OVERALL";
            _overallButton.Tag = "OVERALL";
            _overallButton.HoverColor = _hoverColor;
            _overallButton.PushedColor = _PushedButtonColor;
            _overallButton.BackgroundColor = GameConstants.GuiTextColor;
            _overallButton.IsPushButton = true;
            _overallButton.SetBackgroundRectangle(new Rectangle(_monthButton.BackgroundRectangle.X + _buttonsWidth + _buttonsMargin, _buttonsYOffset, _buttonsWidth, _buttonsHeight));
            _overallButton.Clicked += OnTimePeriodButtonClicked;

            _loadingMessageAlignedText = new AlignedText(_scoresFont,
                                                           new Vector2(_resolution.VirtualWidth-50, _resolution.VirtualHeight-20), 50);
            _loadingMessageAlignedText.Color = Color.Red;
            _loadingMessageAlignedText.Alignment = TextAlignment.Right;
            _loadingMessageAlignedText.Text.Length = 0;
            _loadingMessageAlignedText.Text.Append(_loadingMessages[0]);
            _loadingMessageAlignedText.RecalculateDrawingPosition();

            WhatWeAreShowing = PeriodWeAreShowing.Today;
            RefreshTodays();
        }

        private PeriodWeAreShowing WhatWeAreShowing = PeriodWeAreShowing.Today;
        private Vector2 _currentTextPos;
        private Vector2 _namesStartPosition = new Vector2(200,250);
        private Vector2 _scoresStartPosition = new Vector2(750, 250);
        private Vector2 _datesStartPosition = new Vector2(900, 250);
        private float _namesLineHeight = 30;
        private float _namesVerticalSpacing = 3;
        private Color _namesColor = GameConstants.GuiTextColor;
        private static List<ScoreoidScore> _weekScores = new List<ScoreoidScore>();
        private static List<ScoreoidScore> _monthScores = new List<ScoreoidScore>();
        private static List<ScoreoidScore> _allScores = new List<ScoreoidScore>();
        private DateTime _startOfWeek;
        private DateTime _endOfWeek;
        private SpriteFont _scoresFont;
        private int MaxNUmberOfItemsToShow = 15;
        private SpriteFont _buttonFont;
        private AlignedText _loadingMessageAlignedText;
        private bool _weAreLoading;
        private int _weAreLoadingCurrentMsg;
        private CyclicTextTracker _loadingTextTracker;

        private enum PeriodWeAreShowing
        {
            Today,
            ThisWeek,
            ThisMonth,
            Overall
        }

        private void OnBackButtonClicked(object sender, EventArgs e)
        {
            if (PreviousScreen == PreviousScreen.MainMenu)
            {
                _gameScreenManager.ChangeScreen(_gameScreenFactory.CreateMainMenuScreen());
            }
            else
            {
                _gameScreenManager.ChangeScreen(_gameScreenFactory.CreateSurvivalModeGameScreen());
            }           
        }

        private void OnTimePeriodButtonClicked(object sender, EventArgs e)
        {
            UnPushOthers(sender);

            if (sender == _todayButton)
            {
                if (WhatWeAreShowing != PeriodWeAreShowing.Today)
                {
                    WhatWeAreShowing = PeriodWeAreShowing.Today;
                    RefreshTodays();
                }
            }
            else if (sender == _weekButton)
            {
                if (WhatWeAreShowing != PeriodWeAreShowing.ThisWeek)
                {
                    WhatWeAreShowing = PeriodWeAreShowing.ThisWeek;
                    RefreshThisWeek();
                }
            }
            else if (sender == _monthButton)
            {
                if (WhatWeAreShowing != PeriodWeAreShowing.ThisMonth)
                {
                    WhatWeAreShowing = PeriodWeAreShowing.ThisMonth;
                    RefreshThisMonth();
                }
            }
            else if (sender == _overallButton)
            {
                if (WhatWeAreShowing != PeriodWeAreShowing.Overall)
                {
                    WhatWeAreShowing = PeriodWeAreShowing.Overall;
                    RefreshOverall();
                }
            }
        }

        private async void RefreshOverall()
        {
            _weAreLoading = true;
            var scores = await _highScoresRetiriever.GetAll();
            _allScores = scores.Take(MaxNUmberOfItemsToShow).ToList();
            _weAreLoading = false;
        }

        private async void RefreshThisMonth()
        {
            _weAreLoading = true;
            var scores = await _highScoresRetiriever.GetThisMonth();
            _monthScores = scores.Take(MaxNUmberOfItemsToShow).ToList();
            _weAreLoading = false;
        }

        private async void RefreshTodays()
        {
            _weAreLoading = true;
            var scores = await _highScoresRetiriever.GetTodays();
            _todaysScores = scores.Take(MaxNUmberOfItemsToShow).ToList();
            _weAreLoading = false;
        }

        private async void RefreshThisWeek()
        {
            _weAreLoading = true;
            var scores = await _highScoresRetiriever.GetThisWeek();
            _weekScores = scores.Take(MaxNUmberOfItemsToShow).ToList();
            _weAreLoading = false;
        }

        private void UnPushOthers(object senderButton)
        {
            _todayButton.IsPushed = (_todayButton == senderButton);
            _weekButton.IsPushed = (_weekButton == senderButton);
            _monthButton.IsPushed = (_monthButton == senderButton);
            _overallButton.IsPushed = (_overallButton == senderButton);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            _todayButton.Update(gameTime);
            _weekButton.Update(gameTime);
            _monthButton.Update(gameTime);
            _overallButton.Update(gameTime);
            _goBackButton.Update(gameTime);

            if (_weAreLoading)
            {
                _loadingTextTracker.Update(gameTime);
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
           
            _todayButton.Draw(gameTime);
            _weekButton.Draw(gameTime);
            _monthButton.Draw(gameTime);
            _overallButton.Draw(gameTime);

            _goBackButton.Draw(gameTime);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.Default, RasterizerState.CullNone, null, _camera.GetViewTransformationMatrix());

            _highScoresAlignedText.Draw(_spriteBatch);

            if (_weAreLoading)
            {
                DrawLoadingMsg(_spriteBatch);
            }

            DrawScores();
            
            _spriteBatch.End();


        }

        private void DrawLoadingMsg(SpriteBatch spriteBatch)
        {
            _loadingMessageAlignedText.Text.Length = 0;
            _loadingMessageAlignedText.Text.Append(_loadingTextTracker.Items[_loadingTextTracker.CurrentTextIndex]);
            _loadingMessageAlignedText.Draw(spriteBatch);
        }

        private void DrawScores()
        {
            var currentScores = GetCurrentScoresWeAreShowing();

            for (int i = 0; i < currentScores.Count; i++)
            {
                _currentTextPos.X = _namesStartPosition.X;
                _currentTextPos.Y = _namesStartPosition.Y + (i * _namesLineHeight) + _namesVerticalSpacing;
                _spriteBatch.DrawString(_scoresFont, currentScores[i].PlayerName, _currentTextPos, _namesColor);

                _currentTextPos.X = _scoresStartPosition.X;
                _currentTextPos.Y = _scoresStartPosition.Y + (i * _namesLineHeight) + _namesVerticalSpacing;

                _spriteBatch.DrawString(_scoresFont, currentScores[i].Score.ToString(), _currentTextPos, _namesColor);

                _currentTextPos.X = _datesStartPosition.X;
                _currentTextPos.Y = _datesStartPosition.Y + (i * _namesLineHeight) + _namesVerticalSpacing;

                _spriteBatch.DrawString(_scoresFont, currentScores[i].Created.ToString("dd/MM/yyyy"), _currentTextPos, _namesColor);
            }
        }

        private List<ScoreoidScore> GetCurrentScoresWeAreShowing()
        {
            if (WhatWeAreShowing == PeriodWeAreShowing.Today)
            {
                return _todaysScores;
            }
            if (WhatWeAreShowing == PeriodWeAreShowing.ThisWeek)
            {
                return _weekScores;
            }
            if (WhatWeAreShowing == PeriodWeAreShowing.ThisMonth)
            {
                return _monthScores;
            }
            if (WhatWeAreShowing == PeriodWeAreShowing.Overall)
            {
                return _allScores;
            }

            return null;
        }

        public PreviousScreen PreviousScreen = PreviousScreen.MainMenu;
        private Color _PushedButtonColor = Color.Orange;
        private Color _hoverColor = Color.Red;
        private bool _weStartedMenuLoopMusic;
    }

    public enum PreviousScreen
    {
        MainMenu,
        GameScreen
    }
}