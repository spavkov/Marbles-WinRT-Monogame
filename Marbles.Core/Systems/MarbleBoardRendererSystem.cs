using System;
using System.Collections.Generic;
using System.Text;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Components.SpecialMarbles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Roboblob.GameEntitySystem.WinRT;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.Content;
using System.Linq;
using Roboblob.XNA.WinRT.Helpers;
using Roboblob.XNA.WinRT.Input;
using Roboblob.XNA.WinRT.Mathematics;
using Roboblob.XNA.WinRT.Rendering;
using Roboblob.XNA.WinRT.ResolutionIndependence;
using Roboblob.XNA.WinRT.Performance;
using Windows.UI.Xaml;

namespace Marbles.Core.Systems
{
    public class MarbleBoardRendererSystem : IWorldRenderingSystem
    {
        private readonly Game _game;
        private readonly ITextureSheetLoader _textureSheetLoader;
        private MarblesWorld _world;
        private SpriteBatch _spriteBatch;
        private Camera2D _cam;
        private Aspect _aspect;
        private List<GameEntity> _marbles = new List<GameEntity>();
        private Vector2 _marbleTextureOriginVector = new Vector2(60/2, 60/2);
        //private float _scaleOneF = 1f;
        private TextureSheet _gameArtSheet;
        private ResolutionIndependentRenderer _resolution;
        private GameEntity _marble;
        private CurrentGameInformationTrackingSystem _gameInformation;
        private Vector2 _pausedTextSize;
        private Vector2 _pausedPos;
        //private LineClearerSpecialMarbleDetails _lineClearerDetails;
        private static string _pausedText = "PAUSED";
        private Rectangle _arrowLeftRightRect = new Rectangle();
        private Rectangle _arrowUpDownRect = new Rectangle();
        private Rectangle _arrowUpDownLeftRightRect = new Rectangle();
        private Rectangle _yellowMarbleRect;
        private Rectangle _blueMarbleRect;

        private Rectangle _greenMarbleRect;
        private Rectangle _orangeMarbleRect;
        private Rectangle _purpleMarbleRect;
        private Rectangle _redMarbleRect;
        private Rectangle _grayMarbleRect;
        private Rectangle _clockRect;
        private Rectangle _currentRect = new Rectangle();
        private AlignedText _currentGameOverTimerCenterdText;

        public MarbleBoardRendererSystem(Game game)
        {
            _game = game;
            _aspect = new Aspect().HasAllOf(typeof (MarbleScreenDataComponent), typeof (BoardCellChildEntityComponent),
                                 typeof (MarbleComponent));
            _textureSheetLoader = game.Services.GetService(typeof(ITextureSheetLoader)) as ITextureSheetLoader;
            _cam = game.Services.GetService(typeof (Camera2D)) as Camera2D;
            _multitouchHelper = _game.Services.GetService(typeof(MultitouchHelper)) as MultitouchHelper;
            _resolution = game.Services.GetService(typeof(ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            _currentMarbleDestRect = new Rectangle(0, 0, GameConstants.MarbleRadius, GameConstants.MarbleRadius);

            _lineRenderer = new PrimitivesRenderer(_game.GraphicsDevice, _resolution.VirtualWidth,
                                                  _resolution.VirtualHeight);
            _colorsHelper = new MarbleColorsHelper();

            _coloredBackgroundRenderer = new ColoredBackgroundRenderer(_game);
            _coloredBackgroundRenderer.Color = Color.FromNonPremultiplied(252, 109, 38, 128);
            _coloredBackgroundRenderer.BorderColor = Color.FromNonPremultiplied(235, 78, 35, 128);
            _coloredBackgroundRenderer.BorderWidth = 2;
            _coloredBackgroundRenderer.Position = new Vector2(353,94);
            _coloredBackgroundRenderer.Width = 660;
            _coloredBackgroundRenderer.Height = 580;
            // [1:55:31 PM] Slobodan Pavkov: 656,574 je board
        }

        public void Render(GameTime gameTime)
        {
            // this begin end we need for some reason, find out why
/*            _resolution.SetupFullViewport();
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null);
                _spriteBatch.Draw(_menuBgTileTexture, Vector2.Zero, _fullScreenRectangle, Color.White * 0.5f);
                _spriteBatch.End();
                _resolution.SetupVirtualScreenViewport();*/

            _resolution.SetupVirtualScreenViewport();
                //RenderSequence(gameTime);

                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, _cam.GetViewTransformationMatrix());

            _coloredBackgroundRenderer.Draw(gameTime, _spriteBatch);

                _marbles =
                    _world.EntityManager.GetLiveEntities(_aspect).OrderBy(
                e => e.GetComponent<BoardCellChildEntityComponent>().Cell.Row).ToList();

                for (int i = 0; i < _marbles.Count; i++)
                {
                    _marble = _marbles[i];

                    _screenData = _marble.GetComponent<MarbleScreenDataComponent>();
                    var marbleColor = _marble.GetComponent<MarbleComponent>().Color;

                    _marbleTexturesHelper.GetMarbleRectable(marbleColor, out _currentRect);

/*                    switch (marbleColor)
                    {
                        case MarbleColor.Gray:
                            _currentRect = _grayMarbleRect;
                            break;
                        case MarbleColor.Blue:
                            _currentRect = _blueMarbleRect;
                            break;
                        case MarbleColor.Brown:
                            _currentRect = _brownMarbleRect;
                            break;
                        case MarbleColor.Silver:
                            _currentRect = _silverMarbleRect;
                            break;
                        case MarbleColor.Green:
                            _currentRect = _greenMarbleRect;
                            break;
                        case MarbleColor.Orange:
                            _currentRect = _orangeMarbleRect;
                            break;
                        case MarbleColor.Purple:
                            _currentRect = _purpleMarbleRect;
                            break;
                        case MarbleColor.Red:
                            _currentRect = _redMarbleRect;
                            break;
                        case MarbleColor.Yellow:
                            _currentRect = _yellowMarbleRect;
                            break;                        
                    }*/

                    _currentMarbleDestRect.X = (int) _screenData.Position.X;
                    _currentMarbleDestRect.Y = (int) _screenData.Position.Y;
                    _currentMarbleDestRect.Width = GameConstants.MarbleRadius;
                    _currentMarbleDestRect.Height = GameConstants.MarbleRadius;

                    _currentMarbleDrawnIsTouched = false;

                    if (_gameInformation.IsLevelRunning)
                    {
                        if (_marble.HasComponent<TouchableComponent>())
                        {
                            var touchable = _marble.GetComponent<TouchableComponent>();

                            _currentMarbleDrawnIsTouched = touchable.IsTouched;

                            if (touchable.IsTouched)
                            {
                                var since = DateTime.Now - touchable.LastTouchDateTime;
                                var secs = (float) since.TotalSeconds;
                                var scale = TweenHelper.Calculate(1f, 1.15f, secs, 0.3f,
                                                                  ScaleFuncs.CubicEaseOut);
                                

                                RectangleHelper.Scale(ref _currentMarbleDestRect, scale);
                                _screenData.RotationAngle = _screenData.RotationAngle +
                                                            (20*(float) gameTime.ElapsedGameTime.TotalSeconds);
                            }
                        }
                    }

                    _spriteBatch.Draw(_gameArtSheet.Texture, _currentMarbleDestRect,
                        _currentRect, Color.White,
                                      MathHelper.ToRadians(_screenData.RotationAngle), _marbleTextureOriginVector,
                                      SpriteEffects.None, 0);

                    if (_marble.HasComponent<SpecialMarbleComponent>())
                    {
                        var specialMarbleComponent = _marble.GetComponent<SpecialMarbleComponent>();
                        var details = specialMarbleComponent.Details;
                        if (details is TimeExtenderSpecialMarbleDetails)
                        {
                            _spriteBatch.Draw(_gameArtSheet.Texture, _currentMarbleDestRect,
                                              _clockRect, Color.White, 0,
                                              _marbleTextureOriginVector, SpriteEffects.None, 1);
                        }
                        else if (details is ColorBombSpecialMarbleDetails)
                        {
                            _spriteBatch.Draw(_gameArtSheet.Texture, _currentMarbleDestRect,
                                              _colorBombRect, Color.White, 0,
                                              _marbleTextureOriginVector, SpriteEffects.None, 2);
                        }
                        else if (details is SurpriseSpecialMarbleDetails)
                        {
                            _spriteBatch.Draw(_gameArtSheet.Texture, _currentMarbleDestRect,
                                              _surpriseSpecialMarbleGlyphRect, Color.White, 0,
                                              _marbleTextureOriginVector, SpriteEffects.None, 2);
                        }
                        else if (details is GameOverSpecialMarbleDetails)
                        {
/*                            var real = details as GameOverSpecialMarbleDetails;
                            _currentGameOverTimerCenterdText.SetPosition(ref _screenData.Position);
                            _currentGameOverTimerCenterdText.Text.Length = 0;
                            _currentGameOverTimerCenterdText.Text.Concat(real.RemainingTimeInSeconds,0);
                            _currentGameOverTimerCenterdText.RecalculateDrawingPosition();*/

                            _spriteBatch.Draw(_gameArtSheet.Texture, _currentMarbleDestRect,
                                              _gameOverSpecialMarbleGlyphRect, Color.White, 0,
                                              _marbleTextureOriginVector, SpriteEffects.None, 2);

/*
                            var scale = CalculateGameOverSpecialMarbleTimerScale(real.RemainingTimeInSeconds);

                            _spriteBatch.DrawString(_font, _currentGameOverTimerCenterdText.Text, _screenData.Position,
                                                                          Color.Red, 0f, _currentGameOverTimerCenterdText.Origin, scale, SpriteEffects.None, 1f);
*/
                        }
                        else if (details is LineClearerSpecialMarbleDetails)
                        {
                            var lineClearer = details as LineClearerSpecialMarbleDetails;

                            if (lineClearer.ClearerType == LineClearerType.HorizontalClearer)
                            {
                                _spriteBatch.Draw(_gameArtSheet.Texture, _currentMarbleDestRect,
                                                  _arrowLeftRightRect, Color.White, 0,
                                                  _marbleTextureOriginVector, SpriteEffects.None, 2);
                            }
                            else if (lineClearer.ClearerType == LineClearerType.VerticalClearer)
                            {
                                _spriteBatch.Draw(_gameArtSheet.Texture, _currentMarbleDestRect,
                                                  _arrowUpDownRect, Color.White, 0,
                                                  _marbleTextureOriginVector, SpriteEffects.None, 2);
                            }
                            else if (lineClearer.ClearerType == LineClearerType.HorizontalAndVerticalClearer)
                            {
                                _spriteBatch.Draw(_gameArtSheet.Texture, _currentMarbleDestRect,
                                                  _arrowUpDownLeftRightRect, Color.White, 0,
                                                  _marbleTextureOriginVector, SpriteEffects.None, 2);
                            }
                        }
                    }
                }


/*                        _spriteBatch.DrawString(_font, string.Format("Level: {0} (nr: {1}) Entities Count: {2}", _world.IsLevelLoaded ? _world.CurrentLevel.Name : "No LevelLoaded", _world.IsLevelLoaded ? _world.CurrentLevel.Index.ToString() : "No LevelLoaded",  _world.AllEntities.Count()),new Vector2(800,0), Color.Red);
                        _spriteBatch.DrawString(_font, string.Format("Time Left: {0:#.00s}", _gameInformation.LevelRemainingTimeInSeconds), new Vector2(800, 50), Color.Red);
                        _spriteBatch.DrawString(_font, string.Format("Total Score: {0}", _gameInformation.TotalScore), new Vector2(1100, 50), Color.Red);

                        _spriteBatch.DrawString(_font, string.Format("Level Score: {0}", _gameInformation.CurrentLevelScore), new Vector2(800, 100), Color.Red);
                        _spriteBatch.DrawString(_font, string.Format("Level Requirement: {0}", _gameInformation.LevelDefinition.CompletionScore), new Vector2(1000, 100), Color.Red);

                        _spriteBatch.DrawString(_font, string.Format("Current Multiplier: {0:#.00}", _gameInformation.CurrentMultiplier), new Vector2(800, 150), Color.Red);*/

/*            if (_gameInformation.LevelState == LevelState.Paused)
            {
                _spriteBatch.DrawString(_font, string.Format("Paused", _world.AllEntities.Count()), new Vector2(800, 600), Color.Blue);
            }
            if (_gameInformation.LevelState == LevelState.Completed || _gameInformation.LevelState == LevelState.Failed)
            {
                _spriteBatch.DrawString(_font, string.Format(_gameInformation.LevelState == LevelState.Completed ? "Completed" : "Failed", _world.AllEntities.Count()), new Vector2(800, 600), Color.Blue);
            }*/

/*                if (_gameInformation.LevelState == LevelState.Paused)
                {
                    _spriteBatch.DrawString(_font, _pausedText, _pausedPos, Color.White);
                }*/


            _spriteBatch.End();
        }

        public void RenderSequence(GameTime gameTime)
        {
            if (!_multitouchHelper.WeHaveNewTouches())
            {
                return;
            }

            if (_touchSequenceSystem.CurrentSequences.Any())
            {

                foreach (var fingerTouchSequence in _touchSequenceSystem.CurrentSequences.Values)
                {
                    for (int i = 0; i < fingerTouchSequence.EntitiesTouchedSoFar.Count; i++)
                    {
                        if (i + 1 < fingerTouchSequence.EntitiesTouchedSoFar.Count)
                        {
                            var starta = fingerTouchSequence.EntitiesTouchedSoFar[i].GetComponent<BoardCellChildEntityComponent>().Cell.Center;
                            var enda = fingerTouchSequence.EntitiesTouchedSoFar[i + 1].GetComponent<BoardCellChildEntityComponent>().Cell.Center;

                            var color = _colorsHelper.MarbleColorToColor(fingerTouchSequence.Color);

                            _lineRenderer.DrawArrowedLine(starta, enda, 4, color);

                            //_spriteBatch.DrawLine2(starta, enda, GetColor(fingerTouchSequence.Color), 5);
                        }
                    }

/*
                    var start = fingerTouchSequence.LastTouchedEntityInSequence.GetComponent<BoardCellChildEntityComponent>().Cell.Center;

                    foreach (var touchLocation in _multitouchHelper.CurrentTouches)
                    {
                        if (touchLocation.State == TouchLocationState.Moved)
                        {
                            var end = _resolution.ScaleMouseToScreenCoordinates(touchLocation.Position);

                            var color = _colorsHelper.MarbleColorToColor(fingerTouchSequence.Color);

                            _lineRenderer.AddLine(start, color, end, color, 4, true);

                            break;
                        }
                    }*/

                }


            }
        }

        public void LoadContent()
        {
            _menuBgTileTexture = _game.Content.Load<Texture2D>(GameConstants.RollingBackgroundTileName);
            _gameArtSheet = _textureSheetLoader.Load(@"SpriteSheets\GameArt");

            _marbleTexturesHelper = new MarbleTexturesHelper(_gameArtSheet);

            _marbleTextureOriginVector = _marbleTexturesHelper.GetMarbleTextureOriginVector();

            _pausedPos = new Vector2(_resolution.VirtualWidth / 2 - _pausedTextSize.X/2,
                                     _resolution.VirtualHeight / 2 - _pausedTextSize.Y/2);

            _arrowLeftRightRect = _gameArtSheet.SubTextures["ArrowLeftRight"].Rect;
            _arrowUpDownRect = _gameArtSheet.SubTextures["ArrowUpDown"].Rect;
            _arrowUpDownLeftRightRect = _gameArtSheet.SubTextures["ArrowUpDownLeftRight"].Rect;

/*            _yellowMarbleRect = _gameArtSheet.SubTextures["yellow"].Rect;
            _blueMarbleRect = _gameArtSheet.SubTextures["blue"].Rect;
            _greenMarbleRect = _gameArtSheet.SubTextures["green"].Rect;
            _orangeMarbleRect = _gameArtSheet.SubTextures["orange"].Rect;
            _purpleMarbleRect = _gameArtSheet.SubTextures["purple"].Rect;
            _redMarbleRect = _gameArtSheet.SubTextures["red"].Rect;
            _grayMarbleRect = _gameArtSheet.SubTextures["gray"].Rect;
            _brownMarbleRect = _gameArtSheet.SubTextures["brown"].Rect;
            _silverMarbleRect = _gameArtSheet.SubTextures["silver"].Rect;

            _marbleTextureOriginVector = new Vector2(_constants.MarbleTextureWidth / 2, _constants.MarbleTextureHeight / 2); // all marbles should be same size for this to work*/

            _clockRect = _gameArtSheet.SubTextures["Clock"].Rect;
            _colorBombRect = _gameArtSheet.SubTextures["Explosion"].Rect;
            _gameOverSpecialMarbleGlyphRect = _gameArtSheet.SubTextures["Danger"].Rect;
            _surpriseSpecialMarbleGlyphRect = _gameArtSheet.SubTextures["Help"].Rect;

            _fullScreenRectangle = new Rectangle(0,0, _resolution.ScreenWidth, _resolution.ScreenHeight);
        }

/*
        private Rectangle GetDestRectangle(GameEntity entity)
        {
            var screenData = entity.GetComponent<MarbleScreenDataComponent>();
            _currentRectangle = screenData.DestRectangle;

            if (entity.HasComponent<TouchableComponent>() && entity.GetComponent<TouchableComponent>().IsTouched)
            {
                _currentRectangle.Inflate(3, 3);
            }

            return _currentRectangle;
        }
*/

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }

        public void Initialize(World world)
        {
            _world = (MarblesWorld) world;
            _world.GetSystem<MarbleBoardTouchedSequencesReplacerSystem>();

            _spriteBatch = new SpriteBatch(_game.GraphicsDevice);          
        }


        public void Start()
        {
            _gameInformation = _world.GetSystem<CurrentGameInformationTrackingSystem>();
            _touchSequenceSystem = _world.GetSystem<TouchSequencesSystem>();
        }

        public void Stop()
        {
            
        }

        public void Update(GameTime gameTime)
        {
            _fullScreenRectangle.X = _fullScreenRectangle.X + ((int)(100 * gameTime.ElapsedGameTime.TotalSeconds));
            if (_fullScreenRectangle.X > _fullScreenRectangle.X + _menuBgTileTexture.Width)
            {
                _fullScreenRectangle.X = 0;
            }
        }

        public MarbleScreenDataComponent _screenData;
        private Rectangle _fullScreenRectangle = new Rectangle();
        private Rectangle _colorBombRect;
        private Rectangle _gameOverSpecialMarbleGlyphRect;
        private Rectangle _surpriseSpecialMarbleGlyphRect;
        private Rectangle _currentMarbleDestRect = new Rectangle();
        private bool _currentMarbleDrawnIsTouched;
        private Rectangle _brownMarbleRect;
        private Rectangle _silverMarbleRect;
        private MultitouchHelper _multitouchHelper;
        private TouchSequencesSystem _touchSequenceSystem;
        private PrimitivesRenderer _lineRenderer;
        private MarbleColorsHelper _colorsHelper;
        private Texture2D _menuBgTileTexture;
        private MarbleTexturesHelper _marbleTexturesHelper;
        private ColoredBackgroundRenderer _coloredBackgroundRenderer;
    }
}