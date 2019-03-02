using System;
using System.Collections.Generic;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Components.SpecialMarbles;
using Marbles.Core.Model.Levels;
using Marbles.Core.Model.Levels.LevelDefinitionComponents;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;
using System.Linq;

namespace Marbles.Core.Systems
{
    public class SpecialMarblesClearingPostEffectsSystem : IWorldUpdatingSystem
    {
        private World _world;
        private BoardRandomizationSystem _boardRandomizationSys;
        private LevelScoringSystem _levelScoringSys;
        private CurrentGameInformationTrackingSystem _gameInformation;
        private MarbleSpecialEffectsRenderingSystem _specialEffectsSys;
        private object _locker = new object();
        private List<BoardCell<GameEntity>> _cellsToReplace = new List<BoardCell<GameEntity>>();
        private List<BoardCell<GameEntity>> _specialMarbleCellsToReplace = new List<BoardCell<GameEntity>>();
        private MarbleGameLevelControllerSystem _controller;
        private SpecialMarbleType[] _surpriseSpecialMarbleChoicesForArcadeMode;
        private Random _rnd = new Random();
        private LineClearerType[] _allLineClearers;
        private SpecialMarbleType[] _surpriseSpecialMarbleChoicesForSurvivalMode;
        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }

        public SpecialMarblesClearingPostEffectsSystem()
        {
            _surpriseSpecialMarbleChoicesForArcadeMode = Enum.GetValues(typeof(SpecialMarbleType)).Cast<SpecialMarbleType>().Except(new[] { SpecialMarbleType.None, SpecialMarbleType.SurpriseMarble }).ToArray();
            _surpriseSpecialMarbleChoicesForSurvivalMode = Enum.GetValues(typeof(SpecialMarbleType)).Cast<SpecialMarbleType>().Except(new[] { SpecialMarbleType.None, SpecialMarbleType.SurpriseMarble, SpecialMarbleType.TimeExtensionMarble,  }).ToArray();
            _allLineClearers = Enum.GetValues(typeof(LineClearerType)).Cast<LineClearerType>().ToArray();
        }


        public void Initialize(World world)
        {
            _world = world;
        }

        public void Start()
        {
            _boardRandomizationSys = _world.GetSystem<BoardRandomizationSystem>();
            _levelScoringSys = _world.GetSystem<LevelScoringSystem>();
            _gameInformation = _world.GetSystem<CurrentGameInformationTrackingSystem>();

            _specialEffectsSys = _world.GetSystem<MarbleSpecialEffectsRenderingSystem>();
            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();

            _controller.LevelStopped += OnLevelStopped;
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
            _cellsToReplace.Clear();
            _specialMarbleCellsToReplace.Clear();
        }

        public void Update(GameTime gameTime)
        {
            lock (_locker)
            {
                if (_cellsToReplace.Any())
                {
                    _boardRandomizationSys.AddCellsToRandomizationQueue(_cellsToReplace);
                    _cellsToReplace.Clear();
                }

                if (_specialMarbleCellsToReplace.Any())
                {
                    _boardRandomizationSys.AddCellsWithSpecialMarblesToRandomizationQueue(_specialMarbleCellsToReplace);
                    _specialMarbleCellsToReplace.Clear();
                }
            }
        }

        private void AddCellsForClearing(IEnumerable<BoardCell<GameEntity>> cells)
        {
            lock (_locker)
            {
                _cellsToReplace.AddRange(cells);
            }
        }

        public void SpecialMarblePotentialHasBeenFulfilled(SpecialMarbleComponent specialMarbleComponent, GameEntity entity)
        {
            var cell = entity.GetComponent<BoardCellChildEntityComponent>().Cell;
            var originalSpecialItem = cell.Item;
            DoSpecialMarbleClearedConsequences(specialMarbleComponent, cell, originalSpecialItem);
        }

        private void DoSpecialMarbleClearedConsequences(SpecialMarbleComponent specialMarbleComponent, BoardCell<GameEntity> cellThatHoldsEntityWithSpecialItem, GameEntity entityThatHoldsSpecialItem)
        {
            _levelScoringSys.NotifyScoringSystemThatUserClearedPowerup(specialMarbleComponent.SpecialMarbleType, cellThatHoldsEntityWithSpecialItem, entityThatHoldsSpecialItem);

            if (specialMarbleComponent.SpecialMarbleType != SpecialMarbleType.SurpriseMarble)
                AddCellWithSpecialMarbleForClearing(cellThatHoldsEntityWithSpecialItem);

            if (specialMarbleComponent.SpecialMarbleType == SpecialMarbleType.TimeExtensionMarble && specialMarbleComponent.Details is TimeExtenderSpecialMarbleDetails)
            {
                var details = specialMarbleComponent.Details as TimeExtenderSpecialMarbleDetails;
                _gameInformation.LevelRemainingTimeInSeconds += details.TimeToAdd;
            }
            if (specialMarbleComponent.SpecialMarbleType == SpecialMarbleType.SurpriseMarble && specialMarbleComponent.Details is SurpriseSpecialMarbleDetails)
            {
                var toChoseFrom = _gameInformation.LevelDefinition.LevelType == LevelType.Arcade
                                      ? _surpriseSpecialMarbleChoicesForArcadeMode
                                      : _surpriseSpecialMarbleChoicesForSurvivalMode;

                var next = toChoseFrom[_rnd.Next(toChoseFrom.Count())];
                SpecialMarbleRandomizationSettingComponent randomizationSetting = null; 
                if (next == SpecialMarbleType.LineClearerMarble)
                {
                    var lineClearerType = _allLineClearers[_rnd.Next(_allLineClearers.Count())];
                    switch (lineClearerType)
                    {
                         case LineClearerType.VerticalClearer:
                            randomizationSetting = new VerticalLineClearerSpecialMarbleRandomizationSettingsComponent();
                            break;
                         case LineClearerType.HorizontalClearer:
                            randomizationSetting = new HorizontalLineClearerSpecialMarbleRandomizationSettingsComponent();
                            break;
                         case LineClearerType.HorizontalAndVerticalClearer:
                            randomizationSetting = new HorizontalAndVerticalLineClearerSpecialMarbleRandomizationSettingsComponent();
                            break;
                    }
                }
                else
                {
                    switch (next)
                    {
                            case SpecialMarbleType.GameOverMarble:
                            randomizationSetting = new GameOverSpecialMarbleRandomizationSettingsComponent()
                                                       {
                                                           TimeUntilEndInSeconds = _rnd.Next(10, 20)
                                                       };
                            break;
                            case SpecialMarbleType.ColorBombMarble:
                            randomizationSetting = new ColorBombSpecialMarbleRandomizationSettingsComponent()
                                                       {
                                                           
                                                       };
                            break;
                            case SpecialMarbleType.TimeExtensionMarble:
                            randomizationSetting = new TimeIncreaseSpecialMarbleRandomizationSettingsComponent()
                                                       {
                                                           SecondsToAdd = _rnd.Next(10,20)
                                                       };
                            break;
                    }
                }

                _boardRandomizationSys.CellShouldNextTimeGetSpecialMarble(cellThatHoldsEntityWithSpecialItem,
                                                                          new SpecialMarbleRandomizationInstructions()
                                                                              {
                                                                                  SpecialMarbleType = next,
                                                                                  RandomizationSettings =
                                                                                      randomizationSetting
                                                                              });
            }
            if (specialMarbleComponent.SpecialMarbleType == SpecialMarbleType.ColorBombMarble && specialMarbleComponent.Details is ColorBombSpecialMarbleDetails)
            {
                var details = specialMarbleComponent.Details as ColorBombSpecialMarbleDetails;
                var colorToClear = details.MarbleColorToClear;

                var cells = new List<BoardCell<GameEntity>>();
                var board = cellThatHoldsEntityWithSpecialItem.Board;
                for (int r = 0; r < board.RowsCount; r++)
                    for (int c = 0; c < board.ColumnsCount; c++)
                {
                    if (cellThatHoldsEntityWithSpecialItem.Row == r && cellThatHoldsEntityWithSpecialItem.Column == c)
                        continue;
                    var color = board.Cells[r, c].Item.GetComponent<MarbleComponent>().Color;
                    if (colorToClear != color)
                        continue;
                    cells.Add(cellThatHoldsEntityWithSpecialItem.Board.Cells[r, c]);
                }

                if (cells.Any())
                {
                    _levelScoringSys.NotifyScoringSystemThatUserClearedCellsAsSpecialMarbleClearingConsequence(cells, cellThatHoldsEntityWithSpecialItem);
                    AddCellsForClearing(cells);
                    cells.Clear();
                }
            }
            else if (specialMarbleComponent.SpecialMarbleType == SpecialMarbleType.LineClearerMarble && specialMarbleComponent.Details is LineClearerSpecialMarbleDetails)
            {
                var casted = specialMarbleComponent.Details as LineClearerSpecialMarbleDetails;

                var cells = new List<BoardCell<GameEntity>>();

                if (casted.ClearerType == LineClearerType.HorizontalClearer || casted.ClearerType == LineClearerType.HorizontalAndVerticalClearer)
                {
                    for (int i = 0; i < cellThatHoldsEntityWithSpecialItem.Board.ColumnsCount; i++)
                    {
                        if (cellThatHoldsEntityWithSpecialItem.Column == i)
                            continue;
                        cells.Add(cellThatHoldsEntityWithSpecialItem.Board.Cells[cellThatHoldsEntityWithSpecialItem.Row, i]);
                    }
                }
                if (casted.ClearerType == LineClearerType.VerticalClearer || casted.ClearerType == LineClearerType.HorizontalAndVerticalClearer)
                {
                    for (int i = 0; i < cellThatHoldsEntityWithSpecialItem.Board.RowsCount; i++)
                    {
                        if (cellThatHoldsEntityWithSpecialItem.Row == i)
                            continue;
                        cells.Add(cellThatHoldsEntityWithSpecialItem.Board.Cells[i, cellThatHoldsEntityWithSpecialItem.Column]);
                    }
                }

                if (cells.Any())
                {
                    _levelScoringSys.NotifyScoringSystemThatUserClearedCellsAsSpecialMarbleClearingConsequence(cells, cellThatHoldsEntityWithSpecialItem);
                    AddCellsForClearing(cells);
                    cells.Clear();
                }
            }
        }

        private void AddCellWithSpecialMarbleForClearing(BoardCell<GameEntity> cellThatHoldsEntityWithSpecialItem)
        {
            lock (_locker)
            {
                _specialMarbleCellsToReplace.Add(cellThatHoldsEntityWithSpecialItem);
            }
        }
    }
}