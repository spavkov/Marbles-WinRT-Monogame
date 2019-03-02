using System;
using System.Collections.Generic;
using System.Diagnostics;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Roboblob.GameEntitySystem.WinRT;
using Roboblob.XNA.WinRT.Camera;
using Roboblob.XNA.WinRT.Content;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems.ParticleEmitters;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems.ParticleModifiers.Alpha;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems.ParticleModifiers.Color;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems.ParticleModifiers.Gravity;
using Roboblob.XNA.WinRT.GfxEffects.AdvancedParticleSystems.ParticleModifiers.Scale;
using System.Linq;
using Roboblob.XNA.WinRT.GfxEffects.Lightning;
using Roboblob.XNA.WinRT.Helpers;
using Windows.UI.Xaml;

namespace Marbles.Core.Systems
{
    public class MarbleSpecialEffectsRenderingSystem : IWorldRenderingSystem
    {
        private Game _game;
        private World _world;
        private Camera2D _camera;
        private ITextureSheetLoader _textureSheetLoader;
        private CurrentGameInformationTrackingSystem _gameInfo;
        private ParticleSystem _individualMarblesExplosionSystem;
        private TextureSheet _gameArtSheet;
        private ParticleSystem _burnersParticleSystem;
        private BoardRandomizationSystem _boardRandomizationSys;
        private Dictionary<GameEntity, IParticleEmitter> _burningMarbleEmitters = new Dictionary<GameEntity, IParticleEmitter>();
        private Dictionary<GameEntity, ElectricMarbleEmitterData> _electricMarbleEmitters = new Dictionary<GameEntity, ElectricMarbleEmitterData>();
        private List<GameEntity> _burningMarblesToRemove = new List<GameEntity>();
        private object _burningMarblesLocker = new object();
        private MarbleGameLevelControllerSystem _controller;
        private LightningArt _lightningArt;
        private static Random _rnd = new Random();
        private int NumberOfCellsThatElectricMarbleIsShootingLightningaAt = 10;
        private int MaxNumberOfCellsThatElectricMarbleCanShootAtSameTime = 3;
        private float MinNumberOfSecondsBetweenElectricMarbleShootings = 0.5f;
        private float MaxNumberOfSecondsBetweenElectricMarbleShootings = 1.5f;
        private List<GameEntity> _electricMarblesToRemove = new List<GameEntity>();
        private SpriteBatch _spriteBatch;
        private MarbleColorsHelper _marbleColorsHelper;
        private int NumberOfLightningsInsideElectricMarble = 10;
        private List<ILightning> _generalLightnings = new List<ILightning>();
        private readonly List<ILightning> _generalLightingsToRemove = new List<ILightning>();
        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }

        private MarbleTexturesHelper _marbleTexturesHelper;
        private Vector2 _marbleTextureOriginVector;
        private readonly List<MarbleRemovalIndicatorData> _marbleRemovalIndicators = new List<MarbleRemovalIndicatorData>();
        private const float MarbleRemovalIndicatorFadeoutSpeed = 6.5f;
        private readonly List<MarbleRemovalIndicatorData> _marbleRemovalIndicatorsToRemove = new List<MarbleRemovalIndicatorData>();
        private Rectangle _currentMarbleDestRect;
        private Rectangle _currentRect;
        private MarbleSoundsSystem _soundSys;
        private const float MarbleRemovalIndicatorGrowSpeed = 2.8f;

        public MarbleSpecialEffectsRenderingSystem(Game game)
        {
            _game = game;
            _camera = game.Services.GetService(typeof (Camera2D)) as Camera2D;
            _textureSheetLoader = game.Services.GetService(typeof (ITextureSheetLoader)) as ITextureSheetLoader;
            _spriteBatch = new SpriteBatch(_game.GraphicsDevice);
            _marbleColorsHelper = new MarbleColorsHelper();
        }

        public void Initialize(World world)
        {
            _world = world;

/*            _individualMarblesExplosionSystem = new ParticleSystem(_game, _camera, 5000);
            _individualMarblesExplosionSystem.Modifiers.Add(new ExplosionsCombinedModifier());*/

            _burnersParticleSystem = new ParticleSystem(_game, _camera, 10000);
            _burnersParticleSystem.Modifiers.Add(new BurnersCombinedModifier());
        }

        public void Start()
        {
            _gameInfo = _world.GetSystem<CurrentGameInformationTrackingSystem>();
            _boardRandomizationSys = _world.GetSystem<BoardRandomizationSystem>();
            if (_boardRandomizationSys != null)
            {
                _boardRandomizationSys.MarblesRemoved += OnMarblesRemoved;
            }

            _soundSys = _world.GetSystem<MarbleSoundsSystem>();

            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();
            _controller.LevelStopped += OnLevelStopped;
        }

        private void OnLevelStopped(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void OnMarblesRemoved(object sender, List<GameEntity> e)
        {
            lock (_burningMarblesLocker)
            {
                foreach (var gameEntity in e)
                {
                    if (_burningMarbleEmitters.ContainsKey(gameEntity))
                    {
                        _burningMarblesToRemove.Add(gameEntity);
                    }
                    else if (_electricMarbleEmitters.ContainsKey(gameEntity))
                    {
                        _electricMarblesToRemove.Add(gameEntity);
                    }
                }
            }
        }

        private class MarbleScreenDataDynamicPosition : IDynamicPositionInformation
        {
            private MarbleScreenDataComponent _screenData;

            public MarbleScreenDataDynamicPosition(MarbleScreenDataComponent marbleScreenDataComponent)
            {
                _screenData = marbleScreenDataComponent;
            }

            public void GetPosition(out Vector2 position)
            {
                position = _screenData.Position;
            }
        }

        private class MarbleRemovalIndicatorData
        {
            public MarbleColor MarbleColor;
            public Color CurrentDisplayColor = Color.White;
            public Vector2 Position;
            public float CurrentScale = 0.6f;
        }

        public void AddCellExplosion(BoardCell<GameEntity> cell)
        {
            if (cell != null && cell.Item != null)
            {
                var center = cell.Center;
                var color = cell.Item.GetComponent<MarbleComponent>().Color;

                var indicatorData = new MarbleRemovalIndicatorData()
                    {
                        MarbleColor = color,
                        Position = center,
                    };

                _marbleRemovalIndicators.Add(indicatorData);
            }
        }

        public void Stop()
        {
            if (_boardRandomizationSys != null)
            {
                _boardRandomizationSys.MarblesRemoved -= OnMarblesRemoved;
            }

            if (_controller != null)
            {
                _controller.LevelStopped -= OnLevelStopped;
            }

            Cleanup();
        }

        private void Cleanup()
        {
            if (_individualMarblesExplosionSystem != null)
            {
                _individualMarblesExplosionSystem.Emitters.Clear();
                _individualMarblesExplosionSystem.Cleanup();              
            }

            if (_burnersParticleSystem != null)
            {
                _burnersParticleSystem.Emitters.Clear();
                _burnersParticleSystem.Cleanup();
            }

            _burningMarblesToRemove.Clear();
            _burningMarbleEmitters.Clear();

            _electricMarbleEmitters.Clear();
            _electricMarblesToRemove.Clear();

            _marbleRemovalIndicators.Clear();
            _marbleRemovalIndicatorsToRemove.Clear();
        }

        public void Update(GameTime gameTime)
        {
            if (!_gameInfo.IsLevelRunning)
                return;

            //_individualMarblesExplosionSystem.Update(gameTime);

            lock (_burningMarblesLocker)
            {
                if (_burningMarblesToRemove.Any())
                {
                    foreach (var gameEntity in _burningMarblesToRemove)
                    {
                        if (_burningMarbleEmitters.ContainsKey(gameEntity))
                        {
                            _burningMarbleEmitters[gameEntity].Cleanup();
                            _burnersParticleSystem.Emitters.Remove(_burningMarbleEmitters[gameEntity]);
                            _burningMarbleEmitters.Remove(gameEntity);
                        }
                    }
                }

                _burningMarblesToRemove.Clear();

                if (_electricMarblesToRemove.Any())
                {
                    foreach (var gameEntity in _electricMarblesToRemove)
                    {
                        if (_electricMarbleEmitters.ContainsKey(gameEntity))
                        {
                            _electricMarbleEmitters.Remove(gameEntity);
                        }
                    }
                    _electricMarblesToRemove.Clear();
                }

                if (_generalLightingsToRemove.Any())
                {
                    foreach (var lightning in _generalLightingsToRemove)
                    {
                        if (_generalLightingsToRemove.Contains(lightning))
                        {
                            _generalLightnings.Remove(lightning);
                        }
                    }
                    _generalLightingsToRemove.Clear();
                }
            }

            foreach (var burningMarbleEmitter in _burningMarbleEmitters)
            {
                if (burningMarbleEmitter.Key.IsBeingRemoved)
                {
                    _burningMarblesToRemove.Add(burningMarbleEmitter.Key);
                }
                else
                {
                    //burningMarbleEmitter.Value.Position = ExtractCurrentMarblePosition(burningMarbleEmitter.Key);                    
                }
            }

            foreach (var emitter in _electricMarbleEmitters)
            {
                if (emitter.Key.IsBeingRemoved)
                {
                    _electricMarblesToRemove.Add(emitter.Key);                    
                }
                else
                {
                    //emitter.Value.Emitter.Source = ExtractCurrentMarblePosition(emitter.Key);
                    emitter.Value.Emitter.Update(gameTime);                   
                }
            }

            foreach (var generalLightning in _generalLightnings)
            {
                if (generalLightning.IsComplete)
                {
                    _generalLightingsToRemove.Add(generalLightning);
                }
                else
                {
                    generalLightning.Update();
                }
            }

            foreach (var indicator in _marbleRemovalIndicators)
            {
                var secs = (float) gameTime.ElapsedGameTime.TotalSeconds;
                indicator.CurrentScale += secs*MarbleRemovalIndicatorGrowSpeed;

                indicator.CurrentDisplayColor *= (1 - (MarbleRemovalIndicatorFadeoutSpeed * secs));

                if (indicator.CurrentDisplayColor.A <= 0)
                {
                    _marbleRemovalIndicatorsToRemove.Add(indicator);
                    indicator.CurrentDisplayColor.A = 0;
                }
            }

            if (_marbleRemovalIndicatorsToRemove.Any())
            {
                foreach (var marbleRemovalIndicatorData in _marbleRemovalIndicatorsToRemove)
                {
                    _marbleRemovalIndicators.Remove(marbleRemovalIndicatorData);
                }
            }

            _burnersParticleSystem.Update(gameTime);
        }

        public void Render(GameTime gameTime)
        {
            //_individualMarblesExplosionSystem.Draw(gameTime);
            _burnersParticleSystem.Draw(gameTime);

            if (_electricMarbleEmitters.Any())
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, _camera.GetViewTransformationMatrix());

                foreach (var emitter in _electricMarbleEmitters)
                {
                    emitter.Value.Emitter.Draw(gameTime, _spriteBatch);
                }

                foreach (var generalLightning in _generalLightnings)
                {
                    if (!generalLightning.IsComplete)
                    {
                        generalLightning.Draw(_spriteBatch);
                    }
                }

                _spriteBatch.End();
            }


            if (_marbleRemovalIndicators.Any())
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, _camera.GetViewTransformationMatrix());

                foreach (var marbleRemovalIndicatorData in _marbleRemovalIndicators)
                {
                    _currentMarbleDestRect.X = (int)marbleRemovalIndicatorData.Position.X;
                    _currentMarbleDestRect.Y = (int)marbleRemovalIndicatorData.Position.Y;
                    _currentMarbleDestRect.Width = GameConstants.MarbleRadius;
                    _currentMarbleDestRect.Height = GameConstants.MarbleRadius;

                    _marbleTexturesHelper.GetMarbleRectable(marbleRemovalIndicatorData.MarbleColor, out _currentRect);

                    RectangleHelper.Scale(ref _currentMarbleDestRect, marbleRemovalIndicatorData.CurrentScale);

                    _spriteBatch.Draw(_gameArtSheet.Texture, _currentMarbleDestRect, _currentRect, marbleRemovalIndicatorData.CurrentDisplayColor, 0f, _marbleTextureOriginVector, SpriteEffects.None, 0f);
                }

                _spriteBatch.End();
            }

            _spriteBatch.End();

        }

        public void LoadContent()
        {
            _gameArtSheet = _textureSheetLoader.Load(@"SpriteSheets\GameArt");

/*            _individualMarblesExplosionSystem.TextureSheet = _gameArtSheet;
            _individualMarblesExplosionSystem.ParticleSubtextureNames.Add("circle");
            _individualMarblesExplosionSystem.ParticleSubtextureNames.Add("diamond");
            _individualMarblesExplosionSystem.ParticleSubtextureNames.Add("star");*/

            _marbleTexturesHelper = new MarbleTexturesHelper(_gameArtSheet);

            _marbleTextureOriginVector = _marbleTexturesHelper.GetMarbleTextureOriginVector();

            _lightningArt = new LightningArt();
            _lightningArt.LightningSegment = _game.Content.Load<Texture2D>(@"LightningBolts\Lightning Segment");
            _lightningArt.HalfCircle = _game.Content.Load<Texture2D>(@"LightningBolts\Half Circle");
            _lightningArt.Pixel = _game.Content.Load<Texture2D>(@"LightningBolts\Pixel");

            _burnersParticleSystem.TextureSheet = _gameArtSheet;
            _burnersParticleSystem.ParticleSubtextureNames.Add("circle");
        }

        private class ExplosionsCombinedModifier : IParticleModifier
        {
            private IParticleModifier _m1, _m2, _m3;

            public ExplosionsCombinedModifier()
            {
                _m1 = (new AlphaAgeModifier() { FadeIn = false });
                _m2 = new ScaleAgeModifier() { StartScale = new Vector2(0.5f, 0.5f), EndScale = new Vector2(0.1f, 0.1f) };
                _m3 = new ColorAgeModifier() { StartColor = Color.OrangeRed, EndColor = Color.Yellow };
            }

            public void Update(GameTime gameTime, Particle particle, float ageInPercents)
            {
                _m1.Update(gameTime, particle, ageInPercents);
                _m2.Update(gameTime, particle, ageInPercents);
                _m3.Update(gameTime, particle, ageInPercents);
            }
        }

        private class BurnersCombinedModifier : IParticleModifier
        {
            private IParticleModifier _m1, _m2, _m3, _m4;
            private Vector2 _gravityForBurners = new Vector2(0, -25f);

            public BurnersCombinedModifier()
            {
                _m1 = (new AlphaAgeModifier() { FadeIn = true });
                _m2 = new ScaleAgeModifier() { StartScale = new Vector2(0.5f, 0.5f), EndScale = new Vector2(0.0f, 0.0f) };
                _m3 = new DirectionalPullModifier {Gravity = _gravityForBurners};
                _m4 = new ColorAgeModifier() { StartColor = Color.OrangeRed, EndColor = Color.Yellow };
            }

            public void Update(GameTime gameTime, Particle particle, float ageInPercents)
            {
                _m1.Update(gameTime, particle, ageInPercents);
                _m2.Update(gameTime, particle, ageInPercents);
                _m3.Update(gameTime, particle, ageInPercents);
                _m4.Update(gameTime, particle, ageInPercents);
            }
        }

        private class MarbleScreenDataComponentListLightningSourceInformation : ILightningSourceInformation
        {
            private readonly MarbleScreenDataComponent _screenData;

            public MarbleScreenDataComponentListLightningSourceInformation(MarbleScreenDataComponent screenData)
            {
                _screenData = screenData;
            }

            public void GetSource(out Vector2 sourcePoint)
            {
                sourcePoint = _screenData.Position;
            }
        }


        private class MarbleScreenDataComponentListLightningTargetsInformation : ILightningTargetsInformation
        {
            private readonly List<MarbleScreenDataComponent> _list;

            public MarbleScreenDataComponentListLightningTargetsInformation(List<MarbleScreenDataComponent> list)
            {
                _list = list;               
            }

            public void GetTarget(int index, out Vector2 targetPoint)
            {
                targetPoint = _list[index].Position;
            }

            public int Count
            {
                get { return _list.Count; }
            }

            public void AddTarget(MarbleScreenDataComponent component)
            {
                _list.Add(component);
            }
        }

        public void StartMarbleElectricityCulmination(GameEntity marble, List<GameEntity> targetEntities)
        {
            ElectricMarbleEmitterData sourceMarbleEmitterData;
            if (!_electricMarbleEmitters.TryGetValue(marble, out sourceMarbleEmitterData))
            {
                return;
            }

            var targets =
                new MarbleScreenDataComponentListLightningTargetsInformation(
                    targetEntities.Select(e => e.GetComponent<MarbleScreenDataComponent>()).ToList());

            sourceMarbleEmitterData.Emitter = new LightningEmitter(_lightningArt, new MarbleScreenDataComponentListLightningSourceInformation(marble.GetComponent<MarbleScreenDataComponent>()),   targets);

            sourceMarbleEmitterData.Emitter.MinNumberOfTargetsToHitOnEachEmit = targets.Count / 2;
            sourceMarbleEmitterData.Emitter.MaxNumberOfTargetsToHitOnEachEmit = targets.Count;

            sourceMarbleEmitterData.Emitter.MinSecondsBetweenEmit = 0.2f;
            sourceMarbleEmitterData.Emitter.MaxSecondsBetweenEmit = 0.4f;

            sourceMarbleEmitterData.Emitter.MinPauseBetweenBurstsInSingleEmitInSeconds = 0.2f;
            sourceMarbleEmitterData.Emitter.MaxPauseBetweenBurstsInSingleEmitInSeconds = 0.5f;

            sourceMarbleEmitterData.Emitter.MinEmitDurationInSeconds = 0.6f;
            sourceMarbleEmitterData.Emitter.MaxEmitDurationInSeconds = 1.2f;

            sourceMarbleEmitterData.Emitter.ForceImmediateEmit();

            sourceMarbleEmitterData.Emitter.DoingSingleEmitBurst += OnSingleLightingBurst;
        }

        public void StartMarbleElectricity(BoardCell<GameEntity> cell, GameEntity marble)
        {
            if (marble == null)
            {
                return;
            }

            var marbleData = marble.GetComponent<MarbleComponent>();

            var color = _marbleColorsHelper.MarbleColorToColor(marbleData.Color);

            if (cell.HasNeighbourCell(NeighbourSide.Down))
            {
                var destCell = cell.Neighbours[NeighbourSide.Down];
                var destItem = cell.Neighbours[NeighbourSide.Down].Item;
                if (destItem != null)
                {
                    _generalLightnings.Add(new BranchLightning(cell.Center, destCell.Center, color,  _lightningArt, 2f));
                }
            }

            _soundSys.PlayElectricitySound();

            var cellComponent = marble.GetComponent<BoardCellChildEntityComponent>().Cell;
            var targetsInfor =
                new MarbleScreenDataComponentListLightningTargetsInformation(
                    cellComponent.Neighbours.Select(n => n.Value.Item.GetComponent<MarbleScreenDataComponent>())
                                 .ToList());

            var sourceInfo =
                new MarbleScreenDataComponentListLightningSourceInformation(
                    marble.GetComponent<MarbleScreenDataComponent>());

            var emitter = new ElectricMarbleEmitterData()
                {
                    Cell = cell,
                    Marble = marble,
                    Color = marbleData.Color,
                    Emitter = new LightningEmitter(_lightningArt, sourceInfo,  targetsInfor)
                        {
                            Colors = new List<Color>
                                {
                                    //color,
                                    Color.Silver
                                }
                        }
                };
            emitter.Emitter.DoingSingleEmitBurst += OnSingleLightingBurst;

            var board = cellComponent.Board;

            var toAdd = NumberOfCellsThatElectricMarbleIsShootingLightningaAt - cellComponent.Neighbours.Count;
            for (int i = 0; i < toAdd; i++)
            {
                targetsInfor.AddTarget(board.AllCells[_rnd.Next(board.AllCells.Count)].Item.GetComponent<MarbleScreenDataComponent>());
            }

            /*                var radius = _constants.MarbleRadius / 2;
                            for (int i = 0; i < NumberOfLightningsInsideElectricMarble; i++)
                            {
                                var rads = (float)(_rnd.NextDouble() * MathHelper.TwoPi);
                                var offsetX = (float) Math.Cos(rads) * radius;
                                var offsetY = (float) Math.Sin(rads) * radius;

                                var dest = new Vector2(emitter.Emitter.Source.X + offsetX, emitter.Emitter.Source.Y + offsetY);
                                emitter.Emitter.Targets.Add(dest);
                            }*/

            emitter.Emitter.MinSecondsBetweenEmit = 0.4f;
            emitter.Emitter.MaxSecondsBetweenEmit = 0.7f;

            emitter.Emitter.MinNumberOfTargetsToHitOnEachEmit = MaxNumberOfCellsThatElectricMarbleCanShootAtSameTime/2;
            emitter.Emitter.MaxNumberOfTargetsToHitOnEachEmit = MaxNumberOfCellsThatElectricMarbleCanShootAtSameTime;

            emitter.Emitter.MinEmitDurationInSeconds = 0.4f;
            emitter.Emitter.MaxEmitDurationInSeconds = 0.61f;

            emitter.Emitter.MinPauseBetweenBurstsInSingleEmitInSeconds = 0.2f;
            emitter.Emitter.MaxPauseBetweenBurstsInSingleEmitInSeconds = 0.3f;

            _electricMarbleEmitters.Add(marble, emitter);

            emitter.Emitter.ForceImmediateEmit();
        }

        private void OnSingleLightingBurst(object sender, EventArgs e)
        {
            _soundSys.PlayElectricitySound();
        }

        public void AddMainMarbleBurn(GameEntity marble)
        {           
            CreateMarbleBurningEmitter(marble, true);
            _soundSys.StartPlayingBurnSound();
        }

        private void CreateMarbleBurningEmitter(GameEntity marble, bool isMain)
        {
            if (marble != null)
            {
                if (_burningMarbleEmitters.ContainsKey(marble))
                {
                    return;
                }

                var emitter = new ConstantCircleParticleEmitter(isMain ? 200 : 100, GameConstants.MarbleRadius / 2, new MarbleScreenDataDynamicPosition(marble.GetComponent<MarbleScreenDataComponent>()));
                emitter.ParticleLifespanInSeconds = 1f;
                emitter.ParticleStartColor = isMain ? Color.OrangeRed : Color.Orange;
                emitter.ParticleStartAlpha = 0;
                emitter.ParticleStartingSpeed = 8f;
                emitter.ParticleSubtextureNames.Add("circle");
                _burnersParticleSystem.Emitters.Add(emitter);

                _burningMarbleEmitters.Add(marble, emitter);
            }
        }

        private class ElectricMarbleEmitterData
        {
            public BoardCell<GameEntity> Cell;
            public GameEntity Marble;
            public LightningEmitter Emitter;
            public MarbleColor Color;
        }

        public void StartBurningMarbleCulmination(GameEntity item, List<GameEntity> targets)
        {
            if (item == null)
            {
                return;
            }

            foreach (var marble in targets)
            {
                CreateMarbleBurningEmitter(marble, false);
            }
        }
    }
}