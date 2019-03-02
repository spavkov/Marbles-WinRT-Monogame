using System;
using System.Collections.Generic;
using System.Text;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Components.SpecialMarbles;
using Marbles.Core.Model.Levels;
using Marbles.Core.Repositories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roboblob.GameEntitySystem.WinRT;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.Content;
using Roboblob.XNA.WinRT.Input;
using Roboblob.XNA.WinRT.Rendering;
using Roboblob.XNA.WinRT.Performance;

namespace Marbles.Core.Systems
{
    public class GuiRendererSystem : IWorldRenderingSystem
    {
        private const int _multiplierBarWidth = 20;
        private World _world;
        private Game _game;
        private ITextureSheetLoader _textureSheetLoader;
        private ToggleImageButton _pauseImageButton;
        private ToggleImageButton _soundOnOffImageButton;
        private MarbleGameLevelControllerSystem _controller;
        private CurrentGameInformationTrackingSystem _gameStateSys;
        private GameSettingsRepository _settingsRepository;
        private bool _soundFxDisabled = true;
        private SpriteBatch _spriteBatch;
        private Camera2D _cam;
        private StringBuilder _remainingTimeText = new StringBuilder("Time:");
        private Vector2 _remainingTimeTextPos = new Vector2(380,51);
        private Vector2 _remainingSecondsTextPos = new Vector2(550, 70);
        //private string _remainingSecondsFormat = "{0:0}";
        private AlignedText _remainingSecondsAlignedText;
        private Vector2 _currentScoreTitleTextPos = new Vector2(0, 0);
        private Vector2 _currentScoreValueTextPos = new Vector2(900, 51);
        private StringBuilder _currentScoreTitleText = new StringBuilder("Current Level Score:");

        private Vector2 _requiredScoreTitleTextPos = new Vector2(0,0);
        private StringBuilder _requiredScoreTitleText = new StringBuilder("Level Required Score:");
        private Vector2 _requiredScoreValueTextPos = new Vector2(0, 0);

        private StringBuilder _multiplierTitleText = new StringBuilder("Multiplier");
        private Vector2 _multiplierTitlePos = new Vector2(1070, 360);
        private Vector2 _playerTitlePos = new Vector2(100, 350);
        private Vector2 _multiplierValuePos = new Vector2(1150, 410);

        private string _playerTitle = "Player:";

        private AlignedText _currentPlayerNameAlignedText;
        private Vector2 _currentPlayerNameCenteredTextPos = new Vector2(190, 420);

        private AlignedText _currentPlayerHighScoreAlignedText;
        private Vector2 _currentPlayerHighScoreCenteredTextPos = new Vector2(780, 680);

        private StringBuilder _highScoreTitleText = new StringBuilder("High Score:");
        private Vector2 _highScoreTitleTextPos = new Vector2(500, 680);

        private AlignedText _requiredScoreValueAlignedText;
        private double _currentMultiplierLevel = 0;
        private TextureSheet _gameArtSheet;
        //private Rectangle _multiplierFullGradientRect;
        private Vector2 _fullMultiplierDrawPosition = new Vector2(1181,134);
        private Rectangle _currentMultiplierDestRect;
        private Rectangle _currentMultiplierSrcRect = new Rectangle();
        private ToggleImageButton _restartImageButton;
        private int _originalMultiplierBarImageWidth = 58;
        private float _desiredMultiplierFullHeight = 400;
        private Dictionary<string, Vector2> _multiplierMarks = new Dictionary<string, Vector2>();
        private AlignedText _currentGameOverTimerCenterdText;
        private Aspect _specialMarblesWithScreenComponentAspect;
        private string _kaboomText = "Kaboom!";
        private float _currentScore = 0;
        private float _scoreCatchingUpSpeedInPointsPerSecond = 100;
        private SpriteFont _guiBigFont;
        private Color _guiTextColor = GameConstants.GuiTextColor;
        private StringBuilder _currentScoreValueText = new StringBuilder("0");
        private StringBuilder _currentMultiplierValueText = new StringBuilder("1x");
        private string _currentMultiplierFormat = "{0:0.0}x";
        private ToggleImageButton _backImageButton;
        private MarbleSoundsSystem _soundSys;


        public GuiRendererSystem(Game game)
        {
            _game = game;
            _textureSheetLoader = game.Services.GetService(typeof(ITextureSheetLoader)) as ITextureSheetLoader;
            _settingsRepository = game.Services.GetService(typeof (GameSettingsRepository)) as GameSettingsRepository;
            _cam = game.Services.GetService(typeof (Camera2D)) as Camera2D;
            _textureSheetLoader = game.Services.GetService(typeof(ITextureSheetLoader)) as ITextureSheetLoader;
            _specialMarblesWithScreenComponentAspect =
                new Aspect().HasAllOf(new Type[] {typeof (SpecialMarbleComponent), typeof (MarbleScreenDataComponent)}).ExcludeAllOf(new Type[]{typeof(VerticalBounceComponent)});
        }

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }
        public void Initialize(World world)
        {
            _world = world;

            _backImageButton = new ToggleImageButton(_game, new ToggleButtonSettings()
            {
                Position = new Vector2(150, 120),
                TextureSheetName = @"SpriteSheets\GameArt",
                NormalSubTextureName = "BackButton",
                PressedlSubTextureName = "BackButton"
            });

            _pauseImageButton = new ToggleImageButton(_game, new ToggleButtonSettings()
                                                       {
                                                           Position = new Vector2(220,120),
                                                           TextureSheetName = @"SpriteSheets\GameArt",
                                                           NormalSubTextureName = "PauseButton-Pause",
                                                           PressedlSubTextureName = "PauseButton-Pause"
                                                       });

            _soundOnOffImageButton = new ToggleImageButton(_game, new ToggleButtonSettings()
            {
                Position = new Vector2(1150,120),
                TextureSheetName = @"SpriteSheets\GameArt",
                NormalSubTextureName = "SoundsButton-On",
                PressedlSubTextureName = "SoundsButton-Off"
            });

            _restartImageButton = new ToggleImageButton(_game, new ToggleButtonSettings()
            {
                Position = new Vector2(1220, 120),
                TextureSheetName = @"SpriteSheets\GameArt",
                NormalSubTextureName = "ReplayButton",
                PressedlSubTextureName = "ReplayButton"
            });
        }

        public void Start()
        {
            _soundSys = _world.GetSystem<MarbleSoundsSystem>();

            if (_pauseImageButton != null)
            {
                _pauseImageButton.Clicked += OnPauseImageClicked;
            }

            if (_backImageButton != null)
            {
                _backImageButton.Clicked += OnBackClicked;
            }

            if (_soundOnOffImageButton != null)
            {
                _soundOnOffImageButton.Clicked += OnSoundToggleClicked;
            }

            if (_restartImageButton != null)
            {
                _restartImageButton.Clicked += OnRestartImageClicked;
            }

            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();
            if (_controller != null)
            {
                _controller.LevelLoaded += OnLevelLoaded;
                _controller.GameLevelStateChanged += OnGameLevelStateChanged;
                _controller.CurrentPlayerDataChanged += OnCurrentPlayerDataChanged;
            }

            _gameStateSys = _world.GetSystem<CurrentGameInformationTrackingSystem>();

            _spriteBatch = new SpriteBatch(_game.GraphicsDevice);
        }

        private void OnBackClicked(object sender, ButtonClickedEventArgs e)
        {
            _controller.NotifyThatUserRequestedToGoToMainMenu();
        }

        private void OnCurrentPlayerDataChanged(object sender, EventArgs e)
        {
            UpdatePlayerData();
        }

        private void OnGameLevelStateChanged(object sender, EventArgs e)
        {

        }

        private void UpdatePlayerData()
        {
            if (_currentPlayerNameAlignedText != null)
            {
                _currentPlayerNameAlignedText.Text.Length = 0;
                _currentPlayerNameAlignedText.Text.Append(_gameStateSys.CurrentPlayerName);
                _currentPlayerNameAlignedText.RecalculateDrawingPosition();

                _currentPlayerHighScoreAlignedText.Text.Length = 0;
                _currentPlayerHighScoreAlignedText.Text.Append(_gameStateSys.CurrentPlayerHighScore);
                _currentPlayerHighScoreAlignedText.RecalculateDrawingPosition();
            }
        }

        private void OnLevelLoaded(object sender, EventArgs e)
        {
            _currentScore = 0;
            if (_gameStateSys.LevelDefinition.LevelType == LevelType.Arcade)
            {
                _remainingTimeText = new StringBuilder("Time:");
                _currentScoreTitleText = new StringBuilder("Current Level Score");
                _currentScoreTitleTextPos = new Vector2(30, 310);
            }
            else if (_gameStateSys.LevelDefinition.LevelType == LevelType.Survival)
            {
                _remainingTimeText = new StringBuilder("Time:");
                _currentScoreTitleText = new StringBuilder("Score:");
                _currentScoreTitleTextPos = new Vector2(730, 51);
            }
            InitializeMultiplierMarks();
        }

        private void InitializeMultiplierMarks()
        {
            _multiplierMarks.Clear();

            for (int i = 1; i <= _gameStateSys.LevelDefinition.MaximumMultiplier; i++)
            {

                var currentMultiplierLevel = ((i)/(_gameStateSys.LevelDefinition.MaximumMultiplier));

                var currentMultiplierDestRectX = (int) _fullMultiplierDrawPosition.X - 30;
                var currentMultiplierDestRectY = (int) (_fullMultiplierDrawPosition.Y +
                                                        ((1 - currentMultiplierLevel)*_desiredMultiplierFullHeight));

                _multiplierMarks.Add(i.ToString(), new Vector2(currentMultiplierDestRectX, currentMultiplierDestRectY - 10));
            }
        }

        private void OnRestartImageClicked(object sender, ButtonClickedEventArgs e)
        {
            _controller.RestartCurrentLevel();
        }

        private async void OnSoundToggleClicked(object sender, ButtonClickedEventArgs e)
        {
            _soundFxDisabled = e.IsPressed;
            _settingsRepository.Settings.SoundsEffectsEnabled = !_soundFxDisabled;
            await _settingsRepository.Save();

            if (e.IsPressed)
            {
                _soundSys.TemporarilyMuteCurrentLoopingSounds();
            }
            else
            {
                _soundSys.UnmuteCurrentLoopingSounds();
            }
        }

        private void OnPauseImageClicked(object sender, ButtonClickedEventArgs e)
        {
            if (_controller != null)
            {
                if (_gameStateSys.LevelState == LevelState.Paused)
                {
                    _controller.ResumeCurrentLevel();
                }
                else if (_gameStateSys.LevelState == LevelState.Running)
                {
                    _controller.PauseCurrentLevel();
                }
            }
        }

        public void Stop()
        {
            if (_backImageButton != null)
            {
                _backImageButton.Clicked -= OnBackClicked;
            }
            if (_pauseImageButton != null)
            {
                _pauseImageButton.Clicked -= OnPauseImageClicked;
            }

            if (_soundOnOffImageButton != null)
            {
                _soundOnOffImageButton.Clicked -= OnSoundToggleClicked;
            }

            if (_restartImageButton != null)
            {
                _restartImageButton.Clicked -= OnRestartImageClicked;
            }

            if (_controller != null)
            {
                _controller.LevelLoaded -= OnLevelLoaded;
            }
        }

        public void LoadContent()
        {
            _pauseImageButton.LoadContent();
            _backImageButton.LoadContent();
            _soundOnOffImageButton.LoadContent();
            _restartImageButton.LoadContent();
            _guiBigFont = _game.Content.Load<SpriteFont>(GameConstants.GuiFontLarge);
            _remainingSecondsAlignedText = new AlignedText(_guiBigFont, _remainingSecondsTextPos, 10) {Color = _guiTextColor};
            _requiredScoreValueAlignedText = new AlignedText(_guiBigFont, _requiredScoreValueTextPos, 20) { Color = _guiTextColor };
            _currentPlayerNameAlignedText = new AlignedText(_guiBigFont, _currentPlayerNameCenteredTextPos, 20) { Color = _guiTextColor };
            _currentPlayerHighScoreAlignedText = new AlignedText(_guiBigFont, _currentPlayerHighScoreCenteredTextPos, 20) { Color = _guiTextColor, Alignment = TextAlignment.Left };
            _gameArtSheet = _textureSheetLoader.Load(@"SpriteSheets\GameArt");            
/*            _multiplierFullGradientRect = _gameArtSheet.SubTextures["GradientGold2"].Rect;
            _multiplierFullGradientRect.Inflate(2,2);*/
            _currentGameOverTimerCenterdText = new AlignedText(_guiBigFont, Vector2.Zero, 2);
            UpdatePlayerData();
        }

        public void Render(GameTime gameTime)
        {
            _backImageButton.Draw(gameTime);
            _pauseImageButton.Draw(gameTime);
            _soundOnOffImageButton.Draw(gameTime);
            _restartImageButton.Draw(gameTime);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, _cam.GetViewTransformationMatrix());

            _spriteBatch.DrawString(_guiBigFont, _remainingTimeText, _remainingTimeTextPos, _guiTextColor,0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

            _remainingSecondsAlignedText.Draw(_spriteBatch);
//            _spriteBatch.DrawString(_guiBigFont, _remainingSecondsAlignedText.Text, _remainingSecondsAlignedText.Position, _guiTextColor);

            _spriteBatch.DrawString(_guiBigFont, _currentScoreTitleText, _currentScoreTitleTextPos, _guiTextColor);
            _spriteBatch.DrawString(_guiBigFont, _currentScoreValueText, _currentScoreValueTextPos, _guiTextColor);

            if (_gameStateSys.LevelDefinition.LevelType == LevelType.Arcade)
            {
                _spriteBatch.DrawString(_guiBigFont, _requiredScoreTitleText, _requiredScoreTitleTextPos, _guiTextColor);
                _requiredScoreValueAlignedText.Draw(_spriteBatch);
                /*_spriteBatch.DrawString(_guiBigFont, _requiredScoreValueAlignedText.Text,
                                        _requiredScoreValueAlignedText.Position, _guiTextColor);*/
            }


            _spriteBatch.DrawString(_guiBigFont, _multiplierTitleText, _multiplierTitlePos, _guiTextColor);
            _spriteBatch.DrawString(_guiBigFont, _playerTitle, _playerTitlePos, _guiTextColor);

            _spriteBatch.DrawString(_guiBigFont, _currentMultiplierValueText, _multiplierValuePos, _guiTextColor);           

            _spriteBatch.DrawString(_guiBigFont, _highScoreTitleText, _highScoreTitleTextPos, _guiTextColor);
            
            _currentPlayerNameAlignedText.Draw(_spriteBatch);
            _currentPlayerHighScoreAlignedText.Draw(_spriteBatch);
            //_spriteBatch.DrawString(_guiBigFont, _currentPlayerNameAlignedText.Text, _currentPlayerNameAlignedText.Position, _guiTextColor);
            //_spriteBatch.DrawString(_guiBigFont, _currentPlayerHighScoreAlignedText.Text, _currentPlayerHighScoreAlignedText.Position, _guiTextColor);

/*
            foreach (var multiplierMark in _multiplierMarks)
            {
                _spriteBatch.DrawString(_guiBigFont, multiplierMark.Key, multiplierMark.Value, _guiTextColor);
            }
*/

/*            _spriteBatch.Draw(_gameArtSheet.Texture, _currentMultiplierDestRect, _currentMultiplierSrcRect, Color.White);*/

            var specialMarblesWithScreenComponent =
                _world.EntityManager.GetLiveEntities(_specialMarblesWithScreenComponentAspect);

            foreach (var marble in specialMarblesWithScreenComponent)
            {
                var specialMarbleComponent = marble.GetComponent<SpecialMarbleComponent>();
                var screenData = marble.GetComponent<MarbleScreenDataComponent>();
                var details = specialMarbleComponent.Details;
                if (details is GameOverSpecialMarbleDetails)
                {
                    var real = details as GameOverSpecialMarbleDetails;

                    if (real.RemainingTimeInSeconds > 61)
                    {
                        continue;
                    }

                    _currentGameOverTimerCenterdText.Position = screenData.Position;
                    _currentGameOverTimerCenterdText.Text.Length = 0;

                    if (real.RemainingTimeInSeconds >= 1)
                        _currentGameOverTimerCenterdText.Text.Concat(real.RemainingTimeInSeconds, 0);
                    else
                    {
                        _currentGameOverTimerCenterdText.Text.Append(_kaboomText);
                    }
                    _currentGameOverTimerCenterdText.RecalculateDrawingPosition();

                    var scale = CalculateGameOverSpecialMarbleTimerScale(real.RemainingTimeInSeconds);

                    _spriteBatch.DrawString(_guiBigFont, _currentGameOverTimerCenterdText.Text, screenData.Position,
                                            Color.DarkViolet, 0f, _currentGameOverTimerCenterdText.Origin, scale,
                                            SpriteEffects.None, 1f);
                }
            }

            _spriteBatch.End();

        }

        private float CalculateGameOverSpecialMarbleTimerScale(float remainingTimeInSeconds)
        {
            var fraction = (float)Math.Abs(remainingTimeInSeconds - Math.Truncate(remainingTimeInSeconds));

            if (fraction > 0.5)
            {
                return 0f;
            }

            var multiplied = fraction * 10;



            return Math.Abs(multiplied);
        }

        public void Update(GameTime gameTime)
        {
            if (_settingsRepository.IsLoaded)
            {
                _soundFxDisabled = !_settingsRepository.Settings.SoundsEffectsEnabled;
            }

            if (_gameStateSys.LevelState == LevelState.Paused)
            {
                _pauseImageButton.IsPressed = true;
            }
            else
            {
                _pauseImageButton.IsPressed = false;
            }

            if (_currentScore < _gameStateSys.CurrentLevelScore)
            {
                _currentScore += _scoreCatchingUpSpeedInPointsPerSecond * (float) gameTime.ElapsedGameTime.TotalSeconds;
            }
            else
            {
                _currentScore = _gameStateSys.CurrentLevelScore;
            }

            _currentMultiplierValueText.Length = 0;
            _currentMultiplierValueText.AppendFormat(_currentMultiplierFormat, _gameStateSys.CurrentMultiplier);

            _currentScoreValueText.Length = 0;
            _currentScoreValueText.Concat(_currentScore, 0);

            if (_gameStateSys.IsLevelLoaded)
            {
                if (_gameStateSys.LevelDefinition.LevelType == LevelType.Arcade)
                {
                    _remainingSecondsAlignedText.Text.Length = 0;
                    _remainingSecondsAlignedText.Text.Concat((int) _gameStateSys.LevelRemainingTimeInSeconds);
                    _remainingSecondsAlignedText.RecalculateDrawingPosition();
                }
                else if (_gameStateSys.LevelDefinition.LevelType == LevelType.Survival)
                {
                    _remainingSecondsAlignedText.Text.Length = 0;
                    _remainingSecondsAlignedText.Text.Concat((int)_gameStateSys.LevelRemainingTimeInSeconds);
                    _remainingSecondsAlignedText.RecalculateDrawingPosition();                    
                }

                _requiredScoreValueAlignedText.Text.Length = 0;
                _requiredScoreValueAlignedText.Text.Concat(_gameStateSys.LevelDefinition.CompletionScore);
                _requiredScoreValueAlignedText.RecalculateDrawingPosition();

/*                _currentMultiplierLevel =  ((_gameStateSys.CurrentMultiplier) / (_gameStateSys.LevelDefinition.MaximumMultiplier));

                _currentMultiplierDestRect.X = (int) _fullMultiplierDrawPosition.X;
                _currentMultiplierDestRect.Y = (int) (_fullMultiplierDrawPosition.Y +
                                                      ((1 - _currentMultiplierLevel) * _desiredMultiplierFullHeight));
                _currentMultiplierDestRect.Width = _multiplierBarWidth;
                _currentMultiplierDestRect.Height = (int)(_currentMultiplierLevel * _desiredMultiplierFullHeight);


                _currentMultiplierSrcRect.X = _multiplierFullGradientRect.X;
                _currentMultiplierSrcRect.Y = (int) (_multiplierFullGradientRect.Y +
                                                     ((1 - _currentMultiplierLevel)*_multiplierFullGradientRect.Height));
                _currentMultiplierSrcRect.Width = _originalMultiplierBarImageWidth;
                _currentMultiplierSrcRect.Height = (int) (_currentMultiplierLevel*_multiplierFullGradientRect.Height);*/




            }

            _soundOnOffImageButton.IsPressed = _soundFxDisabled;

            _backImageButton.Update(gameTime);
            _pauseImageButton.Update(gameTime);
            _soundOnOffImageButton.Update(gameTime);
            _restartImageButton.Update(gameTime);
        }
    }
}