using System;
using System.Collections.Generic;
using System.Linq;

namespace GeminiChessAnalysis.Helpers
{
    public class ChessGame
    {
        public string CurrentFEN { get; private set; }
        public ChessBoard Board { get; private set; }
        public List<string> FENList { get; private set; }

        public ChessGame(string initialFEN= "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")
        {
            CurrentFEN = initialFEN;
            Board = new ChessBoard(initialFEN);
            FENList = new List<string> { initialFEN };
        }

        public void ApplyMovesFromPGN(string pgn)
        {
            var moves = ParsePGN(pgn);
            foreach (var move in moves)
            {
                Board.ApplyMove(move);
                CurrentFEN = Board.ToFEN();
                FENList.Add(CurrentFEN);
            }
        }

        private List<string> ParsePGN(string pgn)
        {
            // Basic PGN parsing, splitting by spaces and removing move numbers
            return pgn.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                      .Where(x => !x.Contains("."))
                      .ToList();
        }
    }

    public class ChessBoard
    {
        // Represents the chess board and handles applying moves
        private char[,] board;
        // Additional properties and methods to handle other aspects such as en passant, turn, move counters, and castling rights
        private char currentTurn = 'w'; // 'w' for white, 'b' for black
        private int fullMoveCounter = 1; // Starts at 1 and increments after black's move

        public ChessBoard(string fen)
        {
            LoadFromFEN(fen);
        }

        public void LoadFromFEN(string fen)
        {
            // Initialize the board from the FEN string
            var parts = fen.Split(' ');
            var rows = parts[0].Split('/');
            board = new char[8, 8];

            for (int r = 0; r < 8; r++)
            {
                int file = 0;
                foreach (var ch in rows[r])
                {
                    if (char.IsDigit(ch))
                    {
                        file += ch - '0';
                    }
                    else
                    {
                        board[r, file] = ch;
                        file++;
                    }
                }
            }
        }

        public void ApplyMove(string move)
        {
            // Remove special characters for clarity
            move = move.Replace("+", "").Replace("#", "");

            // Handle castling
            if (move == "O-O" || move == "O-O-O")
            {
                HandleCastling(move);
            }
            else
            {
                char pieceType;
                int targetFile, targetRank;
                bool isCapture = move.Contains("x");

                // Check for captures and adjust the move string accordingly
                if (isCapture)
                {
                    int xIndex = move.IndexOf('x');
                    // If the piece type is specified (e.g., "Qxd5"), extract it
                    if (char.IsUpper(move[0]) && xIndex == 1)
                    {
                        pieceType = move[0];
                        move = move.Substring(2); // Remove piece type and 'x' from move string
                    }
                    else if (char.IsLower(move[0]) && xIndex == 1) // For pawn captures like "exd5"
                    {
                        pieceType = currentTurn == 'w' ? 'P' : 'p';
                        move = move.Substring(2); // Adjust for pawn captures to only leave the target square
                    }
                    else
                    {
                        pieceType = currentTurn == 'w' ? 'P' : 'p'; // Default to pawn if not specified
                        move = move.Substring(1); // Assume the move is a simple pawn capture without specifying the file
                    }
                }
                else if (char.IsUpper(move[0]))
                {
                    pieceType = move[0]; // Piece type (N, B, R, Q, K)
                    move = move.Substring(1); // Remove piece type from move string
                }
                else
                {
                    pieceType = 'P'; // Pawn move
                }

                pieceType = currentTurn == 'w' ? char.ToUpper(pieceType) : char.ToLower(pieceType);

                // Extract target file and rank
                targetFile = move[move.Length - 2] - 'a';
                targetRank = 8 - (move[move.Length - 1] - '0');

                // Extract disambiguation information if present
                int disambiguationFile = -1;
                int disambiguationRank = -1;

                if (move.Length == 4)
                {
                    if (char.IsLetter(move[1]))
                    {
                        disambiguationFile = move[1] - 'a';
                    }
                    else if (char.IsDigit(move[1]))
                    {
                        disambiguationRank = 8 - (move[1] - '0');
                    }
                }

                // Find all pieces of the specified type that can legally move to the target square
                List<(int, int)> possibleStartPositions = FindPossibleStartPositions(pieceType, targetFile, targetRank, disambiguationFile >= 0 ? (int?)disambiguationFile : null, disambiguationRank >= 0 ? (int?)disambiguationRank : null);

                foreach (var (startRank, startFile) in possibleStartPositions)
                {
                    // Check if the move is legal (e.g., path is clear, not putting own king in check)
                    // This is a simplified check; real implementation needs comprehensive validation
                    if (IsMoveLegal(startRank, startFile, targetRank, targetFile))
                    {
                        // Execute the move
                        board[targetRank, targetFile] = board[startRank, startFile];
                        board[startRank, startFile] = '\0';
                        break; // Assuming only one valid move is possible
                    }
                }
            }

            // Toggle the turn at the end of the move
            currentTurn = currentTurn == 'w'? 'b' : 'w';
            fullMoveCounter++;
        }

        /// <summary>
        /// Finds all pieces of the specified type that can legally move to the target square.
        /// </summary>
        /// <param name="pieceType">The type of the piece (e.g., 'P' for pawn, 'R' for rook).</param>
        /// <param name="targetFile">The file (column) of the target square (0-7).</param>
        /// <param name="targetRank">The rank (row) of the target square (0-7).</param>
        /// <param name="disambiguationFile">Optional file (column) for disambiguation (0-7).</param>
        /// <param name="disambiguationRank">Optional rank (row) for disambiguation (0-7).</param>
        /// <returns>A list of tuples representing the ranks and files of possible start positions.</returns>
        private List<(int, int)> FindPossibleStartPositions(char pieceType, int targetFile, int targetRank, int? disambiguationFile = null, int? disambiguationRank = null)
        {
            var positions = new List<(int, int)>();

            for (int r = 0; r < 8; r++)
            {
                for (int f = 0; f < 8; f++)
                {
                    if (board[r, f] == pieceType)
                    {
                        // Check if the piece can legally move to the target square
                        if (IsMoveLegal(r, f, targetRank, targetFile))
                        {
                            // Apply disambiguation if provided
                            if ((disambiguationFile == null || f == disambiguationFile) &&
                                (disambiguationRank == null || r == disambiguationRank))
                            {
                                positions.Add((r, f));
                            }
                        }
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// Determines if a move is legal based on the piece type and the start and target positions.
        /// </summary>
        /// <param name="startRank">The rank (row) of the starting square (0-7).</param>
        /// <param name="startFile">The file (column) of the starting square (0-7).</param>
        /// <param name="targetRank">The rank (row) of the target square (0-7).</param>
        /// <param name="targetFile">The file (column) of the target square (0-7).</param>
        /// <returns>True if the move is legal, otherwise false.</returns>
        private bool IsMoveLegal(int startRank, int startFile, int targetRank, int targetFile)
        {
            char piece = board[startRank, startFile];
            int rankDiff = targetRank - startRank;
            int fileDiff = targetFile - startFile;

            switch (char.ToLower(piece))
            {
                case 'p': // Pawn
                    return IsPawnMoveLegal(startRank, startFile, targetRank, targetFile, rankDiff, fileDiff);
                case 'r': // Rook
                    return IsRookMoveLegal(startRank, startFile, targetRank, targetFile, rankDiff, fileDiff);
                case 'b': // Bishop
                    return IsBishopMoveLegal(startRank, startFile, targetRank, targetFile, rankDiff, fileDiff);
                case 'n': // Knight
                    return IsKnightMoveLegal(rankDiff, fileDiff);
                case 'k':
                case 'q':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if a pawn move is legal.
        /// </summary>
        /// <param name="startRank">The rank (row) of the starting square (0-7).</param>
        /// <param name="startFile">The file (column) of the starting square (0-7).</param>
        /// <param name="targetRank">The rank (row) of the target square (0-7).</param>
        /// <param name="targetFile">The file (column) of the target square (0-7).</param>
        /// <param name="rankDiff">The difference in ranks between the start and target squares.</param>
        /// <param name="fileDiff">The difference in files between the start and target squares.</param>
        /// <returns>True if the pawn move is legal, otherwise false.</returns>
        private bool IsPawnMoveLegal(int startRank, int startFile, int targetRank, int targetFile, int rankDiff, int fileDiff)
        {
            char piece = board[startRank, startFile];
            bool isWhite = piece == 'P';
            int direction = isWhite ? -1 : 1;

            // Normal move
            if (fileDiff == 0 && board[targetRank, targetFile] == '\0')
            {
                if (rankDiff == direction) return true; // Single step
                if ((isWhite && startRank == 6 || !isWhite && startRank == 1) && rankDiff == 2 * direction && board[startRank + direction, startFile] == '\0')
                    return true; // Double step from starting position
            }

            // Capture move
            if (Math.Abs(fileDiff) == 1 && rankDiff == direction && board[targetRank, targetFile] != '\0' && char.IsUpper(board[targetRank, targetFile]) != isWhite)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a rook move is legal.
        /// </summary>
        /// <param name="startRank">The rank (row) of the starting square (0-7).</param>
        /// <param name="startFile">The file (column) of the starting square (0-7).</param>
        /// <param name="targetRank">The rank (row) of the target square (0-7).</param>
        /// <param name="targetFile">The file (column) of the target square (0-7).</param>
        /// <param name="rankDiff">The difference in ranks between the start and target squares.</param>
        /// <param name="fileDiff">The difference in files between the start and target squares.</param>
        /// <returns>True if the rook move is legal, otherwise false.</returns>
        private bool IsRookMoveLegal(int startRank, int startFile, int targetRank, int targetFile, int rankDiff, int fileDiff)
        {
            if (rankDiff != 0 && fileDiff != 0) return false; // Rook moves in straight lines

            int stepRank = rankDiff == 0 ? 0 : rankDiff / Math.Abs(rankDiff);
            int stepFile = fileDiff == 0 ? 0 : fileDiff / Math.Abs(fileDiff);

            for (int r = startRank + stepRank, f = startFile + stepFile; r != targetRank || f != targetFile; r += stepRank, f += stepFile)
            {
                if (board[r, f] != '\0') return false; // Path is not clear
            }

            return true;
        }

        /// <summary>
        /// Determines if a bishop move is legal.
        /// </summary>
        /// <param name="startRank">The rank (row) of the starting square (0-7).</param>
        /// <param name="startFile">The file (column) of the starting square (0-7).</param>
        /// <param name="targetRank">The rank (row) of the target square (0-7).</param>
        /// <param name="targetFile">The file (column) of the target square (0-7).</param>
        /// <param name="rankDiff">The difference in ranks between the start and target squares.</param>
        /// <param name="fileDiff">The difference in files between the start and target squares.</param>
        /// <returns>True if the bishop move is legal, otherwise false.</returns>
        private bool IsBishopMoveLegal(int startRank, int startFile, int targetRank, int targetFile, int rankDiff, int fileDiff)
        {
            if (Math.Abs(rankDiff) != Math.Abs(fileDiff)) return false; // Bishop moves diagonally

            int stepRank = rankDiff / Math.Abs(rankDiff);
            int stepFile = fileDiff / Math.Abs(fileDiff);

            for (int r = startRank + stepRank, f = startFile + stepFile; r != targetRank || f != targetFile; r += stepRank, f += stepFile)
            {
                if (board[r, f] != '\0') return false; // Path is not clear
            }

            return true;
        }

        /// <summary>
        /// Determines if a knight move is legal.
        /// </summary>
        /// <param name="rankDiff">The difference in ranks between the start and target squares.</param>
        /// <param name="fileDiff">The difference in files between the start and target squares.</param>
        /// <returns>True if the knight move is legal, otherwise false.</returns>
        private bool IsKnightMoveLegal(int rankDiff, int fileDiff)
        {
            return (Math.Abs(rankDiff) == 2 && Math.Abs(fileDiff) == 1) || (Math.Abs(rankDiff) == 1 && Math.Abs(fileDiff) == 2);
        }

        private void HandleCastling(string move)
        {
            if (move == "O-O")
            {
                if (currentTurn == 'w')
                {
                    board[7, 6] = 'K';
                    board[7, 4] = '\0';
                    board[7, 5] = 'R';
                    board[7, 7] = '\0';
                }
                else
                {
                    board[0, 6] = 'k';
                    board[0, 4] = '\0';
                    board[0, 5] = 'r';
                    board[0, 7] = '\0';
                }
            }
            else if (move == "O-O-O")
            {
                if (currentTurn == 'w')
                {
                    board[7, 2] = 'K';
                    board[7, 4] = '\0';
                    board[7, 3] = 'R';
                    board[7, 0] = '\0';
                }
                else
                {
                    board[0, 2] = 'k';
                    board[0, 4] = '\0';
                    board[0, 3] = 'r';
                    board[0, 0] = '\0';
                }
            }

            // Update castling rights (not shown)
        }

        public string ToFEN()
        {
            // Convert the board back to a FEN string
            var fen = new List<string>();
            for (int r = 0; r < 8; r++)
            {
                int emptyCount = 0;
                var row = new List<char>();
                for (int f = 0; f < 8; f++)
                {
                    if (board[r, f] == '\0')
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            row.Add((char)('0' + emptyCount));
                            emptyCount = 0;
                        }
                        row.Add(board[r, f]);
                    }
                }
                if (emptyCount > 0)
                {
                    row.Add((char)('0' + emptyCount));
                }
                fen.Add(new string(row.ToArray()));
            }

            // Update the tail of the FEN string to include the current turn, castling availability, en passant target, half-move clock, and full-move counter
            string turnIndicator = currentTurn == 'w' ? "w" : "b";
            // Assuming castling availability and en passant target are not changed in this example
            string fenTail = $"{turnIndicator} KQkq - 0 {fullMoveCounter}";

            return string.Join("/", fen) + " " + fenTail;
        }
    }
}
