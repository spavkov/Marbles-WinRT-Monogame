using System;
using System.Collections.Generic;
using System.Text;
using Marbles.Core.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roboblob.GameEntitySystem.WinRT;
using Roboblob.XNA.WinRT.Camera;
using System.Linq;
using Roboblob.XNA.WinRT.Mathematics;

namespace Marbles.Core.Systems
{
    public class ScoreChangesVisualizerSystem :IWorldSystem, IWorldRenderingSystem
    {
        private readonly Game _game;
        private World _world;
        private LevelScoringSystem _scoringSys;
        private SpriteFont _font;
        private List<GameScoreChangeIndicationData> _currentScores = new List<GameScoreChangeIndicationData>();
        private SpriteBatch _spriteBatch;
        private List<GameScoreChangeIndicationData> _toRemove = new List<GameScoreChangeIndicationData>();
        private CurrentGameInformationTrackingSystem _gameInfo;

        private string _PlusFormat = "+{0}";
        private string _PlusSecondsFormat = "+{0} Seconds";
        private string _MinusFormat = "-{0}";
        private Camera2D _camera;
        private MarbleGameLevelControllerSystem _controller;
        private float _indicatorVerticalSlideSpeedInPixelsPerSeconds = 50f;
        private float _numberOfSecondsForIndicatorToBeShownBeforeStartingToFade = 1.2f;
        private float _numberOfSecondsForIndicatorToBecomeFullyVisible = 0.3f;
        private Random _rnd = new Random();
        private string _multipleComboTextFormat = "{0} x Combo";
        private string _singleComboText = "Combo";
        private float _gameScoreChangeIndicationSpeed = 40f;

        public ScoreChangesVisualizerSystem(Game game)
        {
            _game = game;
            _camera = _game.Services.GetService(typeof (Camera2D)) as Camera2D;
        }

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }
        public void Initialize(World world)
        {
            _world = world;
        }

        public void Start()
        {
            _scoringSys = _world.GetSystem<LevelScoringSystem>();
            if (_scoringSys != null)
            {
                _scoringSys.GameScoreChanged += OnGameScoreChanged;
            }

            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();
            if (_controller != null)
            {
                _controller.LevelStopped += OnLevelStopped;
            }

            _gameInfo = _world.GetSystem<CurrentGameInformationTrackingSystem>();
        }

        private void OnLevelStopped(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void OnGameScoreChanged(object sender, CumulativeGameScoreChangeEvent e)
        {
            foreach (var gameScoreChangeIndicationData in e.ScoreChanges)
            {
                if (gameScoreChangeIndicationData.ChangeType == ScoreChangeType.ScoreIncrease || gameScoreChangeIndicationData.ChangeType == ScoreChangeType.ScoreDecrease)
                {
                    _currentScores.Add(new GameScoreChangeIndicationData()
                                           {
                                               SpeedMultiplier = 1f + (float)(_rnd.NextDouble() * 1.5),
                                               Text = (gameScoreChangeIndicationData.ChangeType == ScoreChangeType.ScoreIncrease)
                                                       ? string.Format(_PlusFormat,
                                                                       gameScoreChangeIndicationData.ChangeAmount)
                                                       : string.Format(_MinusFormat,
                                                                       gameScoreChangeIndicationData.ChangeAmount),
                                               CurrentScale = 0.0f,
                                               FinalScale = 1f,
                                               CurrentColor =
                                                   (gameScoreChangeIndicationData.ChangeType == ScoreChangeType.ScoreIncrease)
                                                       ? Color.Orange
                                                       : Color.Red,
                                               RemainingSecondsToBeShown = _numberOfSecondsForIndicatorToBeShownBeforeStartingToFade,
                                               TotalDurationOfAnimationForBecomingFullyVisible = _numberOfSecondsForIndicatorToBecomeFullyVisible,
                                               CurrentPosition = gameScoreChangeIndicationData.ChangeScreenOrigin,
                                               Direction = CreateDirection()
                                               
                                           });
                }
                if (gameScoreChangeIndicationData.ChangeType == ScoreChangeType.TimeIncrease)
                {
                    _currentScores.Add(new GameScoreChangeIndicationData()
                    {
                        SpeedMultiplier = 1f + (float)(_rnd.NextDouble() * 0.5),
                        Text = (gameScoreChangeIndicationData.ChangeType == ScoreChangeType.TimeIncrease)
                                ? string.Format(_PlusSecondsFormat,
                                                gameScoreChangeIndicationData.ChangeAmount)
                                : string.Format(_MinusFormat,
                                                gameScoreChangeIndicationData.ChangeAmount),
                        TotalDurationOfAnimationForBecomingFullyVisible = _numberOfSecondsForIndicatorToBecomeFullyVisible,
                        CurrentColor =
                            (gameScoreChangeIndicationData.ChangeType == ScoreChangeType.ScoreIncrease)
                                ? Color.Orange
                                : Color.DarkOrange,
                        RemainingSecondsToBeShown =
                            _numberOfSecondsForIndicatorToBeShownBeforeStartingToFade,
                        CurrentPosition = gameScoreChangeIndicationData.ChangeScreenOrigin,
                        Direction = CreateDirection()
                    });
                }
                else if (gameScoreChangeIndicationData.ChangeType == ScoreChangeType.Combo)
                {
                    var timesCombo = gameScoreChangeIndicationData.TotalMarblesClearedCount / _gameInfo.LevelDefinition.NumberOfMarblesInSequenceToIncreaseBonusMultiplier;

                    _currentScores.Add(new GameScoreChangeIndicationData()
                    {
                        Text = timesCombo > 1 ? string.Format(_multipleComboTextFormat, timesCombo) : _singleComboText,
                        SpeedMultiplier = 1f + (float) (_rnd.NextDouble() * 0.5),
                        TotalDurationOfAnimationForBecomingFullyVisible = _numberOfSecondsForIndicatorToBecomeFullyVisible,
                        CurrentColor = Color.Gold, 
                        RemainingSecondsToBeShown = _numberOfSecondsForIndicatorToBeShownBeforeStartingToFade,
                        CurrentPosition = gameScoreChangeIndicationData.ChangeScreenOrigin,
                        Direction = CreateDirection()
                    });
                }
            }
        }

        private Vector2 CreateDirection()
        {
            var d = new Vector2((_rnd.Next(-10, +10))/10f, (_rnd.Next(-10, +10))/10f);
            d.Normalize();
            return d;
        }

        public void Stop()
        {
            if (_scoringSys != null)
            {
                _scoringSys.GameScoreChanged -= OnGameScoreChanged;
            }

            if (_controller != null)
            {
                _controller.LevelStopped -= OnLevelStopped;
            }

            Cleanup();
        }

        private void Cleanup()
        {
            _currentScores.Clear();
            _toRemove.Clear();
        }

        public void Update(GameTime gameTime)
        {
            if (_gameInfo.LevelState != LevelState.Running)
                return;

            _toRemove.Clear();

            var secs = (float)gameTime.ElapsedGameTime.TotalSeconds;
            foreach (var gameScoreChangeIndicationData in _currentScores)
            {
                gameScoreChangeIndicationData.RemainingSecondsToBeShown -= secs;
                gameScoreChangeIndicationData.TotalAliveTime += secs;

                gameScoreChangeIndicationData.CurrentScale =
                    TweenHelper.Calculate(gameScoreChangeIndicationData.InitialScale,
                                          gameScoreChangeIndicationData.FinalScale,
                                          gameScoreChangeIndicationData.TotalAliveTime,
                                          gameScoreChangeIndicationData.TotalDurationOfAnimationForBecomingFullyVisible,
                                          ScaleFuncs.SineEaseInOut);

                gameScoreChangeIndicationData.CurrentPosition.X += gameScoreChangeIndicationData.Direction.X * (_gameScoreChangeIndicationSpeed * secs);
                gameScoreChangeIndicationData.CurrentPosition.Y += gameScoreChangeIndicationData.Direction.Y * (_gameScoreChangeIndicationSpeed* secs);

                if (gameScoreChangeIndicationData.RemainingSecondsToBeShown <= 0)
                {
                    var toDeduct = (byte) (255f*secs);
                    if (toDeduct >= gameScoreChangeIndicationData.CurrentColor.A)
                    {
                        _toRemove.Add(gameScoreChangeIndicationData);
                        gameScoreChangeIndicationData.CurrentColor.A = 0;
                    }
                    else
                    {
                        gameScoreChangeIndicationData.CurrentColor.A -= toDeduct;
                    }
                }
            }

            foreach (var gameScoreChangeIndicationData in _toRemove)
            {
                _currentScores.Remove(gameScoreChangeIndicationData);
            }
        }

        public void Render(GameTime gameTime)
        {
            if (!_currentScores.Any())
            {
                return;
            }

            _spriteBatch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone,null,_camera.GetViewTransformationMatrix());
            foreach (var gameScoreChangeIndicationData in _currentScores)
            {
                _spriteBatch.DrawString(_font, gameScoreChangeIndicationData.Text, gameScoreChangeIndicationData.CurrentPosition,  gameScoreChangeIndicationData.CurrentColor, 0f, Vector2.Zero, gameScoreChangeIndicationData.CurrentScale, SpriteEffects.None, 0f );
            }
            _spriteBatch.End();
        }

        public void LoadContent()
        {
            _font = _game.Content.Load<SpriteFont>(GameConstants.ScoreChangeVisualizationsFont);
            _spriteBatch = new SpriteBatch(_game.GraphicsDevice);
        }

        private class GameScoreChangeIndicationData
        {
            public Vector2 CurrentPosition;

            public string Text;

            public float RemainingSecondsToBeShown;

            public Color CurrentColor;

            public float SpeedMultiplier;

            public float TotalDurationOfAnimationForBecomingFullyVisible;
            public float CurrentScale = 0f;
            public float FinalScale = 1f;
            public float InitialScale = 0f;
            public float TotalAliveTime = 0f;
            public Vector2 Direction;
        }
    }
}