using System;
using System.Collections.Generic;
using System.Linq;
using Marbles.Core.Helpers;
using Marbles.Core.Model.Components;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Systems
{
    public class MarbleVerticalBouncerSys : IWorldUpdatingSystem
    {
        private Aspect _aspect;
        private World _world;


        public MarbleVerticalBouncerSys()
        {
        }

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }
        public void Initialize(World world)
        {
            _world = world;
            _aspect = new Aspect().HasAllOf(typeof(VerticalBounceComponent));
        }

        public void Start()
        {
            _gameInfo = _world.GetSystem<CurrentGameInformationTrackingSystem>();
            _soundSys = _world.GetSystem<MarbleSoundsSystem>();
            _expolsionsSys = _world.GetSystem<MarbleSpecialEffectsRenderingSystem>();
        }

        public void Stop()
        {
            
        }

        public void Update(GameTime gameTime)
        {
            if (!_gameInfo.IsLevelRunning)
                return;

            var entitiesToBounce = _world.EntityManager.GetLiveEntities(_aspect);

            if (!entitiesToBounce.Any())
            {
                return;
            }

            var rows = entitiesToBounce.Select(s => s.GetComponent<BoardCellChildEntityComponent>().Cell.Row).Distinct().ToList();

            foreach (var gameEntity in entitiesToBounce)
            {
                var bounceComponent = gameEntity.GetComponent<VerticalBounceComponent>();

                var cellChildComponent = gameEntity.GetComponent<BoardCellChildEntityComponent>();

                if (!bounceComponent.Started)
                {
                    InitializeEntityScreenDataIfNeeded(gameEntity, bounceComponent, rows);
                    UpdateBounceComponentOfEntity(bounceComponent, cellChildComponent, gameTime);
                }
                else
                {
                    UpdateBounceComponentOfEntity(bounceComponent, cellChildComponent, gameTime);
                }

                var screenData = gameEntity.GetComponent<MarbleScreenDataComponent>();

                screenData.Position = bounceComponent.CurrentPosition;

                RecalculateCurrentRotation(screenData, gameTime);

                if (bounceComponent.Finished)
                {
                    var touchable = gameEntity.GetComponent<TouchableComponent>();                                   

                    touchable.Bounds = new BoundingSphere(
                        new Vector3(bounceComponent.DestinationPosition.X, bounceComponent.DestinationPosition.Y, 0),
                        screenData.Radius / 2);
                    touchable.IsTouchable = true;

                    gameEntity.RemoveComponent<VerticalBounceComponent>();

                    if (gameEntity.HasComponent<ShouldGetNewSpecialMarbleComponent>())
                    {
                        var componentToGet = gameEntity.GetComponent<ShouldGetNewSpecialMarbleComponent>();
                        gameEntity.RemoveComponent<ShouldGetNewSpecialMarbleComponent>();

                        switch (componentToGet.EffectType)
                        {
                            case PostEffectType.Electric:
                                gameEntity.AddComponent(new ElectricMarbleComponent());
                                _expolsionsSys.StartMarbleElectricity(cellChildComponent.Cell, gameEntity);
                                break;
                            case PostEffectType.Burn:
                                gameEntity.AddComponent(new BurningMarbleComponent());
                                _expolsionsSys.AddMainMarbleBurn(gameEntity);
                                break;
                            default:
                                throw new Exception("Not covered new special marble effect type: " + componentToGet.EffectType);
                        }
                    }
                }
            }
        }

        private void RecalculateCurrentRotation(MarbleScreenDataComponent screenData, GameTime gameTime)
        {
            var newRotationAngle = screenData.RotationAngle + (10* (float) gameTime.ElapsedGameTime.TotalSeconds);
            screenData.RotationAngle = newRotationAngle;

        }

        private float CalculateStartingYPosForMarble(BoardCellChildEntityComponent cell, MarbleScreenDataComponent screenData, List<int> rows)
        {
            var index = rows.IndexOf(cell.Cell.Row);
            return -(index * screenData.Radius);
        }

        private void InitializeEntityScreenDataIfNeeded(GameEntity gameEntity, VerticalBounceComponent bounceComponent, List<int> rows)
        {
            var cell = gameEntity.GetComponent<BoardCellChildEntityComponent>();
            if (!gameEntity.HasComponent<MarbleScreenDataComponent>())
            {
                var screenData = new MarbleScreenDataComponent()
                                     {
                                         Radius = GameConstants.MarbleRadius
                                     };
                screenData.Position = cell.Cell.Center;
                screenData.Position.Y = CalculateStartingYPosForMarble(cell, screenData, rows);
                screenData.RotationAngle = _rnd.Next(0, 360);

                bounceComponent.CurrentPosition = screenData.Position;

                gameEntity.AddComponent(screenData);
            }
            else
            {
                var screen = gameEntity.GetComponent<MarbleScreenDataComponent>();
                bounceComponent.CurrentPosition = screen.Position;
            }
        }

/*        private void RecalculateMarbleDestinationRectangle(MarbleScreenDataComponent screenData)
        {
            screenData.DestRectangle.X = (int)(screenData.Position.X - screenData.Radius / 2);
            screenData.DestRectangle.Y = (int)(screenData.Position.Y - screenData.Radius / 2);
            screenData.DestRectangle.Width = (int)screenData.Radius;
            screenData.DestRectangle.Height = (int)screenData.Radius;
        }*/

        public void UpdateBounceComponentOfEntity(VerticalBounceComponent component, BoardCellChildEntityComponent cellChildComponent, GameTime gameTime)
        {
            component.DestinationPosition = cellChildComponent.Cell.Center;

            if (component.Finished)
            {
                return;
            }

            if (!component.Started)
            {
                component.Started = true;
                component.CurrentVelocity = component.InitialVelocity;
            }

            if (component.CurrentPosition.Y >= component.DestinationPosition.Y && component.CurrentVelocity > 0)
            {
                if (Math.Abs(component.CurrentVelocity) <= 1)
                {
                    component.Finished = true;                    
                }
                else
                {
                    component.CurrentVelocity *= -component.BounceCoeficient;                
                }
            }

            component.CurrentVelocity += VerticalGravity;
            component.CurrentVelocity /= Friction;

            component.CurrentPosition.Y += (component.CurrentVelocity * (float)gameTime.ElapsedGameTime.TotalSeconds);

            if (component.Finished)
            {
                component.CurrentPosition = component.DestinationPosition;
            }
            else if (component.CurrentPosition.Y > component.DestinationPosition.Y)
            {
                component.CurrentPosition.Y = component.DestinationPosition.Y;

                if (component.CurrentVelocity < VerticalGravity)
                {
                    component.Finished = true;
                }
            }
        }

        public float VerticalGravity = 15.8f;
        public float Friction = 1.008f;
        private Random _rnd = new Random();
        private CurrentGameInformationTrackingSystem _gameInfo;
        private MarbleSoundsSystem _soundSys;
        private MarbleSpecialEffectsRenderingSystem _expolsionsSys;
    }
}