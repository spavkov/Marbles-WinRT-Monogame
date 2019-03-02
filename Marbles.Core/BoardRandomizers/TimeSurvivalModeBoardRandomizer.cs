using System;
using System.Collections.Generic;
using System.Linq;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components.SpecialMarbles;
using Marbles.Core.Model.Levels;
using Marbles.Core.Systems;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.BoardRandomizers
{
    public class TimeSurvivalModeBoardRandomizer : IBoardRandomizer
    {
        private World _world;
        private LevelDefinition _level;
        private readonly ISpecialItemsCountTracker _specialItemsCountTracker;
        private Action<BoardCell<GameEntity>> _onSpecialMarbleAddedToCell;
        private MarblesFactory _worldFactory;
        private Random _random = new Random();
        private SpecialItemDecider _specialItemsDecider;

        public TimeSurvivalModeBoardRandomizer(World world, LevelDefinition levelDefinition, ISpecialItemsCountTracker specialItemsCountTracker, Action<BoardCell<GameEntity>> onSpecialMarbleAddedToCell)
        {
            _world = world;
            _level = levelDefinition;
            _specialItemsCountTracker = specialItemsCountTracker;
            _onSpecialMarbleAddedToCell = onSpecialMarbleAddedToCell;
            _worldFactory = new MarblesFactory(_world);
            _specialItemsDecider = new SpecialItemDecider(levelDefinition, _specialItemsCountTracker);
        }

        public BoardRandomizationResult Randomize(MarbleBoard board, LevelDefinition levelDefinition)
        {
            var result = new BoardRandomizationResult();

            for (int row = 0; row < board.RowsCount; row++)
                for (int column = 0; column < board.ColumnsCount; column++)
                {
                    var cell = board.Cells[row, column];
                    var res = RandomizeSingleCell(cell, levelDefinition, true);
                    if (res.WeAddedSpecialMarble)
                    {
                        result.WeAddedSpecialMarbles = true;
                        result.CellsWithSpecialMarbles.Add(cell);
                    }
                }

            if (result.WeAddedSpecialMarbles)
            {
                DoPostSpecialMarbleRandomization(result.CellsWithSpecialMarbles, board, levelDefinition, true);
            }

            return result;
        }

        private void DoPostSpecialMarbleRandomization(List<BoardCell<GameEntity>> cellsWithSpecialMarbles, MarbleBoard board, LevelDefinition levelDefinition, bool isInitialRandomization)
        {
            foreach (var cell in cellsWithSpecialMarbles)
            {
                var specials = cell.Item.Components.OfType<SpecialMarbleComponent>();
                foreach (var specialMarbleComponent in specials)
                {

                }
            }
        }

        private CellRandimizationResult RandomizeSingleCell(BoardCell<GameEntity> cell, LevelDefinition levelDefinition, bool isInitialRandomization)
        {
            var result = new CellRandimizationResult();

            //var newItem = GenerateSpecialMarbleForCellIfConditionsAreRight(levelDefinition, isInitialRandomization, cell);
            

            result.WeAddedSpecialMarble = false;

            if (!result.WeAddedSpecialMarble)
            {
                GenerateSimpleMarbleForCell(cell, levelDefinition);
            }

            return result;
        }

        private GameEntity GenerateSpecialMarbleForCellIfConditionsAreRight(LevelDefinition levelDefinition, bool isInitialRandomization, BoardCell<GameEntity> cell)
        {
            SpecialMarbleRandomizationInstructions instructions;
            if (_specialItemsDecider.DoWeNeedToAddSpecialItem(isInitialRandomization, out instructions))
            {
                return GenerateSpecialMarbleForCell(cell, levelDefinition, instructions);
            }

            return null;
        }


        private void GenerateSimpleMarbleForCell(BoardCell<GameEntity> cell, LevelDefinition levelDefinition)
        {
            var color = GenerateRandomMarbleColor(levelDefinition);
            var marble = _worldFactory.CreateSimpleMarbleForCell(color, cell);
            marble.Start();
        }

        private MarbleColor GenerateRandomMarbleColor(LevelDefinition levelDefinition)
        {
            return levelDefinition.AvailableColors[_random.Next(levelDefinition.AvailableColors.Count)];
        }

        private GameEntity GenerateSpecialMarbleForCell(BoardCell<GameEntity> cell, LevelDefinition levelDefinition, SpecialMarbleRandomizationInstructions instructions)
        {
            var color = GenerateRandomMarbleColor(levelDefinition);
            var entityWithSpecialMarble = _worldFactory.GenerateSpecialMarbleForCell(cell, instructions, color);
            entityWithSpecialMarble.Start();
            OnSpecialMarbleAdded(cell);
            return entityWithSpecialMarble;
        }

        public CellRandimizationResult RandomizeCell(BoardCell<GameEntity> cell, LevelDefinition levelDefinition, bool isInitialRandomization)
        {
            return RandomizeSingleCell(cell, levelDefinition, isInitialRandomization);
        }

        private void OnSpecialMarbleAdded(BoardCell<GameEntity> cell)
        {
            if (_onSpecialMarbleAddedToCell != null)
            {
                _onSpecialMarbleAddedToCell(cell);
            }
        }

        public void Reset()
        {
            _onSpecialMarbleAddedToCell = null;
        }

        public void RandomizeCellWithSpecialMarble(BoardCell<GameEntity> cell, LevelDefinition levelDefinition, SpecialMarbleRandomizationInstructions instructions)
        {
            GenerateSpecialMarbleForCell(cell, levelDefinition, instructions);
        }
    }
}