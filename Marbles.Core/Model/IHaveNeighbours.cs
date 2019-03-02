using System.Collections.Generic;

namespace Marbles.Core.Model
{
    public interface IHaveNeighbours<TItem> where TItem : class
    {
        Dictionary<NeighbourSide, BoardCell<TItem>> Neighbours { get; set; }
        bool HasNeighbourCell(NeighbourSide side);
    }
}