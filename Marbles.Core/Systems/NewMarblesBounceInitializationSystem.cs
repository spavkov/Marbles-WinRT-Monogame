using System;
using System.Collections.Generic;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;
using System.Linq;

namespace Marbles.Core.Systems
{
    public class NewMarblesBounceInitializationSystem : IWorldUpdatingSystem
    {
        private World _world;
        private Random _rnd;
        private Aspect _aspectForStart;
        private Aspect _aspectForAllThatNeedToBeInitialized;
        private CurrentGameInformationTrackingSystem _gameInfo;
        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }


        public void Initialize(World world)
        {
            _world = world;
            _aspectForAllThatNeedToBeInitialized = new Aspect().HasAllOf(typeof(BoardCellChildEntityComponent), typeof(NeedsToBeBouncedDownComponent)).ExcludeAllOf(typeof(VerticalBounceComponent));
            _rnd = new Random();
        }

        public NewMarblesBounceInitializationSystem()
        {

        }

        public void Start()
        {
            _gameInfo = _world.GetSystem<CurrentGameInformationTrackingSystem>();

            var allChildrenOfCells = _world.EntityManager.GetLiveEntities(_aspectForStart);

            foreach (var entityThatIsCellChild in allChildrenOfCells)
            {
                var cell = entityThatIsCellChild.GetComponent<BoardCellChildEntityComponent>();
                AddBounceComponentToEntity(cell, entityThatIsCellChild);
            }
        }

        private void AddBounceComponentToEntity(BoardCellChildEntityComponent cell,
                                                GameEntity entityThatIsCellChild)
        {
            var bounceComponent = new VerticalBounceComponent();
            bounceComponent.Height = GameConstants.MarbleRadius/2;
            bounceComponent.InitialVelocity = _rnd.Next(350, 700);
            bounceComponent.BounceCoeficient = ((float) _rnd.Next(1, 20))/100;
            bounceComponent.DestinationPosition = cell.Cell.Center;
            entityThatIsCellChild.AddComponent(bounceComponent);
        }

        public void Stop()
        {
            
        }

        public void Update(GameTime gameTime)
        {
            if (!_gameInfo.IsLevelRunning)
                return;

            var allChildrenOfCells = _world.EntityManager.GetLiveEntities(_aspectForAllThatNeedToBeInitialized);

            foreach (var entityThatIsCellChild in allChildrenOfCells)
            {
                entityThatIsCellChild.RemoveComponent<NeedsToBeBouncedDownComponent>();
                var cell = entityThatIsCellChild.GetComponent<BoardCellChildEntityComponent>();
                AddBounceComponentToEntity(cell, entityThatIsCellChild);
            }
        }
    }
}