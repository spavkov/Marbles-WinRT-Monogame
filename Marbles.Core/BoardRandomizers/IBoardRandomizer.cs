using Marbles.Core.Helpers;
using Marbles.Core.Model;
using Marbles.Core.Model.Levels;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.BoardRandomizers
{
    public interface IBoardRandomizer
    {
        BoardRandomizationResult Randomize(MarbleBoard board, LevelDefinition levelDefinition);
        CellRandimizationResult RandomizeCell(BoardCell<GameEntity> cell, LevelDefinition levelDefinition, bool isInitialRandomization);
        void Reset();
        void RandomizeCellWithSpecialMarble(BoardCell<GameEntity> cell, LevelDefinition levelDefinition, SpecialMarbleRandomizationInstructions instructions);
    }
}