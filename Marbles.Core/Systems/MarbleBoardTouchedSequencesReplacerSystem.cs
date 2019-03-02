using System;
using System.Collections.Generic;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Components.SpecialMarbles;
using Marbles.Core.Model.Levels.LevelDefinitionComponents;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;
using System.Linq;

namespace Marbles.Core.Systems
{
    public class MarbleBoardTouchedSequencesReplacerSystem : IWorldUpdatingSystem
    {
        private World _world;
        private ITouchSequenceSystem _touchSquenceSys;
        private MarbleGameLevelControllerSystem _controller;
        private CurrentGameInformationTrackingSystem _gameInformation;
        //private List<BoardCell<GameEntity>> _cellsToRand = new List<BoardCell<GameEntity>>();
        private List<BoardCell<GameEntity>> _cellsToRandOnNextUpdateOfThisSystem = new List<BoardCell<GameEntity>>();
        private BoardRandomizationSystem _boardRandomizationSystem;
        private object _locker = new object();
        private CompletedTouchSequenceShapeDetector _shapeDetector;
        private LevelScoringSystem _scoreSys;
        private MarbleSoundsSystem _soundSys;
        private List<BoardCell<GameEntity>> EmptyListOfCells = new List<BoardCell<GameEntity>>();
        private MarbleSpecialEffectsRenderingSystem _specialEffectsSys;
        private readonly List<BoardCell<GameEntity>> _cellsToMakeBurningInitiators = new List<BoardCell<GameEntity>>();
        private readonly List<BoardCell<GameEntity>> _cellsClearedThatHadElectricInitiators = new List<BoardCell<GameEntity>>();
        private readonly List<BoardCell<GameEntity>> _cellsClearedThatHadBurningInitiators = new List<BoardCell<GameEntity>>();
        private Random _rnd = new Random();
        private readonly List<BoardCell<GameEntity>> _copyListOfCells = new List<BoardCell<GameEntity>>();
        private List<BoardCell<GameEntity>> _thisSequenceCellsToRand = new List<BoardCell<GameEntity>>();
        private SequenceVisualizationRenderingSystem _sequenceRenderer;

        public void Update(GameTime gameTime)
        {
            lock (_locker)
            {
                if (_cellsClearedThatHadElectricInitiators.Any())
                {
                    _copyListOfCells.Clear();
                    _copyListOfCells.AddRange(_cellsClearedThatHadElectricInitiators);
                    _cellsClearedThatHadElectricInitiators.Clear();
                    foreach (var cell in _copyListOfCells)
                    {
                        cell.Item.RemoveComponent<ElectricMarbleComponent>();

                        var color = cell.Item.GetComponent<MarbleComponent>().Color;
                        var remainingCellsOfSameColor = GetCellsWithColor(color, cell.Board, _cellsToRandOnNextUpdateOfThisSystem);
                        if (!remainingCellsOfSameColor.Any())
                        {
                            // we want to remove it anyhow
                            _cellsToRandOnNextUpdateOfThisSystem.Add(cell); 
                            continue;
                        }

                        _cellsToRandOnNextUpdateOfThisSystem.Remove(cell);                       

                        if (remainingCellsOfSameColor.Contains(cell))
                        {
                            remainingCellsOfSameColor.Remove(cell);
                        }

                        cell.Item.GetComponent<TouchableComponent>().IsTouchable = false;
                        cell.Item.AddComponent(new IsCurrentlyPartOfSpecialMarblePostEffect());
                        _cellsToRandOnNextUpdateOfThisSystem.Remove(cell);

                        foreach (var boardCell in remainingCellsOfSameColor)
                        {
                            boardCell.Item.AddComponent(new IsCurrentlyPartOfSpecialMarblePostEffect());
                            boardCell.Item.GetComponent<TouchableComponent>().IsTouchable = false;
                        }

                        var component = new NewSpecialMarblePostEffectComponent()
                        {
                            TargetCells = remainingCellsOfSameColor,
                            TargetEntities = remainingCellsOfSameColor.Select(c => c.Item).ToList(),
                            RemainingDurationinSeconds = 2f,
                            EffectType = PostEffectType.Electric
                        };
                        cell.Item.AddComponent(component);

                        _specialEffectsSys.StartMarbleElectricityCulmination(cell.Item, component.TargetEntities);
                    }
                }

                if (_cellsClearedThatHadBurningInitiators.Any())
                {
                    _copyListOfCells.Clear();
                    _copyListOfCells.AddRange(_cellsClearedThatHadBurningInitiators);
                    _cellsClearedThatHadBurningInitiators.Clear();
                    foreach (var cell in _copyListOfCells)
                    {
                        _cellsToRandOnNextUpdateOfThisSystem.Remove(cell);

                        cell.Item.RemoveComponent<BurningMarbleComponent>();
                        cell.Item.GetComponent<TouchableComponent>().IsTouchable = false;

                        var lineOrColumn = _rnd.Next(11);

                        var component = new NewSpecialMarblePostEffectComponent()
                        {
                            RemainingDurationinSeconds = 2f,
                            EffectType = PostEffectType.Burn
                        };

                        if (lineOrColumn <= 5)
                        {
                            // row
                            var row = cell.Row;
                            for (int column = 0; column < cell.Board.ColumnsCount; column++)
                            {
                                var cellToMark = cell.Board.Cells[row, column];
                                if (_cellsToRandOnNextUpdateOfThisSystem.Contains(cellToMark))
                                {
                                    _cellsToRandOnNextUpdateOfThisSystem.Remove(cellToMark);
                                }
                                if (IsInitiatorOrEffectOfNewSpecialMarblePostEffect(cellToMark.Item))
                                {
                                    continue;
                                }

                                cellToMark.Item.AddComponent(new IsCurrentlyPartOfSpecialMarblePostEffect());
                                cellToMark.Item.GetComponent<TouchableComponent>().IsTouchable = false;
                                component.TargetCells.Add(cellToMark);
                                component.TargetEntities.Add(cellToMark.Item);
                            }
                        }
                        else
                        {
                            // column
                            var column = cell.Column;
                            for (int row = 0; row < cell.Board.RowsCount; row++)
                            {
                                var cellToMark = cell.Board.Cells[row, column];
                                if (_cellsToRandOnNextUpdateOfThisSystem.Contains(cellToMark))
                                {
                                    _cellsToRandOnNextUpdateOfThisSystem.Remove(cellToMark);
                                }
                                if (IsInitiatorOrEffectOfNewSpecialMarblePostEffect(cellToMark.Item))
                                {
                                    continue;
                                }

                                cellToMark.Item.AddComponent(new IsCurrentlyPartOfSpecialMarblePostEffect());
                                cellToMark.Item.GetComponent<TouchableComponent>().IsTouchable = false;
                                component.TargetCells.Add(cellToMark);
                                component.TargetEntities.Add(cellToMark.Item);
                            }
                        }
                        cell.Item.AddComponent(component);
                        _specialEffectsSys.StartBurningMarbleCulmination(cell.Item, component.TargetCells.Select(t => t.Item).ToList());
                    }
                }

                foreach (var cell in _cellsToRandOnNextUpdateOfThisSystem)
                {
                    cell.Item.GetComponent<TouchableComponent>().IsTouched = false;
                }

                _boardRandomizationSystem.AddCellsToRandomizationQueue(_cellsToRandOnNextUpdateOfThisSystem);
                _cellsToRandOnNextUpdateOfThisSystem.Clear();

                foreach (var cell in _cellsToMakeBurningInitiators)
                {
                    _boardRandomizationSystem.MarbleInCellShouldBecomeBurningMarble(cell);
                }

                _cellsToMakeBurningInitiators.Clear();
            }
        }

        public int Priority { get { return GamePriorities.Systems.IndexOf(GetType()); } }

        public void Initialize(World world)
        {
            _world = world;
            _shapeDetector = new CompletedTouchSequenceShapeDetector();
        }

        public void Start()
        {
            _touchSquenceSys = _world.GetSystem<ITouchSequenceSystem>();
            if (_touchSquenceSys != null)
            {
                _touchSquenceSys.TouchSequenceEnded += OnTouchSequenceEnded;
            }
            _controller = _world.GetSystem<MarbleGameLevelControllerSystem>();

            if (_controller != null)
            {
                _controller.LevelStopped += OnLevelStopped;
            }

            _scoreSys = _world.GetSystem<LevelScoringSystem>();

            _boardRandomizationSystem = _world.GetSystem<BoardRandomizationSystem>();

            _gameInformation = _world.GetSystem<CurrentGameInformationTrackingSystem>();

            _soundSys = _world.GetSystem<MarbleSoundsSystem>();

            _specialEffectsSys = _world.GetSystem<MarbleSpecialEffectsRenderingSystem>();

            _sequenceRenderer = _world.GetSystem<SequenceVisualizationRenderingSystem>();
        }


        private void OnLevelStopped(object sender, EventArgs e)
        {
            lock (_locker)
            {
                _cellsToRandOnNextUpdateOfThisSystem.Clear();
            }

            lock (_locker)
            {
                _thisSequenceCellsToRand.Clear();
            }
        }

        public void Stop()
        {
            if (_touchSquenceSys != null)
                _touchSquenceSys.TouchSequenceEnded -= OnTouchSequenceEnded;

            if (_controller != null)
            {
                _controller.LevelStopped -= OnLevelStopped;
            }
        }

        private void OnTouchSequenceEnded(object sender, FingerTouchSequenceEndedArgs e)
        {
            lock (_locker)
            {
                var touched = e.CellsTouchedInSequenceSoFar.ToList();
                _thisSequenceCellsToRand.Clear();

                foreach (var gameEntity in e.EntitiesTouchedInSequenceSoFar)
                {
                    gameEntity.GetComponent<TouchableComponent>().IsTouched = false;
                }

                if (e.NumberOfItemsInSequence < 2)
                {
                    return;
                }

                if (e.NumberOfItemsInSequence >= _gameInformation.LevelDefinition.NumberOfMarblesInSequenceForCombo)
                {
                    _cellsToMakeBurningInitiators.Add(
                        touched[_gameInformation.LevelDefinition.NumberOfMarblesInSequenceForCombo - 1]);
                }

                foreach (var cell in touched)
                {
                    if (cell.Item.HasComponent<ElectricMarbleComponent>())
                    {
                        _cellsClearedThatHadElectricInitiators.Add(cell);
                    }
                    else if (cell.Item.HasComponent<BurningMarbleComponent>())
                    {
                        _cellsClearedThatHadBurningInitiators.Add(cell);
                    }
                    else
                    {
                        _thisSequenceCellsToRand.Add(cell);
                    }
                }

                var shapeRes = _shapeDetector.Detect(e);

                if (shapeRes.Type != TouchSequenceShapeType.None)
                {
                    if (shapeRes.Type == TouchSequenceShapeType.Rectangle)
                    {
                        // clears all 
                        var board = e.FirstTouchedEntityInSequence.GetComponent<BoardCellChildEntityComponent>().Cell.Board;
                        var cellsToClread = GetAllCels(board);

                        if (cellsToClread.Any())
                        {
                            _thisSequenceCellsToRand.AddRange(cellsToClread);                           
                        }
                    }
                    else if (shapeRes.Type == TouchSequenceShapeType.Square)
                    {
                        // clears rows and cols
                        var board = e.FirstTouchedEntityInSequence.GetComponent<BoardCellChildEntityComponent>().Cell.Board;
                        var rows = e.CellsTouchedInSequenceSoFar.Select(c => c.Row).ToList();
                        var cols = e.CellsTouchedInSequenceSoFar.Select(c => c.Column).ToList();
                        var cellsToClread = GetCellsWithRowsAndColumns(board, rows, cols);

                        if (cellsToClread.Any())
                        {
                            _thisSequenceCellsToRand.AddRange(cellsToClread);
                           
                        }
                    }
                    else if (shapeRes.Type == TouchSequenceShapeType.Triangle)
                    {
                        _gameInformation.LevelRemainingTimeInSeconds += e.NumberOfItemsInSequence;
                    }
                }

                _thisSequenceCellsToRand = _thisSequenceCellsToRand.Distinct().ToList();

                if (_thisSequenceCellsToRand.Any())
                    if (shapeRes.Type == TouchSequenceShapeType.Rectangle)
                    {
                        _scoreSys.NotifyScoringSystemThatUserClearedARectangle(e, _thisSequenceCellsToRand);
                        _sequenceRenderer.NotifySystemThatUserClearedTriangle(shapeRes.PointsThatFormedSequenceShape, shapeRes.Color);

                    }
                    else if (shapeRes.Type == TouchSequenceShapeType.Square)
                    {
                        _scoreSys.NotifyScoringSystemThatUserClearedASquare(e, _thisSequenceCellsToRand);
                        _sequenceRenderer.NotifySystemThatUserClearedTriangle(shapeRes.PointsThatFormedSequenceShape, shapeRes.Color);
                    }
                    else if (shapeRes.Type == TouchSequenceShapeType.Triangle)
                    {
                        _scoreSys.NotifyScoringSystemThatUserClearedATriangle(e);
                        _sequenceRenderer.NotifySystemThatUserClearedTriangle(shapeRes.PointsThatFormedSequenceShape, shapeRes.Color);
                    }
            }

            lock (_locker)
            {
                _cellsToRandOnNextUpdateOfThisSystem.AddRange(_thisSequenceCellsToRand);
            }
        }

        private bool IsInitiatorOrEffectOfNewSpecialMarblePostEffect(GameEntity item)
        {
            if (item == null)
            {
                return false;
            }
            return item.HasComponent<IsCurrentlyPartOfSpecialMarblePostEffect>()
                   || item.HasComponent<ShouldGetNewSpecialMarbleComponent>()
                   || item.HasComponent<NewSpecialMarblePostEffectComponent>()
                   || item.HasComponent<ElectricMarbleComponent>()
                   || item.HasComponent<BurningMarbleComponent>()
                   || item.HasComponent<SpecialMarbleComponent>();
        }

        private List<BoardCell<GameEntity>> GetAllCels(Board<GameEntity> board)
        {
            var cells = new List<BoardCell<GameEntity>>();

            foreach (var boardCell in board.AllCells)
            {
                if (!IsInitiatorOrEffectOfNewSpecialMarblePostEffect(boardCell.Item))
                {
                    cells.Add(boardCell);
                }
            }

            return cells;

        }

        private List<BoardCell<GameEntity>> GetCellsWithRowsAndColumns(Board<GameEntity> board, List<int> rows, List<int> cols)
        {
            var cells = new List<BoardCell<GameEntity>>();

            foreach (var row in rows)
            {
                for (int c = 0; c < board.ColumnsCount; c++)
                {
                    if (!IsInitiatorOrEffectOfNewSpecialMarblePostEffect(board.Cells[row, c].Item))
                        cells.Add(board.Cells[row,c]);
                }
            }

            foreach (var col in cols)
            {
                for (int r = 0; r < board.RowsCount; r++)
                {
                    var cell = board.Cells[r, col];
                    if (!cells.Contains(cell))
                    {
                        if (!IsInitiatorOrEffectOfNewSpecialMarblePostEffect(board.Cells[r, col].Item))
                            cells.Add(board.Cells[r, col]);
                    }
                }
            }

            return cells;
        }

        private List<BoardCell<GameEntity>> GetCellsWithColor(MarbleColor colorToClear, Board<GameEntity> board, List<BoardCell<GameEntity>> except)
        {
            var cells = new List<BoardCell<GameEntity>>();

            for (int r = 0; r < board.RowsCount; r++)
                for (int c = 0; c < board.ColumnsCount; c++)
                {
                    if (except.Contains(board.Cells[r, c]))
                    {
                        continue;
                    }

                    if (IsInitiatorOrEffectOfNewSpecialMarblePostEffect(board.Cells[r, c].Item))
                    {
                        continue;
                    }

                    var color = board.Cells[r, c].Item.GetComponent<MarbleComponent>().Color;
                    if (colorToClear != color)
                        continue;
                    cells.Add(board.Cells[r, c]);
                }
            return cells;
        }
    }
}