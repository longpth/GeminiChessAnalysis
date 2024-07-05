using GeminiChessAnalysis.ViewModels;
using System;
using System.Collections.Generic;
using Point = Xamarin.Forms.Point;

namespace GeminiChessAnalysis.Models
{

    public class PiecesMoveRecord : BaseModel
    {
        private EnumKingOrQueenSide _castling = EnumKingOrQueenSide.None;
        private bool _isWhiteAtBottom = true;

        public PiecesMoveRecord(bool isWhiteAtBottom)
        {
            _isWhiteAtBottom = isWhiteAtBottom;
        }

        public EnumKingOrQueenSide Castling
        {
            get => _castling;
            set
            {
                if (_castling != value)
                {
                    if (value == EnumKingOrQueenSide.KingSide)
                    {
                        PgnType = "O-O";
                    }
                    else if (value == EnumKingOrQueenSide.QueenSide)
                    {
                        PgnType = "O-O-O";
                    }
                    OnPropertyChanged(nameof(Castling));
                }
            }
        }
        private Position _piecePosition;
        public Position PiecePosition
        {
            get => _piecePosition;
            set
            {
                if (_piecePosition != value)
                {
                    _piecePosition = value;
                    if (_piecePosition != null)
                    {
                        PgnPostion = ConvertToPGN(_piecePosition.RowIndex, _piecePosition.ColIndex, _isWhiteAtBottom);
                    }
                    OnPropertyChanged(nameof(PiecePosition));
                }
            }
        }
        private EnumPieceType _EnumPieceType;
        public EnumPieceType PieceTypeProp
        {
            get => _EnumPieceType;
            set
            {
                if (_EnumPieceType != value)
                {
                    _EnumPieceType = value;

                    PgnType = PieceType2String(_EnumPieceType);

                    OnPropertyChanged(nameof(PieceTypeProp));
                }
            }
        }

        public static string PieceType2String(EnumPieceType pieceType)
        {
            switch (pieceType)
            {
                case EnumPieceType.Pawn:
                    return "";
                case EnumPieceType.Knight:
                    return "N";
                case EnumPieceType.Bishop:
                    return "B";
                case EnumPieceType.Rook:
                    return "R";
                case EnumPieceType.Queen:
                    return "Q";
                case EnumPieceType.King:
                    return "K";
                default:
                    return "";
            }
        }

        public static string PieceType2StringFullName(EnumPieceType pieceType)
        {
            switch (pieceType)
            {
                case EnumPieceType.Pawn:
                    return "Pawn";
                case EnumPieceType.Knight:
                    return "Knight";
                case EnumPieceType.Bishop:
                    return "Bishop";
                case EnumPieceType.Rook:
                    return "Rook";
                case EnumPieceType.Queen:
                    return "Queen";
                case EnumPieceType.King:
                    return "King";
                default:
                    return "";
            }
        }

        public static string Col2String(int colIndex, bool isWhiteAtBottom)
        {
            char column = isWhiteAtBottom ? (char)('a' + colIndex) : (char)('h' - colIndex);
            return $"{column}";
        }

        public string PgnType { get; set; }
        public string PgnPostion { get; set; }
        public static string ConvertToPGN(int rowIndex, int colIndex, bool isWhiteAtBottom)
        {
            char column;
            char row;

            if (isWhiteAtBottom)
            {
                column = (char)('a' + colIndex);
                row = (char)('8' - rowIndex);
            }
            else
            {
                column = (char)('h' - colIndex);
                row = (char)('1' + rowIndex);
            }

            return $"{column}{row}";
        }
        public static List<Point> ConvertStringToPieceIndexes(string move, bool isWhiteAtBottom)
        {
            // Ensure the move string is valid
            if (move.Length != 4) throw new ArgumentException("Invalid move format.");

            // Parse the string
            char startColumnChar = move[0];
            int startRowNumber = int.Parse(move[1].ToString());
            char endColumnChar = move[2];
            int endRowNumber = int.Parse(move[3].ToString());

            // Convert to indices based on the orientation of the board
            int startColumnIndex = isWhiteAtBottom ? startColumnChar - 'a' : 'h' - startColumnChar;
            int startRowIndex = isWhiteAtBottom ? 8 - startRowNumber : startRowNumber - 1;
            int endColumnIndex = isWhiteAtBottom ? endColumnChar - 'a' : 'h' - endColumnChar;
            int endRowIndex = isWhiteAtBottom ? 8 - endRowNumber : endRowNumber - 1;

            // Create Points for start and end positions
            Point startPoint = new Point(startColumnIndex, startRowIndex);
            Point endPoint = new Point(endColumnIndex, endRowIndex);

            // Return as a list of Points
            return new List<Point> { startPoint, endPoint };
        }
    }
}
