using System;
using System.Collections.Generic;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Roboblob.GameEntitySystem.WinRT;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.Content;
using Roboblob.XNA.WinRT.Input;
using Roboblob.XNA.WinRT.Mathematics;
using Roboblob.XNA.WinRT.Rendering;
using System.Linq;
using Roboblob.XNA.WinRT.ResolutionIndependence;

namespace Marbles.Core.Systems
{
    public class SequenceVisualizationRenderingSystem : IWorldRenderingSystem
    {
        private const float _shapesFadeoutSpeed = 0.9f;
        private readonly Game _game;
        private World _world;
        private TouchSequencesSystem _touchSequenceSystem;
        private SpriteBatch _spriteBatch;
        private MultitouchHelper _multitouchHelper;
        private Camera2D _cam;
        private ResolutionIndependentRenderer _resolution;
        private PrimitivesRenderer _lineRenderer;
        private MarbleColorsHelper _colorsHelper;
        private Texture2D _menuBgTileTexture;
        private Rectangle _fullScreenRectangle;
        private MarbleGameLevelControllerSystem _controller;
        private List<ClearedShapeData> _shapesToRender = new List<ClearedShapeData>(10);
        private List<ClearedShapeData> _shapesQueue = new List<ClearedShapeData>(10);
        private TextureSheet _gameArt;
        private ITextureSheetLoader _textureSheetLoader;
        private Rectangle _circleSourceRect;
        private Vector2 _circleOriginVector;
        private float _shapeSpeed = 30.3f;
        private List<ClearedShapeData> _clearedShapesToRemove = new List<ClearedShapeData>();

        public SequenceVisualizationRenderingSystem(Game game)
        {
            _game = game;
            _multitouchHelper = _game.Services.GetService(typeof (MultitouchHelper)) as MultitouchHelper;
            _cam = game.Services.GetService(typeof(Camera2D)) as Camera2D;
            _resolution = game.Services.GetService(typeof(ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            _lineRenderer = new PrimitivesRenderer(_game.GraphicsDevice, _resolution.VirtualWidth,
                                                  _resolution.VirtualHeight);

            _textureSheetLoader = game.Services.GetService(typeof (ITextureSheetLoader)) as ITextureSheetLoader;

            _colorsHelper = new MarbleColorsHelper();
        }

        public int Priority { 
            get { return GamePriorities.Systems.IndexOf(this.GetType()); }
        }
        public void Initialize(World world)
        {
            _world = world;
        }

        public void Start()
        {
            _touchSequenceSystem = _world.GetSystem<TouchSequencesSystem>();
            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();
        }

        public void Stop()
        {

        }

        public void NotifySystemThatUserClearedTriangle(List<Vector2> points, MarbleColor color)
        {
            var shape = (new ClearedShapeData()
                {
                    Color = _colorsHelper.MarbleColorToColor(color),
                    Points = points,
                });

            SimpleMathHelper.CalculateCentroid(points, points.Count - 1, out shape.Centroid);
            shape.PointDirections = new Vector2[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                shape.PointDirections[i] = points[i] - shape.Centroid;
                shape.PointDirections[i].Normalize();
            }

            _shapesQueue.Add(shape);
        }

        public void Update(GameTime gameTime)
        {
            _fullScreenRectangle.X = _fullScreenRectangle.X - ((int)(100 * gameTime.ElapsedGameTime.TotalSeconds));
            if (_fullScreenRectangle.X < _fullScreenRectangle.X - _menuBgTileTexture.Width)
            {
                _fullScreenRectangle.X = 0;
            }

            _shapesToRender.AddRange(_shapesQueue);
            _shapesQueue.Clear();

            var secs = (float) gameTime.ElapsedGameTime.TotalSeconds;
            var speed = secs*_shapeSpeed;
            //var toDeduct = (byte) (100f*secs);
            foreach (var clearedShapeData in _shapesToRender)
            {
                for (int i = 0; i < clearedShapeData.Points.Count; i++)
                {
                    clearedShapeData.Color *= (1 - (secs * _shapesFadeoutSpeed));

                    if (clearedShapeData.Color.A <= 0)
                    {
                        _clearedShapesToRemove.Add(clearedShapeData);
                    }
                    else
                    {
                        clearedShapeData.Points[i] += clearedShapeData.PointDirections[i] * speed;                        
                    }

                }
            }

            foreach (var clearedShapeData in _clearedShapesToRemove)
            {
                _shapesToRender.Remove(clearedShapeData);
            }

            _clearedShapesToRemove.Clear();
        }

        public void LoadContent()
        {
            _spriteBatch = new SpriteBatch(_game.GraphicsDevice);
            _menuBgTileTexture = _game.Content.Load<Texture2D>(GameConstants.RollingBackgroundTileName);

            _gameArt = _textureSheetLoader.Load(@"SpriteSheets\GameArt");

            _fullScreenRectangle = new Rectangle(0, 0, _resolution.ScreenWidth, _resolution.ScreenHeight);
            _circleSourceRect = _gameArt.SubTextures["circle"].Rect;
            _circleOriginVector = new Vector2(_circleSourceRect.Width/2, _circleSourceRect.Height/2);
        }


        public void Render(GameTime gameTime)
        {
            if (_controller.LevelState != LevelState.Running)
            {
                return;
            }

/*            _resolution.SetupFullViewport();
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null);
            _spriteBatch.Draw(_menuBgTileTexture, Vector2.Zero, _fullScreenRectangle, Color.White);
            _spriteBatch.End();

            _resolution.SetupVirtualScreenViewport();*/

            foreach (var clearedShapeData in _shapesToRender)
            {
                for (int i = 0; i < clearedShapeData.Points.Count; i++)
                {
                    if (i == clearedShapeData.Points.Count - 1)
                        _lineRenderer.DrawLine(clearedShapeData.Points[i], clearedShapeData.Points[0], 4, clearedShapeData.Color);
                    else
                    {
                        _lineRenderer.DrawLine(clearedShapeData.Points[i], clearedShapeData.Points[i + 1], 4, clearedShapeData.Color);
                    }
                }
            }

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, _cam.GetViewTransformationMatrix());
            foreach (var clearedShapeData in _shapesToRender)
            {
                for (int i = 0; i < clearedShapeData.Points.Count; i++)
                {
                    _spriteBatch.Draw(_gameArt.Texture, clearedShapeData.Points[i], _circleSourceRect, clearedShapeData.Color, 0f, _circleOriginVector, 1f, SpriteEffects.None, 0f);
                }
            }

            _spriteBatch.End();


            if (!_multitouchHelper.WeHaveNewTouches())
            {
                return;
            }


                foreach (var fingerTouchSequence in _touchSequenceSystem.CurrentSequences.Values)
                {
                    for (int i = 0; i < fingerTouchSequence.EntitiesTouchedSoFar.Count; i++)
                    {
                        if (i+1 < fingerTouchSequence.EntitiesTouchedSoFar.Count)
                        {
                            if (!fingerTouchSequence.EntitiesTouchedSoFar[i].HasComponent<BoardCellChildEntityComponent>())
                            {
                                continue;
                            }

                            if (!fingerTouchSequence.EntitiesTouchedSoFar[i+1].HasComponent<BoardCellChildEntityComponent>())
                            {
                                continue;
                            }

                            var starta = fingerTouchSequence.EntitiesTouchedSoFar[i].GetComponent<BoardCellChildEntityComponent>().Cell.Center;
                            var enda = fingerTouchSequence.EntitiesTouchedSoFar[i+1].GetComponent<BoardCellChildEntityComponent>().Cell.Center;

                            var color = _colorsHelper.MarbleColorToColor(fingerTouchSequence.Color);

                            _lineRenderer.DrawLine(starta, enda, 4, color);


                            //_spriteBatch.DrawLine2(starta, enda, GetColor(fingerTouchSequence.Color), 5);
                        }
                    }


                    if (!fingerTouchSequence.LastTouchedEntityInSequence.HasComponent<BoardCellChildEntityComponent>())
                    {
                        continue;
                    }
                    var start = fingerTouchSequence.LastTouchedEntityInSequence.GetComponent<BoardCellChildEntityComponent>().Cell.Center;

                    foreach (var touchLocation in _multitouchHelper.CurrentTouches)
                    {
                        if (touchLocation.State == TouchLocationState.Moved)
                        {
                            if (touchLocation.Id != fingerTouchSequence.FingerId)
                            {
                                continue;
                            }

                            var end = _resolution.ScaleMouseToScreenCoordinates(touchLocation.Position);

                            var color = _colorsHelper.MarbleColorToColor(fingerTouchSequence.Color);

                            _lineRenderer.DrawArrowedLine(start, end, 4, color);                           

                            break;
                        }
                    }

                }


        }

    }

    public class ClearedShapeData
    {
        public Color Color;
        public List<Vector2> Points;
        public Vector2[] PointDirections;
        public Vector2 Centroid;

        public bool IsFinished;
    }
}