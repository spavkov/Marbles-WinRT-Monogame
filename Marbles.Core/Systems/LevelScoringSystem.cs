using System;
using System.Collections.Generic;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Levels;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;
using System.Linq;

namespace Marbles.Core.Systems
{
    public class LevelScoringSystem : IWorldSystem
    {
        private World _world;
        private ITouchSequenceSystem _touchSequenceSys;
        private MarbleGameLevelControllerSystem _controller;
        private bool _weAreInitialized;
        private CurrentGameInformationTrackingSystem _gameInformation;
        private MarbleSpecialEffectsRenderingSystem _specialEffectsSys;
        private Random _random = new Random();

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }
        
        public void Initialize(World world)
        {
            _world = world;
            BrokenMarbleInSequenceBonus = 1;
        }

        public void Start()
        {
            _touchSequenceSys = _world.GetSystem<ITouchSequenceSystem>();
            if (_touchSequenceSys != null)
            {
                _touchSequenceSys.TouchSequenceEnded += OnTouchSequenceEnded;
            }

            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();
            if (_controller != null)
            {
                _controller.LevelCompleted += OnLevelCompleted;
            }

            _gameInformation = _world.GetSystem<CurrentGameInformationTrackingSystem>();

            _specialEffectsSys = _world.GetSystem<MarbleSpecialEffectsRenderingSystem>();

            _weAreInitialized = _controller != null && _touchSequenceSys != null && _gameInformation != null && _specialEffectsSys != null;
        }

        private void OnLevelCompleted(object sender, LevelCompletionType e)
        {
            if (e == LevelCompletionType.Success)
            {
                _gameInformation.CurrentLevelScore += (int)_gameInformation.LevelRemainingTimeInSeconds * 10;
                _gameInformation.TotalScore = _gameInformation.CurrentLevelScore;
            }
        }

        private void OnTouchSequenceEnded(object sender, FingerTouchSequenceEndedArgs e)
        {
            if (!_weAreInitialized)
            {
                return;
            }

            var lastTouchedCellCenter = e.CellsTouchedInSequenceSoFar.Last();

            HandleClearedCells(e.CellsTouchedInSequenceSoFar, lastTouchedCellCenter);           
        }

        private void HandleClearedCells(List<BoardCell<GameEntity>> cells, BoardCell<GameEntity> originCell )
        {
            if (cells.Count <= 1)
            {
                return;
            }

            var toAddforIndividualMarbles = (int)(cells.Count * BrokenMarbleInSequenceBonus * _gameInformation.CurrentMultiplier);
            _gameInformation.CurrentLevelScore += toAddforIndividualMarbles;

            var changes = new List<ScoreChangeData>();

            if (cells.Count >= _gameInformation.LevelDefinition.NumberOfMarblesInSequenceToIncreaseBonusMultiplier)
            {
                if (_gameInformation.CurrentMultiplier < _gameInformation.LevelDefinition.MaximumMultiplier)
                    _gameInformation.CurrentMultiplier += _gameInformation.LevelDefinition.MultiplierIncrease;
            }

            changes.Add(new ScoreChangeData()
                                        {
                                            ChangeAmount = toAddforIndividualMarbles,
                                            ChangeType = ScoreChangeType.ScoreIncrease,
                                            ChangeScreenOrigin = CreateScoreChangeIndicatorRandomOffset(originCell.Center)
                                        });

            if (cells.Count >= _gameInformation.LevelDefinition.NumberOfMarblesInSequenceForCombo)
            {
                changes.Add(new ScoreChangeData()
                                {
                                    ChangeType = ScoreChangeType.Combo,
                                    ChangeScreenOrigin = CreateScoreChangeIndicatorRandomOffset(cells.Last().Center),
                                    TotalMarblesClearedCount = cells.Count,
                                });
            }

            OnGameScoreChanged(changes);
        }

        private Vector2 CreateScoreChangeIndicatorRandomOffset(Vector2 center)
        {
            return new Vector2(center.X + _random.Next(-60,60), center.Y + _random.Next(-40,40));
        }

        protected int BrokenMarbleInSequenceBonus { get; set; }

        public void Stop()
        {
            if (_touchSequenceSys != null)
            {
                _touchSequenceSys.TouchSequenceEnded -= OnTouchSequenceEnded;
            }

            if (_controller != null)
            {
                _controller.LevelCompleted -= OnLevelCompleted;
            }
        }

        public void NotifyScoringSystemThatUserClearedPowerup(SpecialMarbleType eventType, BoardCell<GameEntity> cell, GameEntity originalSpecialItem)
        {
            DoSpecialMarbleClearedConsequences(eventType, cell, originalSpecialItem);
        }

        private void DoSpecialMarbleClearedConsequences(SpecialMarbleType eventType, BoardCell<GameEntity> cell, GameEntity originalSpecialItem)
        {
            _gameInformation.CurrentLevelScore += 100;

            OnGameScoreChanged(new List<ScoreChangeData>()
                                   {
                                      new ScoreChangeData()
                                          {
                                              ChangeAmount = 100,
                                              ChangeType = ScoreChangeType.ScoreIncrease,
                                              ChangeScreenOrigin = CreateScoreChangeIndicatorRandomOffset(cell.Center)
                                          }
                                   });
        }

        public event EventHandler<CumulativeGameScoreChangeEvent> GameScoreChanged;

        private void OnGameScoreChanged(IEnumerable<ScoreChangeData> changeDatas)
        {
            var h = GameScoreChanged;
            if (h != null)
            {
                var args = new CumulativeGameScoreChangeEvent()
                               {
                                   ScoreChanges = changeDatas
                               };
                h(this, args);
            }
        }

        public void NotifyScoringSystemThatUserClearedCellsAsSpecialMarbleClearingConsequence(List<BoardCell<GameEntity>> cells, BoardCell<GameEntity> originCell)
        {
            HandleClearedCells(cells, originCell);
        }

        public void NotifyScroingSystemThatSpecialMarbleHasBeenWasted(BoardCell<GameEntity> cell)
        {
            _gameInformation.CurrentLevelScore -= 100;

            OnGameScoreChanged(new List<ScoreChangeData>()
                                   {
                                      new ScoreChangeData()
                                          {
                                              ChangeAmount = 100,
                                              ChangeType = ScoreChangeType.ScoreDecrease,
                                              ChangeScreenOrigin = CreateScoreChangeIndicatorRandomOffset(cell.Center)
                                          }
                                   });
        }

        public void NotifyScoringSystemThatUserClearedASquare(FingerTouchSequenceEndedArgs touchSequence, List<BoardCell<GameEntity>> cellsCleared)
        {
            var toIncrease = cellsCleared.Count;

            _gameInformation.CurrentLevelScore += (int) (toIncrease * _gameInformation.CurrentMultiplier);

            OnGameScoreChanged(new List<ScoreChangeData>()
                                   {
                                      new ScoreChangeData()
                                          {
                                              ChangeAmount = toIncrease,
                                              ChangeType = ScoreChangeType.ScoreIncrease,
                                              ChangeScreenOrigin = CreateScoreChangeIndicatorRandomOffset(touchSequence.LastTouchedEntityInSequence.GetComponent<MarbleScreenDataComponent>().Position)
                                          }
                                   });
        }

        public void NotifyScoringSystemThatUserClearedATriangle(FingerTouchSequenceEndedArgs touchSequence)
        {
            var toIncrease = touchSequence.NumberOfItemsInSequence;

            _gameInformation.CurrentLevelScore += (int)(toIncrease * _gameInformation.CurrentMultiplier);

            OnGameScoreChanged(new List<ScoreChangeData>()
                                   {
                                      new ScoreChangeData()
                                          {
                                              ChangeAmount = toIncrease,
                                              ChangeType = ScoreChangeType.TimeIncrease,
                                              ChangeScreenOrigin = CreateScoreChangeIndicatorRandomOffset(touchSequence.LastTouchedEntityInSequence.GetComponent<MarbleScreenDataComponent>().Position)
                                          }
                                   });
        }

        public void NotifyScoringSystemThatUserClearedARectangle(FingerTouchSequenceEndedArgs fingerTouchSequenceEndedArgs, List<BoardCell<GameEntity>> cellsToClread)
        {
            var toIncrease = cellsToClread.Count;

            _gameInformation.CurrentLevelScore += (int)(toIncrease * _gameInformation.CurrentMultiplier);

            OnGameScoreChanged(new List<ScoreChangeData>()
                                   {
                                      new ScoreChangeData()
                                          {
                                              ChangeAmount = toIncrease,
                                              ChangeType = ScoreChangeType.ScoreIncrease,
                                              ChangeScreenOrigin = CreateScoreChangeIndicatorRandomOffset(fingerTouchSequenceEndedArgs.LastTouchedEntityInSequence.GetComponent<MarbleScreenDataComponent>().Position)
                                          }
                                   });
        }
    }

    public class CumulativeGameScoreChangeEvent
    {
        public IEnumerable<ScoreChangeData> ScoreChanges = new List<ScoreChangeData>();
    }

    public class ScoreChangeData
    {
        public ScoreChangeType ChangeType;

        public int ChangeAmount;

        public Vector2 ChangeScreenOrigin;

        public int TotalMarblesClearedCount;
    }

    public enum ScoreChangeType
    {
        ScoreIncrease,
        ScoreDecrease,
        TimeIncrease,
        Combo,
    }
}