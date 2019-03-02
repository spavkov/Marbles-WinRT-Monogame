using System;
using System.Collections.Generic;

namespace Marbles.Core.Model
{
    public class Board<TItem> where TItem : class 
    {
        private List<BoardCell<TItem>> _allCells = new List<BoardCell<TItem>>();
        private List<TItem> _additionalItems = new List<TItem>();
        public int RowsCount { get; set; }
        public int ColumnsCount { get; set; }

        public Board(int rowsCount, int columnsCount)
        {
            RowsCount = rowsCount;
            ColumnsCount = columnsCount;
            InitializeCellsAndNeighbours();
        }

        public List<TItem> AdditionalItems
        {
            get { return _additionalItems; }
            set { _additionalItems = value; }
        }

        public BoardCell<TItem> [,] Cells { get; set; }
        public List<BoardCell<TItem>>  AllCells
        {
            get { return _allCells; }
            set { _allCells = value; }
        }

        private void InitializeCellsAndNeighbours()
        {
            AllCells.Clear();

            Cells = new BoardCell<TItem>[RowsCount,ColumnsCount];
                for(int row = 0; row < RowsCount; row++)
                    for (int column = 0; column < ColumnsCount; column++)
                    {
                        var cell = new BoardCell<TItem>(this, row, column);
                        Cells[row, column] = cell;
                        AllCells.Add(cell);
                    }

            InitCellNeighbours();
        }

        private void DoForAllCells(Action<BoardCell<TItem>> toDo)
        {
                for(int row = 0; row < RowsCount; row++)
                    for (int column = 0; column < ColumnsCount; column++)
                    {
                        toDo(Cells[row, column]);
                    }
        }

        private void InitCellNeighbours()
        {
            DoForAllCells((cell) =>
                              {
                                  if (cell.Row - 1 >= 0)
                                  {
                                      var upCell = Cells[cell.Row - 1, cell.Column];
                                      cell.Neighbours.Add(NeighbourSide.Up, upCell);
                                  }

                                  if (cell.Row + 1 < RowsCount)
                                  {
                                      var downCell = Cells[cell.Row + 1, cell.Column];
                                      cell.Neighbours.Add(NeighbourSide.Down, downCell);
                                  }

                                  if (cell.Column - 1 >= 0)
                                  {
                                      var leftCell = Cells[cell.Row, cell.Column - 1];
                                      cell.Neighbours.Add(NeighbourSide.Left, leftCell);
                                  }

                                  if (cell.Column + 1 < ColumnsCount)
                                  {
                                      var rightCell = Cells[cell.Row, cell.Column + 1];
                                      cell.Neighbours.Add(NeighbourSide.Right, rightCell);
                                  }

                                  if (cell.Column - 1 >= 0 && cell.Row - 1 >= 0)
                                  {
                                      var upLeftCell = Cells[cell.Row - 1, cell.Column - 1];
                                      cell.Neighbours.Add(NeighbourSide.LeftUp, upLeftCell);
                                  }

                                  if (cell.Row + 1 < RowsCount && cell.Column + 1 < ColumnsCount)
                                  {
                                      var downRightCell = Cells[cell.Row + 1, cell.Column + 1];
                                      cell.Neighbours.Add(NeighbourSide.DownRight, downRightCell);
                                  }

                                  if (cell.Row - 1 >= 0 && cell.Column + 1 < ColumnsCount)
                                  {
                                      var upRightCell = Cells[cell.Row - 1, cell.Column + 1];
                                      cell.Neighbours.Add(NeighbourSide.UpRight, upRightCell);
                                  }

                                  if (cell.Row + 1 < RowsCount && cell.Column - 1 >= 0)
                                  {
                                      var leftDownCell = Cells[cell.Row + 1, cell.Column - 1];
                                      cell.Neighbours.Add(NeighbourSide.DownLeft, leftDownCell);
                                  }
                              });
        }

        public void SetItem(int row, int column, TItem item)
        {
            var cell = Cells[row, column];
            cell.Item = item;
        }
    }
}