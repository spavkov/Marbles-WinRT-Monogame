using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;

namespace Marbles.Core.Model
{
    public class BoardCell<TItem> : IHaveNeighbours<TItem> where TItem : class 
    {
        private readonly Board<TItem> _board;
        private Dictionary<NeighbourSide, BoardCell<TItem>> _neighbours = new Dictionary<NeighbourSide, BoardCell<TItem>>();
        public int Row { get; set; }
        public int Column { get; set; }

        public BoardCell(Board<TItem> board, int row, int column)
        {
            _board = board;
            Row = row;
            Column = column;
            Rectangle = new Rectangle();
            Center = new Vector2();
        }

        public TItem Item { get; set; }

        public Board<TItem> Board
        {
            get { return _board; }
        }

        public Dictionary<NeighbourSide, BoardCell<TItem>> Neighbours
        {
            get { return _neighbours; }
            set { _neighbours = value; }
        }

        public Rectangle Rectangle;

        public Vector2 Center;



        public bool HasNeighbourCell(NeighbourSide side)
        {
            return Neighbours.ContainsKey(side);
        }

        public bool IsNighbouringCell(BoardCell<TItem> nextCellTouched)
        {
            return Neighbours.Values.Any(c => c.Equals(nextCellTouched));
        }

        public override string ToString()
        {
            return string.Format("{0}:{1} -> {2}", Row, Column, Item);
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            var p = obj as BoardCell<TItem>;
            if (p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (Column == p.Column && Row == p.Row);
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + Column.GetHashCode();
            hash = (hash * 7) + Row.GetHashCode();
            return hash;
        }
    }
}