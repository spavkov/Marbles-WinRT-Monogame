using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Model
{
    public class MarbleBoard : Board<GameEntity>
    {
        public MarbleBoard(int rowsCount, int columnsCount)
            : base(rowsCount, columnsCount)
        {
        }
    }
}