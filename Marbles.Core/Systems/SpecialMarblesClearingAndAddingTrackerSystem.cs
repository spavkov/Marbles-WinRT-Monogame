using System;
using System.Collections.Generic;
using System.Diagnostics;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Components.SpecialMarbles;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;
using System.Linq;

namespace Marbles.Core.Systems
{
    public class SpecialMarblesClearingAndAddingTrackerSystem : IWorldUpdatingSystem, ISpecialItemsCountTracker
    {
        private World _world;
        private readonly Dictionary<GameEntity, SpecialMarblePotential> _currentLevelRunSpecialMarblePotentials = new Dictionary<GameEntity,SpecialMarblePotential>();

        private CurrentGameInformationTrackingSystem _currentGameInformationTrackingSystem;
        private LevelScoringSystem _levelScoringSys;
        private BoardRandomizationSystem _boardRandomizationSys;
        private readonly List<SpecialMarblePotential> _powerupPotentialsToRemove = new List<SpecialMarblePotential>();
        private SpecialMarblesClearingPostEffectsSystem _specialMarbleClearingPostEffectsSystem;
        private MarbleGameLevelControllerSystem _controller;
        private List<SpecialMarblePotential> _powerupsThatLostTheirPower = new List<SpecialMarblePotential>();

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }
        public void Initialize(World world)
        {
            _world = world;
        }

        public void Start()
        {
            _touchSquenceSys = _world.GetSystem<ITouchSequenceSystem>();
            if (_touchSquenceSys != null)
            {
                _touchSquenceSys.TouchSequenceEnded += OnTouchSequenceEnded;
            }

            _boardRandomizationSys = _world.GetSystem<BoardRandomizationSystem>();

            if (_boardRandomizationSys != null)
                _boardRandomizationSys.CellsRandomized += OnCellsRandomized;

            _currentGameInformationTrackingSystem = _world.GetSystem<CurrentGameInformationTrackingSystem>();

            _levelScoringSys = _world.GetSystem<LevelScoringSystem>();

            _specialMarbleClearingPostEffectsSystem = _world.GetSystem<SpecialMarblesClearingPostEffectsSystem>();

            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();

            _controller.LevelStopped += OnLevelStopped;
        }

        private void OnLevelStopped(object sender, EventArgs e)
        {
            Cleanup();
        }

        private void OnCellsRandomized(object sender, IEnumerable<BoardCell<GameEntity>> e)
        {
/*            var entitiesRandomized = e.Select(s => s.Item);
            var presek = _currentLevelRunSpecialMarblePotentials.Keys.Intersect(entitiesRandomized).ToList();

            foreach (var potential in presek)
            {
                _currentLevelRunSpecialMarblePotentials.Remove(potential);
            }*/
        }

        private void OnTouchSequenceEnded(object sender, FingerTouchSequenceEndedArgs e)
        {
            if (!_currentLevelRunSpecialMarblePotentials.Any())
            {
                return;
            }

            var clearedCells = e.CellsTouchedInSequenceSoFar;

            foreach (var powerupPotential in _currentLevelRunSpecialMarblePotentials)
            {
                if (!_world.EntityManager.IsAlive(powerupPotential.Key))
                {
                    _powerupPotentialsToRemove.Add(powerupPotential.Value);
                    continue;
                }

                var cellThatHoldsIt = powerupPotential.Value.Entity.GetComponent<BoardCellChildEntityComponent>().Cell;
                var neigbourItems = GetImmediateNeigbouringCellsForPowerup(cellThatHoldsIt);
                if (neigbourItems.TrueForAll(clearedCells.Contains))
                {
                    Debug.WriteLine("Sending info that special marble has been cleared...");
                    _specialMarbleClearingPostEffectsSystem.SpecialMarblePotentialHasBeenFulfilled(powerupPotential.Value.SpecialMarbleComponent, powerupPotential.Value.Entity);
                    _powerupPotentialsToRemove.Add(powerupPotential.Value);
                }
            }


        }

        public void SpecialMarbleWasAddedToCell(BoardCell<GameEntity> boardCell)
        {
            var entity = boardCell.Item;
            if (entity == null)
            {
                return;
            }

            var specialBoardItems = entity.Components.OfType<SpecialMarbleComponent>().ToList();

            foreach (var specialBoardItem in specialBoardItems)
            {
                AddCurrentLevelPotential(specialBoardItem, entity);
            }
        }

        private void AddCurrentLevelPotential(SpecialMarbleComponent specialMarbleComponent, GameEntity entity)
        {
            var potential = new SpecialMarblePotential()
                                {
                                    SpecialMarbleComponent = specialMarbleComponent,
                                    Entity = entity
                                };


            _currentLevelRunSpecialMarblePotentials.Add(entity, potential);
            Debug.WriteLine("Added special marble potential {0}", potential.SpecialMarbleComponent.SpecialMarbleType);
        }

        private List<BoardCell<GameEntity>> GetImmediateNeigbouringCellsForPowerup(BoardCell<GameEntity> boardCell)
        {
            return boardCell.Neighbours.Where(
                nd =>
                nd.Key == NeighbourSide.Left || nd.Key == NeighbourSide.Right || nd.Key == NeighbourSide.Up ||
                nd.Key == NeighbourSide.Down).Select(s => s.Value).ToList();
        }

        public void Stop()
        {
            if (_touchSquenceSys != null)
            {
                _touchSquenceSys.TouchSequenceEnded -= OnTouchSequenceEnded;
            }

            if (_boardRandomizationSys != null)
            {
                _boardRandomizationSys.CellsRandomized -= OnCellsRandomized;
            }

            if (_controller != null)
            {
                _controller.LevelStopped -= OnLevelStopped;
            }

            Cleanup();
        }

        public void Update(GameTime gameTime)
        {
            if (_currentGameInformationTrackingSystem.IsLevelRunning)
            {
                if (_powerupPotentialsToRemove.Any())
                {
                    foreach (var specialMarblePotential in _powerupPotentialsToRemove)
                    {
                        _currentLevelRunSpecialMarblePotentials.Remove(specialMarblePotential.Entity);
                    }
                    _powerupPotentialsToRemove.Clear();
                }
                if (_powerupsThatLostTheirPower.Any())
                {
                    foreach (var specialMarblePotential in _powerupsThatLostTheirPower)
                    {
                        _currentLevelRunSpecialMarblePotentials.Remove(specialMarblePotential.Entity);
                    }
                    _powerupsThatLostTheirPower.Clear();
                }

                foreach (var currentLevelRunSpecialMarblePotential in _currentLevelRunSpecialMarblePotentials)
                {
                    if (!currentLevelRunSpecialMarblePotential.Value.Entity.HasComponent<SpecialMarbleComponent>())
                    {
                        _powerupsThatLostTheirPower.Add(currentLevelRunSpecialMarblePotential.Value);
                    }

                    if (currentLevelRunSpecialMarblePotential.Value.SpecialMarbleComponent.SpecialMarbleType ==
                        SpecialMarbleType.GameOverMarble)
                    {
                        var detail = currentLevelRunSpecialMarblePotential.Value.SpecialMarbleComponent.Details as GameOverSpecialMarbleDetails;
                        detail.RemainingTimeInSeconds -= (float) gameTime.ElapsedGameTime.TotalSeconds;
                        if (detail.RemainingTimeInSeconds <= 0)
                        {
                            detail.RemainingTimeInSeconds = 0;
                            _controller.MakeThisLevelFailed();
                        }
                    }
                }
            }
        }

        private void Cleanup()
        {
            _currentLevelRunSpecialMarblePotentials.Clear();
            _powerupPotentialsToRemove.Clear();
        }

        private class SpecialMarblePotential
        {
            public SpecialMarbleComponent SpecialMarbleComponent;

            public GameEntity Entity;
        }

        private ITouchSequenceSystem _touchSquenceSys { get; set; }

        public int CurrentNumberOfSpecialMarbles
        {
            get { return _currentLevelRunSpecialMarblePotentials.Count; }
        }
    }

    public interface ISpecialItemsCountTracker
    {
        int CurrentNumberOfSpecialMarbles { get; }
        
    }
}