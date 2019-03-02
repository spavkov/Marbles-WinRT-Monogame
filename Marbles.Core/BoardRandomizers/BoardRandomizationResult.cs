using System.Collections.Generic;
using Marbles.Core.Model;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.BoardRandomizers
{
    public class BoardRandomizationResult
    {
        public bool WeAddedSpecialMarbles;
        public List<BoardCell<GameEntity>> CellsWithSpecialMarbles = new List<BoardCell<GameEntity>>();
    }
}