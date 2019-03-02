using System.Collections.Generic;
using Marbles.Core.Model;
using Marbles.Core.Model.Components;
using Marbles.Core.Systems;
using System.Linq;
using Microsoft.Xna.Framework;
using Roboblob.GameEntitySystem.WinRT;

namespace Marbles.Core.Helpers
{
    public class CompletedTouchSequenceShapeDetector
    {
        private TouchSequenceShapeResult NoneSequenceShapeResult = new TouchSequenceShapeResult()
            {
                Type = TouchSequenceShapeType.None,
                PointsThatFormedSequenceShape = new List<Vector2>(),
                Color = MarbleColor.Silver                
            };

        public TouchSequenceShapeResult Detect(FingerTouchSequenceEndedArgs e)
        {
            if (e.LastEntityThatWasTouchedRegardlessIfItWasInSequence.GetComponent<MarbleComponent>().Color != e.Color)
            {
                return NoneSequenceShapeResult;
            }

/*            if (!e.LastTouchedEntityInSequence.GetComponent<BoardCellChildEntityComponent>().Cell.IsNighbouringCell(e.CellsTouchedInSequenceSoFar[0]))
            {
                return NoneSequenceShapeResult;
            }*/

            bool itsClosedSequence = false;

            var allCells = new List<BoardCell<GameEntity>>();

            foreach (var cell in e.CellsTouchedInSequenceSoFar)
            {
                if (!itsClosedSequence)
                {
                    itsClosedSequence = cell.Item.Equals(e.LastEntityThatWasTouchedRegardlessIfItWasInSequence);
                }

                if (itsClosedSequence)
                {
                    allCells.Add(cell);
                }
            }

            if (!itsClosedSequence)
            {
                return NoneSequenceShapeResult;
            }

            allCells.Add(e.LastEntityThatWasTouchedRegardlessIfItWasInSequence.GetComponent<BoardCellChildEntityComponent>().Cell);

            var rows = allCells.Select(s => s.Row).ToList();
            var columns = allCells.Select(s => s.Column).ToList();

            var minRow = rows.Min();
            var maxRow = rows.Max();

            var minCol = columns.Min();
            var maxCol = columns.Max();

            var width = maxCol - minCol;
            var height = maxRow - minRow;

            var rowsSequenceOrder = SequenceProgress.NotStarted;
            var colsSequenceOrder = SequenceProgress.NotStarted;

            var totalSequenceTurns = 0;

            var pointsThatFormedShape = new List<Vector2>() { allCells[0].Center };

            for (int i = 0; i < allCells.Count-1; i++)
            {
                var current = allCells[i];
                var next = allCells[i + 1];

                if (rowsSequenceOrder == SequenceProgress.NotStarted || colsSequenceOrder == SequenceProgress.NotStarted)
                {
                    rowsSequenceOrder = DetectSequenceProgress(current.Row, next.Row);
                    colsSequenceOrder = DetectSequenceProgress(current.Column, next.Column);
                    continue;
                }

                var nextRowSeq = DetectSequenceProgress(current.Row, next.Row);
                var nextColSeq = DetectSequenceProgress(current.Column, next.Column);
                
                if (nextRowSeq != rowsSequenceOrder || nextColSeq != colsSequenceOrder)
                {
                    totalSequenceTurns++;
                    pointsThatFormedShape.Add(current.Center);
                }

                rowsSequenceOrder = nextRowSeq;
                colsSequenceOrder = nextColSeq;
            }

            var isSquare = totalSequenceTurns == 3 && width == height;

            if (isSquare)
                return new TouchSequenceShapeResult()
                    {
                        Type = TouchSequenceShapeType.Square,
                        PointsThatFormedSequenceShape = pointsThatFormedShape,
                        Color = e.Color
                    };

            var isRectangle = totalSequenceTurns == 3 && width != height;

            if (isRectangle)
            {
                return new TouchSequenceShapeResult()
                {
                    Type = TouchSequenceShapeType.Rectangle,
                    PointsThatFormedSequenceShape = pointsThatFormedShape,
                    Color = e.Color
                };
            }

            var isTriangle = totalSequenceTurns == 2;

            if (isTriangle)
            {
                return new TouchSequenceShapeResult()
                {
                    Type = TouchSequenceShapeType.Triangle,
                    PointsThatFormedSequenceShape = pointsThatFormedShape,
                    Color = e.Color

                };
            }

            return NoneSequenceShapeResult;
        }

        private SequenceProgress DetectSequenceProgress(int current, int next)
        {
            if (next == current)
            {
                return SequenceProgress.Constant;
            }
            if (next > current)
            {
                return SequenceProgress.Inc;
            }

            return SequenceProgress.Dec;
        }

        private enum SequenceProgress
        {
            NotStarted,
            Inc,
            Dec,
            Constant
        }
    }

    public struct TouchSequenceShapeResult
    {
        public TouchSequenceShapeType Type;
        public List<Vector2> PointsThatFormedSequenceShape;
        public MarbleColor Color;
    }

    public enum TouchSequenceShapeType
    {
        None,
        Square,
        Rectangle,
        Triangle
    }
}