using System;
using System.Collections.Generic;
using System.Text;

namespace GeminiChessAnalysis.Models
{
    public class Move
    {
        public int StartRow { get; set; }
        public int StartCol { get; set; }
        public int EndRow { get; set; }
        public int EndCol { get; set; }

        public Move(int startRow, int startCol, int endRow, int endCol)
        {
            StartRow = startRow;
            StartCol = startCol;
            EndRow = endRow;
            EndCol = endCol;
        }
    }
    public class MovePixel
    {
        public double TranslateX { get; set; }
        public double TranslateY { get; set; }
    }

    public class MovePixelWithTime
    {
        public double TranslateX { get; set; }
        public double TranslateY { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class Position
    {
        public Position(int row, int col)
        {
            RowIndex = row;
            ColIndex = col;
        }
        public int RowIndex { get; set; }
        public int ColIndex { get; set; }
    }
}
