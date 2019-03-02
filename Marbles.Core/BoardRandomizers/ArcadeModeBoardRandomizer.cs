using System;
using System.Collections.Generic;
using System.Linq;
using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Model.Components.SpecialMarbles;
using Marbles.Core.Model.Levels;
using Marbles.Core.Systems;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.BoardRandomizers
{
    public class ArcadeModeBoardRandomizer : IBoardRandomizer
    {
        private readonly World _world;
        private readonly ISpecialItemsCountTracker _specialItemsCountTracker;
        private Action<BoardCell<GameEntity>> _specialMarbleAdded;
        private Random _random;
        private MarblesFactory _worldFactory;
        private SpecialItemDecider _specialItemsDecider;
        private MarbleColor[] _allColors;

        public ArcadeModeBoardRandomizer(World world, LevelDefinition level, ISpecialItemsCountTracker specialItemsCountTracker, Action<BoardCell<GameEntity>> specialMarbleAdded = null )
        {
            _world = world;
            _specialItemsCountTracker = specialItemsCountTracker;
            _specialMarbleAdded = specialMarbleAdded;
            _random = new Random();
            _worldFactory = new MarblesFactory(_world);
            _specialItemsDecider = new SpecialItemDecider(level, _specialItemsCountTracker);
            _allColors = Enum.GetValues(typeof(MarbleColor)).Cast<MarbleColor>().ToArray();
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
                DoPostSpecialMarbleRandomization(result.CellsWithSpecialMarbles , board,levelDefinition, true);
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

            var newItem = GenerateSpecialMarbleForCellIfConditionsAreRight(levelDefinition, isInitialRandomization, cell);

            result.WeAddedSpecialMarble = newItem != null;

            if (!result.WeAddedSpecialMarble)
            {
                GenerateSimpleMarbleForCell(cell, levelDefinition);
            }

            return result;
        }


        private GameEntity GenerateSpecialMarbleForCell(BoardCell<GameEntity> cell, LevelDefinition levelDefinition, SpecialMarbleRandomizationInstructions instructions)
        {
            var color = GenerateRandomMarbleColor(levelDefinition);
            var entityWithSpecialMarble = _worldFactory.GenerateSpecialMarbleForCell(cell, instructions,color);
            entityWithSpecialMarble.Start();
            OnSpecialMarbleAdded(cell);
            return entityWithSpecialMarble;
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

        private void OnSpecialMarbleAdded(BoardCell<GameEntity> cell)
        {
            if (_specialMarbleAdded != null)
            {
                _specialMarbleAdded(cell);
            }
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

        public CellRandimizationResult RandomizeCell(BoardCell<GameEntity> cell, LevelDefinition levelDefinition, bool isInitialRandomization)
        {
            return RandomizeSingleCell(cell, levelDefinition, isInitialRandomization);
        }

        public void Reset()
        {
            _specialItemsDecider.Reset();
            _specialMarbleAdded = null;

        }

        public void RandomizeCellWithSpecialMarble(BoardCell<GameEntity> cell, LevelDefinition levelDefinition, SpecialMarbleRandomizationInstructions instructions)
        {
            GenerateSpecialMarbleForCell(cell, levelDefinition,  instructions);
        }
    }
}