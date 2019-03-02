using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Marbles.Core.BoardRandomizers;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Components.SpecialMarbles;
using Marbles.Core.Model.Levels;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;
using System.Linq;

namespace Marbles.Core.Systems
{
    public class BoardRandomizationSystem : IWorldUpdatingSystem
    {
        private World _world;
        private IBoardRandomizer _cellRanomizer;
        private SpecialMarblesClearingAndAddingTrackerSystem _specialMarblesClearingAndAddingTrackerSystem;
        private MarbleBoard _board;
        private LevelDefinition _level;
        private List<BoardCell<GameEntity>> _cellsToRandomizeOnNextUpdate = new List<BoardCell<GameEntity>>();
        private List<BoardCell<GameEntity>> _cellsWitSpecialMarbleThatWeRequestedExplicitlyToRandomizeOnNextUpdate = new List<BoardCell<GameEntity>>();
        private List<int> _columnsToCleanup = new List<int>();
        private readonly object _cellRandomizationQueueLocker = new object();
        private List<BoardCell<GameEntity>> _currentProcessedItemsFromQueue  = new List<BoardCell<GameEntity>>();
        private readonly Dictionary<BoardCell<GameEntity>, SpecialMarbleRandomizationInstructions> _cellsThatShouldGetSpecialMarblesNextTime = new Dictionary<BoardCell<GameEntity>, SpecialMarbleRandomizationInstructions>();
        private Dictionary<BoardCell<GameEntity>, SpecialMarbleRandomizationInstructions> _currentCellsThatWeNeedToRandomizeWithSpecialMarbles = new Dictionary<BoardCell<GameEntity>, SpecialMarbleRandomizationInstructions>();
        private MarbleSpecialEffectsRenderingSystem _cellSpecialEffectRenderingSystem;
        private List<BoardCell<GameEntity>> _cellsThatShouldBecomeElectric = new List<BoardCell<GameEntity>>();
        private List<BoardCell<GameEntity>> _currentCellsToElectrify = new List<BoardCell<GameEntity>>();
        private List<GameEntity> _removedMarbles = new List<GameEntity>();
        private MarbleGameLevelControllerSystem _controller;
        private LevelScoringSystem _scoringSys;
        private List<BoardCell<GameEntity>> _cellsThatShouldBecomeBurning = new List<BoardCell<GameEntity>>();
        private readonly List<BoardCell<GameEntity>> _currentCellsToBurnify = new List<BoardCell<GameEntity>>();
        private string _movedMarbleNameFormat = "moved to cell: r:{0} c:{1}";
        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }
        public void Initialize(World world)
        {
            _world = world;
        }

        public MarbleBoard Board
        {
            get { return _board; }
        }

        private void OnSpecialMarbleAddedToCell(BoardCell<GameEntity> obj)
        {
            _specialMarblesClearingAndAddingTrackerSystem.SpecialMarbleWasAddedToCell(obj);
        }

        public void RandomizeBoard()
        {
            _cellRanomizer.Randomize(_board, _level);
            OnCellsRandomized(_board.AllCells);
        }

        public void RandomizeCell(BoardCell<GameEntity> cell, bool isInitialRandomization)
        {
            _cellRanomizer.RandomizeCell(cell, _level, isInitialRandomization);
            OnCellRandomized(cell);
        }
        public void Start()
        {
            _specialMarblesClearingAndAddingTrackerSystem =
                _world.GetSystem<SpecialMarblesClearingAndAddingTrackerSystem>();

            _cellSpecialEffectRenderingSystem = _world.GetSystem<MarbleSpecialEffectsRenderingSystem>();

            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();

            _scoringSys = _world.GetSystem<LevelScoringSystem>();

            if (_controller != null)
            {
                _controller.LevelStopped += OnLevelStopped;
            }
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
            _cellsToRandomizeOnNextUpdate.Clear();
            if (_cellRanomizer != null)
                _cellRanomizer.Reset();
        }

        public event EventHandler<List<BoardCell<GameEntity>>> CellsRandomized;

        public event EventHandler<List<GameEntity>> MarblesRemoved;

        private void OnCellRandomized(BoardCell<GameEntity> cell)
        {
            var ha = CellsRandomized;
            if (ha != null)
            {
                ha(this, new List<BoardCell<GameEntity>>() {cell});
            }
        }

        private void OnCellsRandomized(List<BoardCell<GameEntity>> cells)
        {
            var ha = CellsRandomized;
            if (ha != null)
            {
                ha(this, cells);
            }
        }

        public void PrepareForNewLevel(MarbleBoard board, LevelDefinition levelDefinition)
        {
            if (levelDefinition.LevelType == LevelType.Arcade)
            {
                _cellRanomizer = new ArcadeModeBoardRandomizer(_world, levelDefinition, _specialMarblesClearingAndAddingTrackerSystem, OnSpecialMarbleAddedToCell);
            }
            else
            {
                _cellRanomizer = new TimeSurvivalModeBoardRandomizer(_world, levelDefinition, _specialMarblesClearingAndAddingTrackerSystem, OnSpecialMarbleAddedToCell);
            }

            _board = board;
            _level = levelDefinition;
        }

        public void AddCellsToRandomizationQueue(IEnumerable<BoardCell<GameEntity>> cellsToRandomize)
        {
            lock (_cellRandomizationQueueLocker)
            {
                _cellsToRandomizeOnNextUpdate.AddRange(cellsToRandomize);
            }
        }

        public void Update(GameTime gameTime)
        {
            lock (_cellRandomizationQueueLocker)
            {
                _currentProcessedItemsFromQueue.AddRange(_cellsToRandomizeOnNextUpdate.Distinct().Where(c => !c.Item.HasComponent<SpecialMarbleComponent>()));
                _currentProcessedItemsFromQueue.AddRange(_cellsWitSpecialMarbleThatWeRequestedExplicitlyToRandomizeOnNextUpdate);
                _cellsToRandomizeOnNextUpdate.Clear();
                _cellsWitSpecialMarbleThatWeRequestedExplicitlyToRandomizeOnNextUpdate.Clear();

                _currentCellsToElectrify.AddRange(_cellsThatShouldBecomeElectric);
                _cellsThatShouldBecomeElectric.Clear();

                _currentCellsToBurnify.AddRange(_cellsThatShouldBecomeBurning);
                _cellsThatShouldBecomeBurning.Clear();

                _currentCellsThatWeNeedToRandomizeWithSpecialMarbles = new Dictionary<BoardCell<GameEntity>, SpecialMarbleRandomizationInstructions>(_cellsThatShouldGetSpecialMarblesNextTime);
                _cellsThatShouldGetSpecialMarblesNextTime.Clear();
            }

            if (_currentProcessedItemsFromQueue.Any() || _currentCellsThatWeNeedToRandomizeWithSpecialMarbles.Any())
            {
                _removedMarbles.Clear();
                _columnsToCleanup.Clear();

                Debug.WriteLine("Randomizing {0} items", _currentProcessedItemsFromQueue.Count);

                foreach (var cell in _currentProcessedItemsFromQueue)
                {
                    _cellSpecialEffectRenderingSystem.AddCellExplosion(cell);

                    if (!_columnsToCleanup.Contains(cell.Column))
                    {
                        _columnsToCleanup.Add(cell.Column);
                    }

                    if (cell.Item != null)
                    {
                        var removedItem = cell.Item;
                        _world.RemoveEntity(cell.Item);
                        cell.Item = null;
                        _removedMarbles.Add(removedItem);
                    }
                }

                _currentProcessedItemsFromQueue.Clear();

                var cellsThatNeedNewItems = new List<BoardCell<GameEntity>>();
                foreach (var column in _columnsToCleanup)
                {
                    MoveMerblesDown(column);
                }

                foreach (var column in _columnsToCleanup)
                {
                    var cellsToRandomize = new List<BoardCell<GameEntity>>();
                    for (int rowToFill = 0; rowToFill < _board.RowsCount; rowToFill++)
                    {
                        if (_board.Cells[rowToFill, column].Item == null)
                            cellsToRandomize.Add(_board.Cells[rowToFill, column]);
                    }

                    cellsThatNeedNewItems.AddRange(cellsToRandomize);
                }

                RandomizeCells(cellsThatNeedNewItems, _currentCellsThatWeNeedToRandomizeWithSpecialMarbles, false);              

                _columnsToCleanup.Clear();

                if (_removedMarbles.Any())
                {
                    OnMarblesRemoved(_removedMarbles);
                    _removedMarbles.Clear();
                }
            }

            if (_currentCellsToElectrify.Any())
            {
                foreach (var boardCell in _currentCellsToElectrify)
                {
                    var weCanMakeItElectric = boardCell.Item != null &&
                                             !boardCell.Item.HasComponent<ElectricMarbleComponent>() &&
                                             !boardCell.Item.HasComponent<BurningMarbleComponent>() &&
                                             !boardCell.Item.HasComponent<ShouldGetNewSpecialMarbleComponent>() &&
                                             !boardCell.Item.HasComponent<SpecialMarbleComponent>();
                    if (weCanMakeItElectric)
                    {
                        boardCell.Item.AddComponent(new ShouldGetNewSpecialMarbleComponent() {EffectType = PostEffectType.Electric});
                    }
                }
            }

            _currentCellsToElectrify.Clear();


            if (_currentCellsToBurnify.Any())
            {
                foreach (var boardCell in _currentCellsToBurnify)
                {
                    var weCanMakeItBurning = boardCell.Item != null &&
                                             !boardCell.Item.HasComponent<BurningMarbleComponent>() &&
                                             !boardCell.Item.HasComponent<ElectricMarbleComponent>() &&
                                              !boardCell.Item.HasComponent<ShouldGetNewSpecialMarbleComponent>() &&
                                             !boardCell.Item.HasComponent<SpecialMarbleComponent>();
                    if (weCanMakeItBurning)
                    {

                        boardCell.Item.AddComponent(new ShouldGetNewSpecialMarbleComponent() { EffectType = PostEffectType.Burn});
                    }
                }
            }

            _currentCellsToBurnify.Clear();
        }

        private void OnMarblesRemoved(List<GameEntity> removedMarbles)
        {
            var h = MarblesRemoved;
            if (h != null)
            {
                h(this, removedMarbles);
            }
        }

        private void RandomizeCells(List<BoardCell<GameEntity>> cellsThatNeedNewItem, Dictionary<BoardCell<GameEntity>, SpecialMarbleRandomizationInstructions> currentSpecialMarblesCells, bool isInitialRandomization)
        {
            if (currentSpecialMarblesCells.Any())
            {
                cellsThatNeedNewItem.RemoveAll(c => currentSpecialMarblesCells.ContainsKey(c));
                foreach (var currentSpecialMarblesCellKeyValuePair in currentSpecialMarblesCells)
                {
                    _cellSpecialEffectRenderingSystem.AddCellExplosion(currentSpecialMarblesCellKeyValuePair.Key);
                    _cellRanomizer.RandomizeCellWithSpecialMarble(currentSpecialMarblesCellKeyValuePair.Key, _level,
                                                                  currentSpecialMarblesCellKeyValuePair.Value);
                    cellsThatNeedNewItem.Remove(currentSpecialMarblesCellKeyValuePair.Key);
                }
            }

            foreach (var cell in cellsThatNeedNewItem)
            {
                _cellRanomizer.RandomizeCell(cell, _level, isInitialRandomization);
            }
            OnCellsRandomized(cellsThatNeedNewItem);
        }

        private void MoveMerblesDown(int column)
        {
            var emptyCells = new List<BoardCell<GameEntity>>();
            int row;

            bool seriesStarted = false;
            int seriesCount = 0;
            MarbleColor seriesColor = MarbleColor.Gray;
            BoardCell<GameEntity> cellUnderDestinationCellForSeries = null;
            BoardCell<GameEntity> cellThatShouldBeMadeElectric = null;
            var weHaveAElectricMarble = false;

            for (row = _board.RowsCount-1; row >= 0; row--)
            {
                weHaveAElectricMarble = false;

                var cell = _board.Cells[row, column];
                if (cell.Item == null)
                {
                    emptyCells.Add(cell);
                    seriesStarted = false;
                    seriesCount = 0;
                    continue;
                }

                if (emptyCells.Any())
                {
                    var currentMarbleData = cell.Item.GetComponent<MarbleComponent>();

                    if (!seriesStarted)
                    {
                        seriesStarted = true;
                        seriesColor = currentMarbleData.Color;
                        seriesCount = 1;
                        cellThatShouldBeMadeElectric = emptyCells[0];
                        cellUnderDestinationCellForSeries = emptyCells[0].HasNeighbourCell(NeighbourSide.Down)
                                                                ? emptyCells[0].Neighbours[NeighbourSide.Down]
                                                                : null;
                    }
                    else
                    {
                        if (currentMarbleData.Color != seriesColor)
                        {
                            seriesStarted = false;
                        }
                        else
                        {
                            seriesCount++;
                        }
                    }

                    if (seriesCount >= 3 && cellUnderDestinationCellForSeries != null && cellUnderDestinationCellForSeries.Item != null)
                    {
                        var cellUnderColor =
                            cellUnderDestinationCellForSeries.Item.GetComponent<MarbleComponent>().Color;

                        if (seriesColor == cellUnderColor)
                        {
                            weHaveAElectricMarble = true;
                            seriesStarted = false;
                            seriesCount = 0;
                        }
                    }


                    var destCell = emptyCells[0];
                    emptyCells.RemoveAt(0);
                    destCell.Item = cell.Item;
                    cell.Item = null;

                    if (destCell.Item.HasComponent<TouchableComponent>())
                        destCell.Item.GetComponent<TouchableComponent>().IsTouchable = false;

                    destCell.Item.GetComponent<BoardCellChildEntityComponent>().Cell = destCell;
                    destCell.Item.Name = string.Format(_movedMarbleNameFormat, destCell.Row, destCell.Column);

                    destCell.Item.AddComponent(new NeedsToBeBouncedDownComponent());

                    if (weHaveAElectricMarble)
                    {                       
                        MarbleInCellShouldBecomeElectricMarble(cellThatShouldBeMadeElectric);
                    }
                    
                    emptyCells.Add(cell);
                }


            }
        }

        public void MarbleInCellShouldBecomeElectricMarble(BoardCell<GameEntity> cell)
        {
            lock (_cellRandomizationQueueLocker)
            {
                if (cell.Item.HasComponent<ElectricMarbleComponent>() ||
                    cell.Item.HasComponent<NewSpecialMarblePostEffectComponent>())
                    return;

                _cellsThatShouldBecomeElectric.Add(cell);
            }
        }


        public void MarbleInCellShouldBecomeBurningMarble(BoardCell<GameEntity> cell)
        {
            lock (_cellRandomizationQueueLocker)
            {
                _cellsThatShouldBecomeBurning.Add(cell);
            }
        }

        public void CellShouldNextTimeGetSpecialMarble(BoardCell<GameEntity> cell, SpecialMarbleRandomizationInstructions instructions)
        {
            lock (_cellRandomizationQueueLocker)
            {
                _cellsThatShouldGetSpecialMarblesNextTime.Add(cell, instructions);
            }
        }

        public void AddCellsWithSpecialMarblesToRandomizationQueue(List<BoardCell<GameEntity>> cellsToReplace)
        {
            lock (_cellRandomizationQueueLocker)
            {
                _cellsWitSpecialMarbleThatWeRequestedExplicitlyToRandomizeOnNextUpdate.AddRange(cellsToReplace);
            }
        }
    }
}