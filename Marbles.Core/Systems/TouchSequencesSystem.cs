using System;
using System.Collections.Generic;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Components.SpecialMarbles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using Roboblob.GameEntitySystem.WinRT;
using Roboblob.XNA.WinRT.Input;
using System.Linq;
using Roboblob.XNA.WinRT.ResolutionIndependence;

namespace Marbles.Core.Systems
{
    public class TouchSequencesSystem : ITouchSequenceSystem
    {
        private readonly Game _game;
        private World _world;
        private MultitouchHelper _multitouchHelper;
        private Dictionary<int, FingerTouchSequence> _currentSequences;
        private ResolutionIndependentRenderer _resolutionIndependentRenderer;
        private List<int> _ignoreFingersUntilPressed = new List<int>();
        private MarbleGameLevelControllerSystem _controller;
        private CurrentGameInformationTrackingSystem _gameInforamtion;
        private Aspect _touchableMarblesAspect;
        private Vector2 _currentPosition;
        private Vector2 _lastPosition;
        private double EPSILON = 0.1;
        private double DELTA = 1;
        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }

        public TouchSequencesSystem(Game game)
        {
            _game = game;
            _multitouchHelper = _game.Services.GetService(typeof (MultitouchHelper)) as MultitouchHelper;
            _resolutionIndependentRenderer =
                _game.Services.GetService(typeof (ResolutionIndependentRenderer)) as ResolutionIndependentRenderer;
            CurrentSequences = new Dictionary<int, FingerTouchSequence>();
        }

        public Dictionary<int, FingerTouchSequence> CurrentSequences
        {
            get { return _currentSequences; }
            set { _currentSequences = value; }
        }

        public void Initialize(World world)
        {
            _world = world;
            _touchableMarblesAspect = new Aspect().HasAllOf(typeof (MarbleComponent), typeof (TouchableComponent)).ExcludeAllOf(typeof(IsCurrentlyPartOfSpecialMarblePostEffect));
        }

        public void Start()
        {
            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();
            _controller.LevelStopped += OnLevelStopped;
            _gameInforamtion = _world.GetSystem<CurrentGameInformationTrackingSystem>();

        }

        private void OnLevelStopped(object sender, EventArgs e)
        {
            Cleanup();
        }

        public void Stop()
        {
            if (_controller != null)
            {
                _controller.LevelStopped -= OnLevelStopped;
            }
            Cleanup();
        }

        private void Cleanup()
        {
            CurrentSequences.Clear();
            _ignoreFingersUntilPressed.Clear();
        }

        public void Update(GameTime gameTime)
        {
            if (_controller == null || _gameInforamtion.LevelState != LevelState.Running)
            {
                return;           
            }

            if (!_multitouchHelper.WeHaveNewTouches())
            {
                return;
            }

            var touches = _multitouchHelper.CurrentTouches;

            foreach (var touchLocation in touches)
            {
                TouchLocation location = touchLocation;

                if (location.State == TouchLocationState.Moved)
                {
                    if (_ignoreFingersUntilPressed.Contains(location.Id))
                    {
                        continue;
                    }
                }
               
                FingerTouchSequence current = null;

                if (touchLocation.State == TouchLocationState.Released)
                {
                    if (CurrentSequences.ContainsKey(touchLocation.Id))
                    {
                        var finger = touchLocation.Id;
                        if (CurrentSequences.ContainsKey(finger))
                        {
                            var done = CurrentSequences[finger];
                            FinishSequence(done);
                        }
                    }

                    return;
                }

                _currentPosition = _resolutionIndependentRenderer.ScaleMouseToScreenCoordinates(location.Position);

                if ((Math.Abs(_currentPosition.X - _lastPosition.X) < DELTA) &&
                    (Math.Abs(_currentPosition.Y - _lastPosition.Y) < DELTA))
                {
                    continue;
                }

                _lastPosition = _currentPosition;

                var touchableMarbles = _world.EntityManager.GetLiveEntities(_touchableMarblesAspect);

                var touched = touchableMarbles.Where(
                    e =>
                        {
                            var t = e.GetComponent<TouchableComponent>();
                            return t.IsTouchable && t.ContainsPoint(_currentPosition);
                        }).ToList();

                if (!touched.Any())
                {
                    return;
                }

                if (touchLocation.State == TouchLocationState.Pressed || touchLocation.State == TouchLocationState.Moved)
                {
                    if (touchLocation.State == TouchLocationState.Pressed && _ignoreFingersUntilPressed.Contains(touchLocation.Id))
                    {
                        _ignoreFingersUntilPressed.Remove(touchLocation.Id);
                    }

                    if (CurrentSequences.ContainsKey(touchLocation.Id) && CurrentSequences[touchLocation.Id].Stage != TouchSequenceStage.Ended)
                    {
                        current = CurrentSequences[touchLocation.Id];
                    }
                    else
                    {
                        current = new FingerTouchSequence()
                        {
                            Stage = TouchSequenceStage.Started,
                            FingerId = touchLocation.Id
                        };

                        CurrentSequences.Add(current.FingerId, current);
                    }
                }

                if (current != null)
                {
                    foreach (var gameEntity in touched)
                    {
                        var currentMarbleComponent = gameEntity.GetComponent<MarbleComponent>();
                        var currentBoardCellChild = gameEntity.GetComponent<BoardCellChildEntityComponent>();
                        var currentTouchableComponent = gameEntity.GetComponent<TouchableComponent>();

                        current.LastTouchedEntityRegardlessOfSequence = gameEntity;

                        if (!current.EntitiesTouchedSoFar.Any())
                        {
                            current.FirstTouchedEntityInSequence = gameEntity;
                            current.LastTouchedEntityInSequence = gameEntity;
                            current.LastTouchedEntityRegardlessOfSequence = gameEntity;
                            current.Color = currentMarbleComponent.Color;
                            current.EntitiesTouchedSoFar.Add(gameEntity);
                            if (!currentTouchableComponent.IsTouched)
                                currentTouchableComponent.LastTouchDateTime = DateTime.Now;

                            currentTouchableComponent.IsTouched = true;

                            OnTouchSequenceGrown(gameEntity, current.EntitiesTouchedSoFar.Count);
                            continue;
                        }

                        if (current.LastTouchedEntityInSequence.Equals(gameEntity))
                        {
                            continue;
                        }

                        var prevBoardCellChild = current.LastTouchedEntityInSequence.GetComponent<BoardCellChildEntityComponent>();
                        if (!prevBoardCellChild.Cell.IsNighbouringCell(currentBoardCellChild.Cell))
                        {
                            currentTouchableComponent.IsTouched = false;
                            current.LastTouchedEntityRegardlessOfSequence = current.EntitiesTouchedSoFar.Last();
                            FinishSequence(current);
                            continue;
                        }

                        if (current.EntitiesTouchedSoFar.Contains(gameEntity) || current.FirstTouchedEntityInSequence.Equals(gameEntity))
                        {
                            currentTouchableComponent.IsTouched = false;
                            FinishSequence(current);
                            return;
                        }



                        current.LastTouchedEntityInSequence = gameEntity;

                        if (!currentTouchableComponent.IsTouched)
                            currentTouchableComponent.LastTouchDateTime = DateTime.Now;

                        currentTouchableComponent.IsTouched = true;

                        if (current.Color != currentMarbleComponent.Color)
                        {
                            currentTouchableComponent.IsTouched = false;
                            FinishSequence(current);
                            return;
                        }

                        current.EntitiesTouchedSoFar.Add(gameEntity);
                        OnTouchSequenceGrown(gameEntity, current.EntitiesTouchedSoFar.Count);
                    }
                }
            }
        }

        private void FinishSequence(FingerTouchSequence fingerTouchSequence)
        {
            if (fingerTouchSequence.Stage != TouchSequenceStage.Ended)
            {
                fingerTouchSequence.Stage = TouchSequenceStage.Ended;
                CurrentSequences.Remove(fingerTouchSequence.FingerId);
                AddFingerToIgnoreUntilPressedAgain(fingerTouchSequence.FingerId);
                OnTouchSequenceEnded(fingerTouchSequence);
            }
        }

        private void AddFingerToIgnoreUntilPressedAgain(int fingerId)
        {
            if (!_ignoreFingersUntilPressed.Contains(fingerId))
            {
                _ignoreFingersUntilPressed.Add(fingerId);
            }
        }

        private void OnTouchSequenceGrown(GameEntity gameEntity, int count)
        {
            var h = TouchSequenceGrown;
            if (h != null)
            {
                TouchSequenceGrown(this, new FingerTouchSequenceGrownArgs(){ 
                    Entity = gameEntity, 
                    NumberOfItemsTouchedSoFar = count
                });
            }
        }

        public event EventHandler<FingerTouchSequenceEndedArgs> TouchSequenceEnded;
        public event EventHandler<FingerTouchSequenceGrownArgs> TouchSequenceGrown;

        private void OnTouchSequenceEnded(FingerTouchSequence sequence)
        {
            var h = TouchSequenceEnded;
            if (h != null)
            {
                var args = new FingerTouchSequenceEndedArgs()
                               {
                                   Color = sequence.Color,
                                   EntitiesTouchedInSequenceSoFar = sequence.EntitiesTouchedSoFar,
                                   NumberOfItemsInSequence = sequence.EntitiesTouchedSoFar.Count,
                                   CellsTouchedInSequenceSoFar = sequence.EntitiesTouchedSoFar.Select(s => s.GetComponent<BoardCellChildEntityComponent>().Cell).ToList(),
                                   FirstTouchedEntityInSequence = sequence.FirstTouchedEntityInSequence,
                                   LastTouchedEntityInSequence = sequence.LastTouchedEntityInSequence,
                                   LastEntityThatWasTouchedRegardlessIfItWasInSequence = sequence.LastTouchedEntityRegardlessOfSequence
                               };
                h(this, args);
            }
        }

        public class FingerTouchSequence
        {
            public TouchSequenceStage Stage;

            public MarbleColor Color;

            public List<GameEntity> EntitiesTouchedSoFar = new List<GameEntity>();

            public int FingerId;

            public GameEntity FirstTouchedEntityInSequence;
            
            public GameEntity LastTouchedEntityInSequence;

            public GameEntity LastTouchedEntityRegardlessOfSequence;
        }
    }

    public class FingerTouchSequenceGrownArgs
    {
        public GameEntity Entity { get; set; }
        public int NumberOfItemsTouchedSoFar { get; set; }
    }

    public class FingerTouchSequenceEndedArgs
    {
        public MarbleColor Color;

        public List<GameEntity> EntitiesTouchedInSequenceSoFar = new List<GameEntity>();

        public List<BoardCell<GameEntity>> CellsTouchedInSequenceSoFar = new List<BoardCell<GameEntity>>();

        public int NumberOfItemsInSequence { get; set; }

        public GameEntity FirstTouchedEntityInSequence;
        public GameEntity LastTouchedEntityInSequence;
        public GameEntity LastEntityThatWasTouchedRegardlessIfItWasInSequence;
    }


    public enum TouchSequenceStage
        {
            Ready,
            Started,
            Ended
        }



    public interface ITouchSequenceSystem : IWorldUpdatingSystem
    {
        event EventHandler<FingerTouchSequenceEndedArgs> TouchSequenceEnded;
        event EventHandler<FingerTouchSequenceGrownArgs> TouchSequenceGrown;
    }
}