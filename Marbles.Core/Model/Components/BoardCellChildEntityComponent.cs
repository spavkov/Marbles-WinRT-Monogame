using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Model.Components
{
    public class BoardCellChildEntityComponent : Component
    {
        public BoardCellChildEntityComponent(BoardCell<GameEntity> cell)
        {
            Cell = cell;
        }

        public BoardCell<GameEntity> Cell { get; set; }
    }
}