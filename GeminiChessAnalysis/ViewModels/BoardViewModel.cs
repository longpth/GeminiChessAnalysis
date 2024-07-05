using GeminiChessAnalysis.Helpers;
using GeminiChessAnalysis.Models;
using GeminiChessAnalysis.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace GeminiChessAnalysis.ViewModels
{
    public enum EnumWhiteSide
    {
        Top = -1,
        Bottom = 1
    }
    public enum EnumKingOrQueenSide
    {
        QueenSide = 0,
        KingSide,
        None
    }
    public class MoveItem
    {
        public string StrMove { get; set; }
        public int MoveIndex { get; set; }
        public bool IsVisibleAndClickable { get; set; } = false;
    }

    public class BoardViewModel : BaseViewModel
    {
        #region Constant
        public const int Size = 8; // Standard chess board size is 8x8
        public const double MovePieceScale = 2.0;
        public const double StayPieceScale = 1.0;
        #endregion

        #region Private Variables
        Piece _currentCell = null;
        Piece _currentPiece = null;
        private EnumWhiteSide _whiteSide = EnumWhiteSide.Bottom;
        private int _moveCount = 0;
        private Piece[,] _kings = new Piece[1,2];
        private Piece[,] _queens = new Piece[1,2];
        private Piece[,] _rooks = new Piece[2,2];
        private Piece[,] _knights = new Piece[2, 2];
        private Piece[,] _bishops = new Piece[2, 2];
        private Piece[,] _pawns = new Piece[2,8];

        private List<Piece[,]> _kingsSnapshot     = new List<Piece[,]>();
        private List<Piece[,]> _queensSnapshot    = new List<Piece[,]>();
        private List<Piece[,]> _rooksSnapshot     = new List<Piece[,]>();
        private List<Piece[,]> _knightsSnapshot   = new List<Piece[,]>();
        private List<Piece[,]> _bishopsSnapshot   = new List<Piece[,]>();
        private List<Piece[,]> _pawnsSnapshot     = new List<Piece[,]>();

        private List<Piece[,]> _kingsSnapshotSub = new List<Piece[,]>();
        private List<Piece[,]> _queensSnapshotSub = new List<Piece[,]>();
        private List<Piece[,]> _rooksSnapshotSub = new List<Piece[,]>();
        private List<Piece[,]> _knightsSnapshotSub = new List<Piece[,]>();
        private List<Piece[,]> _bishopsSnapshotSub = new List<Piece[,]>();
        private List<Piece[,]> _pawnsSnapshotSub = new List<Piece[,]>();

        private List<List<ObservableCollection<Piece>>> _snapShots = new List<List<ObservableCollection<Piece>>>();
        private Dictionary<int, List<ObservableCollection<Piece>>> _snapShotSubs = new Dictionary<int, List<ObservableCollection<Piece>>>();
        private IStockfish _stockfish = new StockfishWrapper();
        private int _animateTime = 200;
        private static BoardViewModel _instance;
        private bool _isNewMove = false;
        private bool _bestMoveAvailable = false;
        private string _question_for_gemnini = "";
        private ChessGame _chessGame;
        private bool _isLoadedPgnMove = false;
        private bool _isBranching = false;
        private int _branchingMoveAtCount = 0;
        #endregion

        #region Properties
        private List<ObservableCollection<Piece>> _ChessPiecesForDisplaying = new List<ObservableCollection<Piece>>();
        public List<ObservableCollection<Piece>> ChessPiecesForDisplaying
        {
            get => _ChessPiecesForDisplaying;
            set
            {
                _ChessPiecesForDisplaying = value;
                OnPropertyChanged(nameof(ChessPiecesForDisplaying));
            }
        }

        public List<ObservableCollection<Piece>> _boardCells = new List<ObservableCollection<Piece>>();
        public List<ObservableCollection<Piece>> BoardCells
        {
            get => _boardCells;
            set
            {
                _boardCells = value;
                OnPropertyChanged(nameof(BoardCells));
            }
        }

        public bool MoveIsValid { get; private set; }
        public bool IsWhiteTurn { get; private set; }

        public int MoveCount
        {
            get => _moveCount;
            set
            {
                _moveCount = value;
                
                if(_moveCount <= _branchingMoveAtCount)
                {
                    _isBranching = false;
                }

                if (_isBranching)
                {
                    MoveIndex = -1;
                    MoveIndexSub = value - 1;
                }
                else
                {
                    MoveIndex = value - 1;
                    MoveIndexSub = -1;
                }
                OnPropertyChanged(nameof(MoveCount));
                OnPropertyChanged(nameof(MoveIndex));
                OnPropertyChanged(nameof(MoveIndexSub));
                _isNewMove = true;
                Others.PlayAudioFile("move_sound.mp3");
            }
        }

        public int MoveIndex { get; set; }
        public int MoveIndexSub { get; set; }

        private string _fenString; // Field to store the FEN string
        public string FenString
        {
           get => _fenString; // Public property to access the FEN string
           set 
           {
               if (_fenString != value)
               {
                   _fenString = value;
                   OnPropertyChanged();
               }
           }
        }

        private string _geminiStringResult; // Field to store the Gemini string
        public string GeminiStringResult
        {
            get => _geminiStringResult; // Public property to access the Gemini string
            set
            {
                if (_geminiStringResult != value)
                {
                    _geminiStringResult = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _bestMove; // Field to store the FEN string
        public string BestMove
        {
            get => _bestMove; // Public property to access the FEN string
            set
            {
                if (_bestMove != "")
                {
                    var previousMove = _bestMove;
                    _bestMove = value;
                    OnPropertyChanged(nameof(BestMove));

                    List<Point> movePoints = PiecesMoveRecord.ConvertStringToPieceIndexes(_bestMove, _whiteSide == EnumWhiteSide.Bottom);
                    // Get chess piece at the start position
                    // Update best move with correct turn and piece source color
                    var chessPieceSrc = GetChessPiece((int)movePoints[0].Y, (int)movePoints[0].X);
                    var chessPieceDst = GetChessPiece((int)movePoints[1].Y, (int)movePoints[1].X);
                    if ((chessPieceSrc.Color == EnumPieceColor.White && IsWhiteTurn == false) ||
                        (chessPieceSrc.Color == EnumPieceColor.Black && IsWhiteTurn == true))
                    {
                        _bestMove = previousMove;
                        // all piece CellWith is the same, so we can use any piece to get the width
                        movePoints = PiecesMoveRecord.ConvertStringToPieceIndexes(_bestMove, _whiteSide == EnumWhiteSide.Bottom);
                        chessPieceSrc = GetChessPiece((int)movePoints[0].Y, (int)movePoints[0].X);
                        chessPieceDst = GetChessPiece((int)movePoints[1].Y, (int)movePoints[1].X);
                    }

                    BestMoveArrowViewModel.ArrowStartX = chessPieceSrc.Center.X;
                    BestMoveArrowViewModel.ArrowStartY = chessPieceSrc.Center.Y;
                    BestMoveArrowViewModel.ArrowEndX = chessPieceDst.Center.X;
                    BestMoveArrowViewModel.ArrowEndY = chessPieceDst.Center.Y;
                    BestMoveArrowViewModel.ArrowVisible = true;

                    OnPropertyChanged(nameof(BestMoveArrowViewModel));
                }
            }
        }

        private double _boardWidth;
        public double BoardWidth
        {
            get => _boardWidth;
            private set
            {
            }
        }

        private double _whiteWinPercentage;
        public double WhiteWinPercentage
        {
            get => _whiteWinPercentage;
            set
            {
                _whiteWinPercentage = value;
                OnPropertyChanged(nameof(WhiteWinPercentage));
            }
        }

        private double _blackWinPercentage;
        public double BlackWinPercentage
        {
            get => _blackWinPercentage;
            set
            {
                _blackWinPercentage = value;
                OnPropertyChanged(nameof(BlackWinPercentage));
            }
        }

        private double _stockfishEvaluationResult;
        private string _stockfishEvaluation;

        public string StockfishEvaluation
        {
            get => _stockfishEvaluation;
            set
            {
                if (_stockfishEvaluation != value)
                {
                   _stockfishEvaluation = value;
                   OnPropertyChanged(nameof(StockfishEvaluation));
                   WhiteWinPercentage = double.TryParse(_stockfishEvaluation, out double whiteWinPercentage) ? 100 * whiteWinPercentage : 0;
                   BlackWinPercentage = 100.0 - whiteWinPercentage;
                }
            }
        }

        public Piece[,] Kings
        {
            get { return _kings; }
            set { _kings = value; }
        }

        public Piece[,] Queens
        {
            get { return _queens; }
            set { _queens = value; }
        }

        public Piece[,] Rooks
        {
            get { return _rooks; }
            set { _rooks = value; }
        }

        public Piece[,] Knights
        {
            get { return _knights; }
            set { _knights = value; }
        }

        public Piece[,] Bishops
        {
            get { return _bishops; }
            set { _bishops = value; }
        }

        public Piece[,] Pawns
        {
            get { return _pawns; }
            set { _pawns = value; }
        }
        private ObservableCollection<MoveItem> _moveList = new ObservableCollection<MoveItem>();
        public ObservableCollection<MoveItem> MoveList
        {
            get => _moveList;
            set
            {
                if (_moveList != value)
                {
                    _moveList = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<MoveItem> _moveListSub = new ObservableCollection<MoveItem>();
        public ObservableCollection<MoveItem> MoveListSub
        {
            get => _moveListSub;
            set
            {
                if (_moveListSub != value)
                {
                    _moveListSub = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<MoveItem> _pgnmoveList = new ObservableCollection<MoveItem>();
        public ObservableCollection<MoveItem> PgnMoveList
        {
            get => _pgnmoveList;
            set
            {
                if (_pgnmoveList != value)
                {
                    _pgnmoveList = value;
                    OnPropertyChanged();
                }
            }
        }

        public event EventHandler ScrollToLatestItem;

        private ArrowViewModel _bestMoveArrowViewModel = new ArrowViewModel();
        public ArrowViewModel BestMoveArrowViewModel
        {
            get => _bestMoveArrowViewModel;
            set
            {
                _bestMoveArrowViewModel = value;
                OnPropertyChanged(nameof(BestMoveArrowViewModel));
            }
        }
        #endregion

        #region Constructor
        private BoardViewModel()
        {
            AskStockFishCommand = new Command(async () => await AskStockFishCommandExecute());
            AskGoogleGeminiCommand = new Command(async () => await AskGoogleGeminiCommandExecute());

            InitializeStockfish();

            // Create a checkerboard pattern
            for (int i = 0; i < Size; i++)
            {
                var row = new ObservableCollection<Piece>();
                for (int j = 0; j < Size; j++)
                {
                    var piece = new Piece()
                    {
                        RowIdx = i,
                        ColIdx = j,
                        TopLeftNumber = (j == 0) ? (Size - i).ToString() : "",
                        IsTopLeftNumberVisible = (j == 0),
                        BottomRightLetter = (i == Size - 1) ? ((char)('a' + j)).ToString() : "",
                        IsBottomRightLetterVisible = (i == Size - 1)
                    };
                    row.Add(piece);
                }
                BoardCells.Add(row);
            }

            _boardWidth = InitializeBoard();

            IsWhiteTurn = true; // White goes first

            Device.StartTimer(TimeSpan.FromMilliseconds(500), () =>
            {
                // Code to execute on each tick
                if (AskStockFishCommand.CanExecute(null))
                {
                    AskStockFishCommand.Execute(null);
                }
                // Return true to keep the timer running, false to stop it
                return true;
            });

            Device.StartTimer(TimeSpan.FromMilliseconds(500), () =>
            {
                if (_bestMoveAvailable)
                {
                    _bestMoveAvailable = false;
                    string question2Gemini_prefix = "Let say, I have a chess board with the following pieces: \n";
                    string question2Gemini_boardInfo = AllPositionInString() + "\n";
                    string question2Gemini_suffix = $"And stockfish say that, the evaluation of current position is {_stockfishEvaluationResult} (white side), tell me about this evaluation in less than 150 words";
                    _question_for_gemnini = question2Gemini_prefix + question2Gemini_boardInfo + question2Gemini_suffix;
                    if (AskGoogleGeminiCommand.CanExecute(null))
                    {
                        AskGoogleGeminiCommand.Execute(null);
                    }
                }

                // Return true to keep the timer running, false to stop it
                return true;
            });

            MessageService.Instance.Subscribe(ProcessPgnImportText);

            MoveList.CollectionChanged += MoveList_CollectionChanged;
        }

        #endregion

        #region Private Method
        private void MoveList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                // Notify the view to scroll to the new item
                ScrollToLatestItem?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ProcessPgnImportText(string pgnText)
        {
            string moveParts = ExtractMovesFromLoadedPgn(pgnText);

            moveParts = moveParts.Replace("\n", " ").Replace("+", "");

            Debug.WriteLine($"Received PGN text: {moveParts}");

            // Split the PGN text into individual moves
            var moves = moveParts.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Reset the Board First
            NewBoardSetup();

            Device.BeginInvokeOnMainThread(() =>
            {
                // Initialize the move index
                int moveIndex = 0;

                // Iterate through the moves and create MoveItem objects
                foreach (var move in moves)
                {
                    // Skip move numbers (e.g., "1.", "2.", etc.)
                    if (move.Contains("."))
                    {
                        continue;
                    }

                    // Create a new MoveItem
                    var moveItem = new MoveItem
                    {
                        StrMove = move,
                        MoveIndex = moveIndex
                    };

                    moveItem.StrMove = moveIndex % 2 == 0 ? $"{moveItem.MoveIndex + 1}. {moveItem.StrMove}" : moveItem.StrMove;

                    // Add the MoveItem to the PgnMoveList
                    PgnMoveList.Add(moveItem);

                    // Clone the MoveItem for MoveList
                    var clonedMoveItem = new MoveItem
                    {
                        StrMove = moveItem.StrMove,
                        MoveIndex = moveItem.MoveIndex
                    };

                    // Add the cloned MoveItem to the MoveList
                    MoveList.Add(clonedMoveItem);

                    // Increment the move index
                    moveIndex++;
                }

                _chessGame = new ChessGame();
                _chessGame.ApplyMovesFromPGN(moveParts);

                //var prevfen = _chessGame.FENList[0];

                //foreach (var fen in _chessGame.FENList)
                //{
                //    _snapShots.Add(CreateBoardCellsFromFen(fen, _whiteSide == EnumWhiteSide.Bottom));
                //    UpdatePiecesFromFens(prevfen, fen, _whiteSide == EnumWhiteSide.Bottom);
                //    CreateSnapshotForPieces();
                //    prevfen = fen;
                //}

                // switch to loaded pgn move, instead of moving manually by user
                _isLoadedPgnMove = true;
            });

        }

        private void UpdatePiecesFromFens(string initialFen, string currentFen, bool isWhiteAtBottom)
        {
            var initialBoard = CreateBoardCellsFromFen(initialFen, isWhiteAtBottom);
            var currentBoard = CreateBoardCellsFromFen(currentFen, isWhiteAtBottom);

            // Iterate through the initial board and update the pieces
            for (int row = 0; row < initialBoard.Count; row++)
            {
                for (int col = 0; col < initialBoard[row].Count; col++)
                {
                    var initialPiece = initialBoard[row][col];
                    var currentPiece = currentBoard[row][col];

                    if (initialPiece != null)
                    {
                        if (currentPiece == null)
                        {
                            // Piece has been captured
                            initialPiece.IsAlive = false;
                        }
                        else if (initialPiece.Type != currentPiece.Type || initialPiece.Color != currentPiece.Color)
                        {
                            // Piece has moved or been replaced
                            initialPiece.RowIdx = currentPiece.RowIdx;
                            initialPiece.ColIdx = currentPiece.ColIdx;
                            initialPiece.HasNotMoved = false;
                        }
                    }
                    else if (currentPiece != null)
                    {
                        // New piece has been created
                        // You can handle this case if needed
                    }
                }
            }

            // Update the piece arrays (_pawns, _rooks, _knights, _bishops, _queens, _kings)
            UpdatePieces(_pawns, initialBoard);
            UpdatePieces(_rooks, initialBoard);
            UpdatePieces(_knights, initialBoard);
            UpdatePieces(_bishops, initialBoard);
            UpdatePieces(_queens, initialBoard);
            UpdatePieces(_kings, initialBoard);
        }

        private void ResetPieceToOriginalPosition()
        {
            //***************************** Initialize pawns *****************************************
            for (int i = 0; i < Size; i++)
            {
                _pawns[(int)EnumPieceColor.White, i] = new Piece(EnumPieceType.Pawn, EnumPieceColor.White) { RowIdx = 6, ColIdx = i, Index = i };
                _pawns[(int)EnumPieceColor.Black, i] = new Piece(EnumPieceType.Pawn, EnumPieceColor.Black) { RowIdx = 1, ColIdx = i, Index = i };
            }

            //****************************** Initialize rooks *****************************************
            _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Rook, EnumPieceColor.White) { RowIdx = 7, ColIdx = 0, Index = 0 };
            _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Rook, EnumPieceColor.White) { RowIdx = 7, ColIdx = 7, Index = 1 };

            _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Rook, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 0, Index = 0 };
            _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Rook, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 7, Index = 1 };

            //***************************** Initialize knights ****************************************
            _knights[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Knight, EnumPieceColor.White) { RowIdx = 7, ColIdx = 1, Index = 0 };
            _knights[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Knight, EnumPieceColor.White) { RowIdx = 7, ColIdx = 6, Index = 1 };

            _knights[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Knight, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 1, Index = 0 };
            _knights[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Knight, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 6, Index = 1 };

            //***************************** Initialize bishops ******************************************
            _bishops[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Bishop, EnumPieceColor.White) { RowIdx = 7, ColIdx = 2, Index = 0 };
            _bishops[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Bishop, EnumPieceColor.White) { RowIdx = 7, ColIdx = 5, Index = 1 };

            _bishops[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Bishop, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 2, Index = 0 };
            _bishops[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Bishop, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 5, Index = 1 };

            //******************************* Initialize queens *******************************************
            _queens[0, (int)EnumPieceColor.White] = new Piece(EnumPieceType.Queen, EnumPieceColor.White) { RowIdx = 7, ColIdx = 3 };
            _queens[0, (int)EnumPieceColor.Black] = new Piece(EnumPieceType.Queen, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 3 };

            //******************************* Initialize kings ********************************************
            _kings[0, (int)EnumPieceColor.White] = new Piece(EnumPieceType.King, EnumPieceColor.White) { RowIdx = 7, ColIdx = 4 };
            _kings[0, (int)EnumPieceColor.Black] = new Piece(EnumPieceType.King, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 4 };
        }

        private string ExtractMovesFromLoadedPgn(string pgnText)
        {
            // Find the index of the last header closing bracket
            int lastHeaderEndIndex = pgnText.LastIndexOf(']');

            if (lastHeaderEndIndex == -1)
            {
                // No headers found, return the original text or handle as error
                return pgnText;
            }

            // Extract the moves part, which starts just after the last header
            string movesPart = pgnText.Substring(lastHeaderEndIndex + 1).Trim();

            // Remove the game result if present (assuming standard results like "1-0", "0-1", "1/2-1/2")
            string[] standardResults = { "1-0", "0-1", "1/2-1/2" };
            foreach (var result in standardResults)
            {
                if (movesPart.EndsWith(result))
                {
                    movesPart = movesPart.Remove(movesPart.Length - result.Length).Trim();
                    break;
                }
            }

            return movesPart;
        }

        private List<ObservableCollection<Piece>> CreateBoardCellsFromFen(string fenString, bool isWhiteAtBottom)
        {
            List<ObservableCollection<Piece>> ret = new List<ObservableCollection<Piece>>();

            // Split the FEN string to get the board layout part
            string[] fenParts = fenString.Split(' ');
            string boardLayout = fenParts[0];

            // Split the board layout into ranks
            string[] ranks = boardLayout.Split('/');

            // Determine the starting rank based on isWhiteAtBottom
            int startRank = isWhiteAtBottom ? 0 : ranks.Length - 1;
            int rankIncrement = isWhiteAtBottom ? 1 : -1;

            // Iterate through each rank
            for (int i = 0; i < ranks.Length; i++)
            {
                int row = startRank + i * rankIncrement;
                ObservableCollection<Piece> rankPieces = new ObservableCollection<Piece>();
                string rank = ranks[i];
                int col = 0;

                // Iterate through each character in the rank
                foreach (char c in rank)
                {
                    if (char.IsDigit(c))
                    {
                        // Empty squares
                        int emptySquares = int.Parse(c.ToString());
                        for (int j = 0; j < emptySquares; j++)
                        {
                            rankPieces.Add(null); // Add null for empty squares
                            col++;
                        }
                    }
                    else
                    {
                        // Non-empty squares (pieces)
                        Piece piece = CreatePieceFromFenChar(c, row, col);
                        rankPieces.Add(piece);
                        col++;
                    }
                }

                ret.Add(rankPieces);
            }

            return ret;
        }

        private Piece CreatePieceFromFenChar(char fenChar, int row, int col)
        {
            EnumPieceColor color = char.IsUpper(fenChar) ? EnumPieceColor.White : EnumPieceColor.Black;
            fenChar = char.ToLower(fenChar);

            switch (fenChar)
            {
                case 'p':
                    return new Piece { Type = EnumPieceType.Pawn, Color = color, RowIdx = row, ColIdx = col };
                case 'r':
                    return new Piece { Type = EnumPieceType.Rook, Color = color, RowIdx = row, ColIdx = col };
                case 'n':
                    return new Piece { Type = EnumPieceType.Knight, Color = color, RowIdx = row, ColIdx = col };
                case 'b':
                    return new Piece { Type = EnumPieceType.Bishop, Color = color, RowIdx = row, ColIdx = col };
                case 'q':
                    return new Piece { Type = EnumPieceType.Queen, Color = color, RowIdx = row, ColIdx = col };
                case 'k':
                    return new Piece { Type = EnumPieceType.King, Color = color, RowIdx = row, ColIdx = col };
                default:
                    throw new ArgumentException($"Invalid FEN character: {fenChar}");
            }
        }

        // Method to initialize Stockfish in a separate thread
        private void InitializeStockfish()
        {
            new Thread(() =>
            {
                string[] args = { "" };
                Debug.WriteLine("Stockfish initialized successfully.");
                _stockfish.InitMyStockfish(args.Length, args);
                string output = _stockfish.GetOutput();
                Debug.WriteLine("Stockfish ended !!!!");
                // Perform additional initialization or UI updates after Stockfish is initialized
            }).Start();
        }

        private void SwapPieces(Piece src, Piece dest)
        {
            // Save the source piece's position
            int srcRow = src.RowIdx;
            int srcCol = src.ColIdx;

            // Update the source piece's position to the destination piece's position
            src.RowIdx = dest.RowIdx;
            src.ColIdx = dest.ColIdx;

            // Update the destination piece's position to the source piece's original position
            dest.RowIdx = srcRow;
            dest.ColIdx = srcCol;

            // Swap the pieces in the ChessPiecesForDisplaying collection
            ChessPiecesForDisplaying[src.RowIdx][src.ColIdx] = src;
            ChessPiecesForDisplaying[dest.RowIdx][dest.ColIdx] = dest;
        }

        private string PieceAndPostionToString(Piece piece)
        {
            string color =  (piece.Color == EnumPieceColor.White) ? "White" : "Black";
            string ret = $"{color} {PiecesMoveRecord.PieceType2StringFullName(piece.Type)} is at {PiecesMoveRecord.Col2String(piece.ColIdx, _whiteSide == EnumWhiteSide.Bottom)}{8 - piece.RowIdx}";
            return ret;
        }

        private string AllPositionInString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    if (ChessPiecesForDisplaying[i][j].Type != EnumPieceType.None)
                    {
                        sb.Append(PieceAndPostionToString(ChessPiecesForDisplaying[i][j]));
                        sb.Append(", ");
                    }
                }
            }
            sb.Remove(sb.Length - 2, 2);
            return sb.ToString();
        }

        private void OnInnerCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Whenever an inner collection changes, notify that the ChessPiecesForDisplaying property has changed
            OnPropertyChanged(nameof(ChessPiecesForDisplaying));
        }

        private double InitializeBoard()
        {
            ChessPiecesForDisplaying.Clear();
            // Clear the pieces on board first
            for (int i = 0; i < Size; i++)
            {
                var row = new ObservableCollection<Piece>();
                //row.CollectionChanged += OnInnerCollectionChanged;
                for (int j = 0; j < Size; j++)
                {
                    var piece = new Piece() { RowIdx = i, ColIdx = j };
                    row.Add(piece);
                }
                ChessPiecesForDisplaying.Add(row);
            }

            //***************************** Initialize pawns *****************************************
            for (int i = 0; i < Size; i++)
            {
                _pawns[(int)EnumPieceColor.White,i] = new Piece(EnumPieceType.Pawn, EnumPieceColor.White) { RowIdx = 6, ColIdx = i, Index = i };
                ChessPiecesForDisplaying[6][i].Copy(_pawns[(int)EnumPieceColor.White, i]);

                _pawns[(int)EnumPieceColor.Black, i] = new Piece(EnumPieceType.Pawn, EnumPieceColor.Black) { RowIdx = 1, ColIdx = i, Index = i };
                ChessPiecesForDisplaying[1][i].Copy(_pawns[(int)EnumPieceColor.Black, i]);
            }

            //****************************** Initialize rooks *****************************************
            _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Rook, EnumPieceColor.White) { RowIdx = 7, ColIdx = 0, Index = 0 };
            ChessPiecesForDisplaying[7][0].Copy(_rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide]);

            _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Rook, EnumPieceColor.White) { RowIdx = 7, ColIdx = 7, Index = 1 };
            ChessPiecesForDisplaying[7][7].Copy(_rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide]);

            _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Rook, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 0, Index = 0 };
            ChessPiecesForDisplaying[0][0].Copy(_rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide]);

            _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Rook, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 7, Index = 1 };
            ChessPiecesForDisplaying[0][7].Copy(_rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide]);

            //***************************** Initialize knights ****************************************
            _knights[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Knight, EnumPieceColor.White) { RowIdx = 7, ColIdx = 1, Index = 0 };
            ChessPiecesForDisplaying[7][1].Copy(_knights[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide]);

            _knights[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Knight, EnumPieceColor.White) { RowIdx = 7, ColIdx = 6, Index = 1 };
            ChessPiecesForDisplaying[7][6].Copy(_knights[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide]);

            _knights[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Knight, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 1, Index = 0 };
            ChessPiecesForDisplaying[0][1].Copy(_knights[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide]);

            _knights[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Knight, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 6, Index = 1 };
            ChessPiecesForDisplaying[0][6].Copy(_knights[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide]);

            //***************************** Initialize bishops ******************************************
            _bishops[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Bishop, EnumPieceColor.White) { RowIdx = 7, ColIdx = 2, Index = 0 };
            ChessPiecesForDisplaying[7][2].Copy(_bishops[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide]);

            _bishops[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Bishop, EnumPieceColor.White) { RowIdx = 7, ColIdx = 5 , Index = 1 };
            ChessPiecesForDisplaying[7][5].Copy(_bishops[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide]);

            _bishops[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide] = new Piece(EnumPieceType.Bishop, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 2, Index = 0 };
            ChessPiecesForDisplaying[0][2].Copy(_bishops[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide]);

            _bishops[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide] = new Piece(EnumPieceType.Bishop, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 5, Index = 1 };
            ChessPiecesForDisplaying[0][5].Copy(_bishops[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide]);

            //******************************* Initialize queens *******************************************
            _queens[0,(int)EnumPieceColor.White] = new Piece(EnumPieceType.Queen, EnumPieceColor.White) { RowIdx = 7, ColIdx = 3 };
            ChessPiecesForDisplaying[7][3].Copy(_queens[0,(int)EnumPieceColor.White]);

            _queens[0,(int)EnumPieceColor.Black] = new Piece(EnumPieceType.Queen, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 3 };
            ChessPiecesForDisplaying[0][3].Copy(_queens[0,(int)EnumPieceColor.Black]);

            //******************************* Initialize kings ********************************************
            _kings[0,(int)EnumPieceColor.White] = new Piece(EnumPieceType.King, EnumPieceColor.White) { RowIdx = 7, ColIdx = 4 };
            ChessPiecesForDisplaying[7][4].Copy(_kings[0,(int)EnumPieceColor.White]);

            _kings[0,(int)EnumPieceColor.Black] = new Piece(EnumPieceType.King, EnumPieceColor.Black) { RowIdx = 0, ColIdx = 4 };
            ChessPiecesForDisplaying[0][4].Copy(_kings[0,(int)EnumPieceColor.Black]);

            foreach (var row in ChessPiecesForDisplaying)
            {
                foreach (var piece in row)
                {
                    piece.DragCommand = new Command(() => OnPieceDragStarted(piece));
                    piece.DropCommand = new Command(() => OnPieceDropped(piece));
                    piece.TapCommand = new Command(() => OnPieceTap(piece));
                }
            }

            CreateSnapshotForPieces();

            _snapShots.Add(CreateSnapshot());

            return _kings[0, (int)EnumPieceColor.Black].ScreenWidth;
        }

        /// <summary>
        /// Creates a snapshot of the current board state as a list of ObservableCollection<Piece>.
        /// This method also updates the internal FEN string representation of the board state.
        /// </summary>
        /// <returns>A snapshot of the current board state.</returns>
        private List<ObservableCollection<Piece>> CreateSnapshot()
        {
            var snapshot = new List<ObservableCollection<Piece>>();
            StringBuilder fenBuilder = new StringBuilder();
            int emptySquares = 0;

            // Determine the iteration direction based on the board orientation
            bool isWhiteAtBottom = _whiteSide == EnumWhiteSide.Bottom;
            int startRank = isWhiteAtBottom ? 0 : ChessPiecesForDisplaying.Count - 1;
            int endRank = isWhiteAtBottom ? ChessPiecesForDisplaying.Count : -1;
            int rankStep = isWhiteAtBottom ? 1 : -1;

            for (int rank = startRank; isWhiteAtBottom ? rank < endRank : rank > endRank; rank += rankStep)
            {
                var row = ChessPiecesForDisplaying[rank];
                var newCollection = new ObservableCollection<Piece>();

                foreach (var piece in row)
                {
                    // Copy piece to snapshot
                    var newPiece = new Piece();
                    newPiece.Copy(piece);
                    newCollection.Add(newPiece);

                    // FEN generation logic
                    if (piece.Type == EnumPieceType.None)
                    {
                        emptySquares++;
                    }
                    else
                    {
                        if (emptySquares > 0)
                        {
                            fenBuilder.Append(emptySquares);
                            emptySquares = 0;
                        }
                        char pieceChar = GetFenCharForPiece(piece);
                        fenBuilder.Append(pieceChar);
                    }
                }

                if (emptySquares > 0)
                {
                    fenBuilder.Append(emptySquares);
                    emptySquares = 0;
                }

                // Add '/' between ranks, but not after the last rank
                if (isWhiteAtBottom ? rank < endRank - 1 : rank > endRank + 1)
                {
                    fenBuilder.Append('/');
                }

                snapshot.Add(newCollection);
            }

            // Append active player's turn
            fenBuilder.Append(IsWhiteTurn ? " b " : " w ");

            // Append castling rights
            StringBuilder castlingRights = new StringBuilder();
            if (CanCastle(_kings[0, (int)EnumPieceColor.White], EnumKingOrQueenSide.KingSide)) castlingRights.Append("K");
            if (CanCastle(_kings[0, (int)EnumPieceColor.White], EnumKingOrQueenSide.QueenSide)) castlingRights.Append("Q");
            if (CanCastle(_kings[0, (int)EnumPieceColor.Black], EnumKingOrQueenSide.KingSide)) castlingRights.Append("k");
            if (CanCastle(_kings[0, (int)EnumPieceColor.Black], EnumKingOrQueenSide.QueenSide)) castlingRights.Append("q");
            if (castlingRights.Length == 0) castlingRights.Append("-");
            fenBuilder.Append(castlingRights + " ");

            // Append placeholders for en passant target, halfmove clock, and fullmove number
            // You'll need to replace these with actual game state information if available
            fenBuilder.Append("- 0 ");

            fenBuilder.Append((_moveCount + 1) / 2 + 1);

            // Append other parts of the FEN string (placeholders for now)
            FenString = fenBuilder.ToString();

            return snapshot;
        }

        private char GetFenCharForPiece(Piece piece)
        {
            char pieceChar = ' ';
            switch (piece.Type)
            {
                case EnumPieceType.Pawn:
                    pieceChar = 'P';
                    break;
                case EnumPieceType.Knight:
                    pieceChar = 'N';
                    break;
                case EnumPieceType.Bishop:
                    pieceChar = 'B';
                    break;
                case EnumPieceType.Rook:
                    pieceChar = 'R';
                    break;
                case EnumPieceType.Queen:
                    pieceChar = 'Q';
                    break;
                case EnumPieceType.King:
                    pieceChar = 'K';
                    break;
                default:
                    throw new InvalidOperationException("Unknown piece type.");
            }

            return piece.Color == EnumPieceColor.Black ? char.ToLower(pieceChar) : pieceChar;
        }

        private string buildStockFishCommand(string cmd)
        {
            return cmd + "\n";
        }

        private bool IsStockFishReady()
        {
            string command2stockfish = buildStockFishCommand("isready");
            _stockfish.SetInput(command2stockfish);
            string output = _stockfish.GetOutput();
            double cnt = 0;
            while( output.Contains("readyok") == false)
            {
                output = _stockfish.GetOutput();
                cnt++;
                if(cnt > 1000)
                {
                    return false;
                }
            }
            return true;
        }

        private string AskStockFishEvaluateBoard()
        {
            string command2stockfish;
            string output;

            if (!IsStockFishReady())
            {
                return "";
            }

            command2stockfish = buildStockFishCommand("eval");
            _stockfish.SetInput(command2stockfish);

            int count = 0;

            do
            {
                output = _stockfish.GetOutput();
                if (output == "")
                {
                    // Wait for 10 ms before getting the output
                    Task.Delay(10);
                    count++;
                    if (count > 1000)
                    {
                        return "";
                    }
                }
            } while (!output.Contains("Final evaluation") && !output.Contains("mate") && !output.Contains("stalemate"));
            return output;
        }

        private string AskStockFishAbourBestMove(string fenString, int depth = 4)
        {
            string command2stockfish;
            string output;

            if (!IsStockFishReady())
            {
                return "";
            }

            command2stockfish = buildStockFishCommand("position fen " + fenString);
            _stockfish.SetInput(command2stockfish);
            Task.Delay(10);
            _stockfish.SetInput(command2stockfish);

            if (!IsStockFishReady())
            {
                return "";
            }

            command2stockfish = buildStockFishCommand("go depth " + depth.ToString());
            _stockfish.SetInput(command2stockfish);

            int count = 0;

            do
            {
                output = _stockfish.GetOutput();
                if (output == "") {
                    // Wait for 10 ms before getting the output
                    Task.Delay(10);
                    count++;
                    if (count > 1000)
                    {
                        return "";
                    }
                }
            } while (!output.Contains("bestmove"));
            return output;
        }

        private void OnPieceDropped(Piece piece)
        {
            Piece dropCell = CheckDrop(piece);
            _animateTime = 0;
            if (dropCell != null && _currentCell !=  null)
            {
                if ((dropCell.Color != _currentCell.Color) && //empty cell
                    ((dropCell.RowIdx != _currentCell.RowIdx) || (dropCell.ColIdx != _currentCell.ColIdx)) &&
                    (dropCell.CircleVisible == true))// a valid cell
                {
                    // Now, update the UI on the main thread
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        MoveCurrentPieceTo(dropCell.RowIdx, dropCell.ColIdx);
                        InvisibleAllCircle();
                        _currentCell.ImageVisible = false;
                        _currentCell = null;
                    });
                }
                else if (dropCell.CircleVisible == false)
                {
                    InvisibleAllCircle();
                    _currentCell = null;
                }
            }
        }

        private Piece CheckDrop(Piece piece)
        {
            Point dropPoint = new Point(piece.Center.X + piece.MoveInfo.TranslateX, piece.Center.Y + piece.MoveInfo.TranslateY);
            Piece dropCellRet = null;
            foreach (var row in ChessPiecesForDisplaying)
            {
                foreach (var dropCell in row)
                {
                    if (dropCell.Rectangle.Contains(dropPoint))
                    {
                        // This is the frame where the item was dropped
                        System.Diagnostics.Debug.WriteLine($"*************************** BoardViewModel Dropped on row {dropCell.RowIdx}, column {dropCell.ColIdx}");
                        dropCellRet = dropCell;
                        break;
                    }
                }
            }
            return dropCellRet;
        }

        private Piece GetPieceAt(int row, int column)
        {
            if (row >= 0 && row < Size && column >= 0 && column < Size)
                return ChessPiecesForDisplaying[row][column];
            return new Piece();
        }

        private void SetPieceAt(int row, int column, Piece piece, bool sim=false, bool force=false)
        {
            if (row >= 0 && row < Size && column >= 0 && column < Size)
            {
                if (!force)
                {
                    // if the destination cell is not empty and the piece we want to set at this cell is not None, which means this cell has an opponent's piece
                    if (!sim && piece.Type != EnumPieceType.None)
                    {
                        var chessPieceDest = GetChessPiece(row, column);
                        if (chessPieceDest != null && chessPieceDest.Type != EnumPieceType.None)
                        {
                            chessPieceDest.IsAlive = false;
                            chessPieceDest.ImageVisible = false;
                        }
                    }

                    ChessPiecesForDisplaying[row][column].CopyContent(piece);

                    if (piece.Type != EnumPieceType.None)
                    {
                        var chessPiece = GetChessPiece(piece);

                        chessPiece.CopyContent(piece);

                        if (!sim)
                        {
                            ChessPiecesForDisplaying[row][column].ImageVisible = false;
                            chessPiece.TranslateTo(row, column, _animateTime);
                            chessPiece.ImageVisible = true;
                        }

                        chessPiece.RowIdx = row;
                        chessPiece.ColIdx = column;
                    }
                }
                else
                {
                    ChessPiecesForDisplaying[row][column].CopyContent(piece);
                    ChessPiecesForDisplaying[row][column].ImageVisible = false;
                }
            }
        }

        /// <summary>
        /// Determines if the King can castle to the specified side.
        /// </summary>
        /// <param name="piece">The piece to check for castling. This should be a king.</param>
        /// <param name="side">The side to check for castling. This can be either the kingside or queenside.</param>
        /// <returns>Returns true if the piece can castle to the specified side, false otherwise.</returns>
        private bool CanCastle(Piece piece, EnumKingOrQueenSide side)
        {
            bool canCastle = false;

            // Determine the side and color based on the piece
            EnumPieceColor color = piece.Color;
            int kingRow = (color == EnumPieceColor.White && _whiteSide == EnumWhiteSide.Bottom) || (color == EnumPieceColor.Black && _whiteSide == EnumWhiteSide.Top) ? 7 : 0;

            // Check for kingside castling
            if (side == EnumKingOrQueenSide.KingSide)
            {
                if (ChessPiecesForDisplaying[kingRow][5].Type == EnumPieceType.None && ChessPiecesForDisplaying[kingRow][6].Type == EnumPieceType.None)
                {
                    bool isKingHasNotMove = KingHasNotMoved(color);
                    bool isRookHasNotMove = RookHasNotMoved(color, EnumKingOrQueenSide.KingSide);
                    if (isKingHasNotMove && isKingHasNotMove && !IsKingUnderAttack(_kings[0, (int)color]))
                    {
                        bool canCastle1 = !SimMoveAndCheckKingUnderAttack(_kings[0, (int)color], kingRow, 5);
                        canCastle = (!SimMoveAndCheckKingUnderAttack(_kings[0, (int)color], kingRow, 6)) && canCastle1 ;
                    }
                }
            }
            // Check for queenside castling
            else if (side == EnumKingOrQueenSide.QueenSide)
            {
                if (ChessPiecesForDisplaying[kingRow][1].Type == EnumPieceType.None && ChessPiecesForDisplaying[kingRow][2].Type == EnumPieceType.None && ChessPiecesForDisplaying[kingRow][3].Type == EnumPieceType.None)
                {
                    if (KingHasNotMoved(color) && RookHasNotMoved(color, EnumKingOrQueenSide.QueenSide) && !IsKingUnderAttack(_kings[0, (int)color]))
                    {
                        bool canCastle1 = !SimMoveAndCheckKingUnderAttack(_kings[0, (int)color], kingRow, 3);
                        canCastle = (!SimMoveAndCheckKingUnderAttack(_kings[0, (int)color], kingRow, 2)) & canCastle1;
                    }
                }
            }

            return canCastle;
        }

        private bool KingHasNotMoved(EnumPieceColor color)
        {
            return color == EnumPieceColor.White ? _kings[0,(int)EnumPieceColor.White].HasNotMoved : _kings[0,(int)EnumPieceColor.Black].HasNotMoved;
        }

        private bool RookHasNotMoved(EnumPieceColor color, EnumKingOrQueenSide side)
        {
            if (color == EnumPieceColor.White)
            {
                return side == EnumKingOrQueenSide.KingSide ? _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide].HasNotMoved : _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide].HasNotMoved;
            }
            else
            {
                return side == EnumKingOrQueenSide.KingSide ? _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide].HasNotMoved : _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide].HasNotMoved;
            }
        }

        private Piece GetChessPiece (Piece cell)
        {
            Piece pieceRet = null;
            Piece[,] pieceCollection = null;
            switch (cell.Type)
            {
                case EnumPieceType.Pawn:
                    pieceCollection = _pawns;
                    break;
                case EnumPieceType.Rook:
                    pieceCollection = _rooks;
                    break;
                case EnumPieceType.Knight:
                    pieceCollection = _knights;
                    break;
                case EnumPieceType.Bishop:
                    pieceCollection = _bishops;
                    break;
                case EnumPieceType.Queen:
                    pieceCollection = _queens;
                    break;
                case EnumPieceType.King:
                    pieceCollection = _kings;
                    break;
                default:
                    pieceRet = new Piece() { ColIdx = cell.ColIdx, RowIdx = cell.RowIdx };
                    break;
            }

            if (pieceRet == null)
            {
                if (cell.Type != EnumPieceType.King && cell.Type != EnumPieceType.Queen)
                {
                    for (int i = 0; i < pieceCollection.GetLength(1); i++)
                    {
                        int rowIndex = (int)cell.Color;
                        if (pieceCollection[rowIndex, i].Index == cell.Index)
                        {
                            pieceRet = pieceCollection[rowIndex, i];
                            break;
                        }
                    }
                }
                else
                {
                    pieceRet = pieceCollection[0, (int)cell.Color];
                }
            }

            return pieceRet;
        }

        private Piece GetChessPiece(int row, int column)
        {
            Piece pieceRet = null;
            Piece cellInfo = GetPieceAt(row, column);
            pieceRet = GetChessPiece(cellInfo);
            return pieceRet;
        }

        private bool IsKingUnderAttack(Piece king)
        {
            int kingRow = king.RowIdx;
            int kingCol = king.ColIdx;
            EnumPieceColor opponentColor = king.Color == EnumPieceColor.White ? EnumPieceColor.Black : EnumPieceColor.White;

            // Check all directions for rooks, bishops, and queens
            if (IsAttackedInDirection(kingRow, kingCol, 1, 0, opponentColor)) return true;  // Up
            if (IsAttackedInDirection(kingRow, kingCol, -1, 0, opponentColor)) return true; // Down
            if (IsAttackedInDirection(kingRow, kingCol, 0, 1, opponentColor)) return true;  // Right
            if (IsAttackedInDirection(kingRow, kingCol, 0, -1, opponentColor)) return true; // Left
            if (IsAttackedInDirection(kingRow, kingCol, 1, 1, opponentColor)) return true;  // Up-Right
            if (IsAttackedInDirection(kingRow, kingCol, 1, -1, opponentColor)) return true; // Up-Left
            if (IsAttackedInDirection(kingRow, kingCol, -1, 1, opponentColor)) return true; // Down-Right
            if (IsAttackedInDirection(kingRow, kingCol, -1, -1, opponentColor)) return true; // Down-Left

            // Check for knight attacks
            if (IsKnightAttacking(kingRow, kingCol, opponentColor)) return true;

            // Check for pawn attacks
            if (IsPawnAttacking(kingRow, kingCol, opponentColor)) return true;

            return false;
        }

        private bool IsAttackedInDirection(int row, int col, int rowStep, int colStep, EnumPieceColor opponentColor)
        {
            int i = row + rowStep;
            int j = col + colStep;
            while (i >= 0 && i < 8 && j >= 0 && j < 8)
            {
                Piece piece = ChessPiecesForDisplaying[i][j];
                if (piece.Type != EnumPieceType.None)
                {
                    if (piece.Color == opponentColor &&
                        (piece.Type == EnumPieceType.Rook || piece.Type == EnumPieceType.Queen ||
                        (piece.Type == EnumPieceType.Bishop && (rowStep != 0 && colStep != 0))))
                    {
                        return true;
                    }
                    break;
                }
                i += rowStep;
                j += colStep;
            }
            return false;
        }

        private bool IsKnightAttacking(int row, int col, EnumPieceColor opponentColor)
        {
            int[][] knightMoves = new int[][]
            {
                new int[] { 2, 1 }, new int[] { 2, -1 },
                new int[] { -2, 1 }, new int[] { -2, -1 },
                new int[] { 1, 2 }, new int[] { 1, -2 },
                new int[] { -1, 2 }, new int[] { -1, -2 }
            };

            foreach (var move in knightMoves)
            {
                int newRow = row + move[0];
                int newCol = col + move[1];
                if (newRow >= 0 && newRow < 8 && newCol >= 0 && newCol < 8)
                {
                    Piece piece = ChessPiecesForDisplaying[newRow][newCol];
                    if (piece.Type != EnumPieceType.None && piece.Color == opponentColor && piece.Type == EnumPieceType.Knight)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsPawnAttacking(int row, int col, EnumPieceColor opponentColor)
        {
            int rowDirection = opponentColor == EnumPieceColor.White ? 1 : -1;
            int[] pawnCols = new int[] { -1, 1 };

            foreach (var colStep in pawnCols)
            {
                int newRow = row + rowDirection;
                int newCol = col + colStep;
                if (newRow >= 0 && newRow < 8 && newCol >= 0 && newCol < 8)
                {
                    Piece piece = ChessPiecesForDisplaying[newRow][newCol];
                    if (piece.Type != EnumPieceType.None && piece.Color == opponentColor && piece.Type == EnumPieceType.Pawn)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private EnumKingOrQueenSide DoCastling(int row, int col)
        {
            EnumKingOrQueenSide ret = EnumKingOrQueenSide.None;
            if (_currentCell.Type == EnumPieceType.King)
            {
                if (_currentCell.Color == EnumPieceColor.White)
                {
                    if (row == _kings[0,(int)EnumPieceColor.White].RowIdx && col == 6)
                    {
                        SetPieceAt(row, _kings[0,(int)EnumPieceColor.White].ColIdx, new Piece());
                        SetPieceAt(row, _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide].ColIdx, new Piece());
                        SetPieceAt(_kings[0,(int)EnumPieceColor.White].RowIdx, 6, _kings[0,(int)EnumPieceColor.White]);
                        SetPieceAt(_rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide].RowIdx, 5, _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide]);
                        _kings[0,(int)EnumPieceColor.White].HasNotMoved = false;
                        _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.KingSide].HasNotMoved = false;
                        ret = EnumKingOrQueenSide.KingSide;
                    }
                    else if (row == 7 && col == 2)
                    {
                        SetPieceAt(row, _kings[0,(int)EnumPieceColor.White].ColIdx, new Piece());
                        SetPieceAt(row, _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide].ColIdx, new Piece());
                        SetPieceAt(_kings[0,(int)EnumPieceColor.White].RowIdx, 2, _kings[0,(int)EnumPieceColor.White]);
                        SetPieceAt(_rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide].RowIdx, 3, _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide]);
                        _kings[0,(int)EnumPieceColor.White].HasNotMoved = false;
                        _rooks[(int)EnumPieceColor.White, (int)EnumKingOrQueenSide.QueenSide].HasNotMoved = false;
                        ret = EnumKingOrQueenSide.QueenSide;
                    }
                }
                else
                {
                    if (row == 0 && col == 6)
                    {
                        SetPieceAt(row, _kings[0,(int)EnumPieceColor.Black].ColIdx, new Piece());
                        SetPieceAt(row, _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide].ColIdx, new Piece());

                        _kings[0, (int)EnumPieceColor.Black].HasNotMoved = false;
                        _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide].HasNotMoved = false;

                        SetPieceAt(_kings[0,(int)EnumPieceColor.Black].RowIdx, 6, _kings[0,(int)EnumPieceColor.Black]);
                        SetPieceAt(_rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide].RowIdx, 5, _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.KingSide]);

                        ret = EnumKingOrQueenSide.KingSide;
                    }
                    else if (row == 0 && col == 2)
                    {
                        SetPieceAt(row, _kings[0,(int)EnumPieceColor.Black].ColIdx, new Piece());
                        SetPieceAt(row, _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide].ColIdx, new Piece());

                        _kings[0, (int)EnumPieceColor.Black].HasNotMoved = false;
                        _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide].HasNotMoved = false;

                        SetPieceAt(_kings[0,(int)EnumPieceColor.Black].RowIdx, 2, _kings[0,(int)EnumPieceColor.Black]);
                        SetPieceAt(_rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide].RowIdx, 3, _rooks[(int)EnumPieceColor.Black, (int)EnumKingOrQueenSide.QueenSide]);
                        ret = EnumKingOrQueenSide.QueenSide;
                    }
                }
            }
            return ret;
        }

        // Move the piece by touching the piece
        private void MoveCurrentPieceTo(int row, int col, bool updateHistory = true)
        {
            if (ChessPiecesForDisplaying[row][col].CircleVisible == false && updateHistory == true)
            {
                MoveIsValid = false;
                return;
            }

            int currentRow = _currentCell.RowIdx;
            int currentCol = _currentCell.ColIdx;
            // Move the piece if valid
            Piece pieceNone = new Piece();
            _currentCell.HasNotMoved = false;

            EnumKingOrQueenSide castling = DoCastling(row, col);

            PiecesMoveRecord pieceMoveRecord = new PiecesMoveRecord(_whiteSide==EnumWhiteSide.Bottom);

            if (updateHistory)
            {
                MoveItem moveItem = new MoveItem();
                moveItem.MoveIndex = MoveCount;
                if (castling == EnumKingOrQueenSide.None)
                {
                    Piece cellDest = GetPieceAt(row, col);

                    // store the move to history
                    pieceMoveRecord.PiecePosition = new Position(row, col);
                    pieceMoveRecord.PieceTypeProp = _currentCell.Type;

                    string tmp = $"{pieceMoveRecord.PgnType} {pieceMoveRecord.PgnPostion}".ToString();

                    // This is an capture move
                    if (cellDest.Type != EnumPieceType.None)
                    {
                        string prefix = _currentCell.Type != EnumPieceType.Pawn ? pieceMoveRecord.PgnType : PiecesMoveRecord.Col2String(_currentCell.ColIdx, _whiteSide==EnumWhiteSide.Bottom);
                        tmp = $"{prefix}x{pieceMoveRecord.PgnPostion}".ToString();
                    }

                    moveItem.StrMove = tmp;

                    SetPieceAt(row, col, _currentCell);
                    SetPieceAt(currentRow, currentCol, pieceNone);

                }
                else if (castling == EnumKingOrQueenSide.KingSide)
                {
                    pieceMoveRecord = new PiecesMoveRecord(_whiteSide == EnumWhiteSide.Bottom);
                    pieceMoveRecord.Castling = EnumKingOrQueenSide.KingSide;
                    string tmp = $"{pieceMoveRecord.PgnType}".ToString();
                    moveItem.StrMove = tmp;
                }
                else
                {
                    pieceMoveRecord = new PiecesMoveRecord(_whiteSide == EnumWhiteSide.Bottom);
                    pieceMoveRecord.Castling = EnumKingOrQueenSide.QueenSide;
                    string tmp = $"{pieceMoveRecord.PgnType}".ToString();
                    moveItem.StrMove = tmp;
                }

                // Add number to label if this is white's move
                moveItem.StrMove = MoveCount % 2 == 0 ? $"{moveItem.MoveIndex/2+1}.{moveItem.StrMove}" : moveItem.StrMove;

                if (_isBranching)
                {
                    // the move is in the sub-branch
                    if(MoveCount < MoveListSub.Count)
                    {
                        MoveListSub[MoveCount] = new MoveItem()
                        {
                            StrMove = moveItem.StrMove,
                            MoveIndex = moveItem.MoveIndex,
                            IsVisibleAndClickable = true
                        };
                        _snapShotSubs[MoveCount] = CreateSnapshot();
                    } 
                    else
                    {
                        MoveListSub.Add(new MoveItem()
                        {
                            StrMove = moveItem.StrMove,
                            MoveIndex = moveItem.MoveIndex,
                            IsVisibleAndClickable = true
                        });
                        _snapShotSubs.Add(MoveCount, CreateSnapshot());
                        CreateSnapshotForPieces();
                    }
                }
                else if (MoveCount < MoveList.Count && moveItem.StrMove != MoveList[MoveCount].StrMove)
                {
                    _isBranching = true;
                    _branchingMoveAtCount = MoveCount;
                    // this is a sub-branch move
                    if (moveItem.StrMove != MoveListSub[MoveCount].StrMove)
                    {
                        MoveListSub[MoveCount] = new MoveItem()
                        {
                            StrMove = moveItem.StrMove,
                            MoveIndex = moveItem.MoveIndex,
                            IsVisibleAndClickable = true
                        };
                        _snapShotSubs.Add(MoveCount, CreateSnapshot());
                    }
                }
                else
                {
                    MoveList.Add(moveItem);
                    MoveListSub.Add(new MoveItem()
                    {
                        StrMove = moveItem.StrMove,
                        MoveIndex = moveItem.MoveIndex
                    });

                    _snapShots.Add(CreateSnapshot());
                    CreateSnapshotForPieces();
                }
            }

            MoveIsValid = true;
            IsWhiteTurn = !IsWhiteTurn; // Switch turns after a valid move

            MoveCount++;
        }

        private void VisualizePossibleMoveCurrentPiece()
        {
            // if no piece is selected, return
            if (_currentCell == null)
            {
                return;
            }

            // visualize all possible move for current piece if it is King, Pawn or Knight
            if (_currentCell.Type == EnumPieceType.Pawn || _currentCell.Type == EnumPieceType.Knight)
            {
                for (int i = 0; i < Size; i++)
                {
                    for (int j = 0; j < Size; j++)
                    {
                        if (IsValidMovePawnKnight(new Move(_currentCell.RowIdx, _currentCell.ColIdx, i, j)))
                        {
                            EnableCurrentPiecePossibleMove(i, j);
                        }
                        else
                        {
                            ChessPiecesForDisplaying[i][j].CircleVisible = false;
                        }
                    }
                }
            }
            // visualize all possible move for current piece if it is Rook
            else if (_currentCell.Type == EnumPieceType.Rook)
            {
                VisualizePossibleMoveCurrentRook();
            }
            // visualize all possible move for current piece if it is Bishop
            else if (_currentCell.Type == EnumPieceType.Bishop)
            {
                VisualizePossibleMoveCurrentBishop();
            }
            // visualize all possible move for current piece if it is Queen
            else if (_currentCell.Type == EnumPieceType.Queen)
            {
                VisualizePossibleMoveCurrentQueen();
            }
            if (_currentCell.Type == EnumPieceType.King)
            {
                VisualizePossibleMoveCurrentKing();
            }
        }

        /// <summary>
        /// Determines whether a proposed move for a given piece would protect the king.
        /// </summary>
        /// <param name="piece">The piece to check.</param>
        /// <param name="row">The row index of the proposed move.</param>
        /// <param name="col">The column index of the proposed move.</param>
        /// <returns>
        /// Returns true if the proposed move would protect the king, false otherwise.
        /// </returns>
        /// <remarks>
        /// This method works by temporarily moving the piece to the proposed position and checking if the king is under attack.
        /// If the king is not under attack with the piece at the proposed position, it means that the move would protect the king, so the method returns true.
        /// After the check, the piece is moved back to its original position.
        /// </remarks>
        private bool IsMoveProtectingKing(Piece piece, int row, int col)
        {
            bool ret = false;
            Piece backupPieceSrc = new Piece();
            Piece backupPieceDst = new Piece();
            backupPieceSrc.Copy(piece);
            backupPieceDst.Copy(GetPieceAt(row, col));

            // set the checked piece to empty, to check if it is None, whether the King is under attacked
            SetPieceAt(piece.RowIdx, piece.ColIdx, new Piece(), true);
            SetPieceAt(row, col, backupPieceSrc, true);

            if (!IsKingUnderAttack(_kings[0, (int)backupPieceSrc.Color]))
            {
                ret = true;
            }

            SetPieceAt(backupPieceSrc.RowIdx, backupPieceSrc.ColIdx, backupPieceSrc, true);
            SetPieceAt(row, col, backupPieceDst, true);

            return ret;
        }

        /// <summary>
        /// Determines whether a possible move for the current piece should be visually indicated on the board.
        /// </summary>
        /// <param name="row">The row index of the possible move.</param>
        /// <param name="col">The column index of the possible move.</param>
        /// <remarks>
        /// This method checks if the king is under attack. If the king is not under attack, it visually indicates the possible move by making the circle at the given position visible.
        /// If the king is under attack, it checks if the proposed move would protect the king. If the move would protect the king, it makes the circle at the given position visible. Otherwise, it makes the circle invisible.
        /// </remarks>
        private void EnableCurrentPiecePossibleMove(int row, int col)
        {
            // if King is not under attacked, show the circle
            if (!IsKingUnderAttack(_kings[0, (int)_currentCell.Color]))
            {
                if (!SimMoveAndCheckKingUnderAttack(_currentCell, row, col))
                {
                    ChessPiecesForDisplaying[row][col].CircleVisible = true;
                }
            }
            else
            {
                // if King is under attacked
                if (IsMoveProtectingKing(_currentCell, row, col))
                {
                    ChessPiecesForDisplaying[row][col].CircleVisible = true;
                }
                else
                {
                    ChessPiecesForDisplaying[row][col].CircleVisible = false;
                }
            }
        }

        /// <summary>
        /// Simulates a move and checks if the king of the moving piece's color would be under attack after the move.
        /// </summary>
        /// <param name="piece">The piece to move.</param>
        /// <param name="row">The row index to move the piece to.</param>
        /// <param name="col">The column index to move the piece to.</param>
        /// <returns>Returns true if the king would be under attack after the move, false otherwise.</returns>
        private bool SimMoveAndCheckKingUnderAttack(Piece piece, int row, int col)
        {
            bool ret = false;
            Piece backupPieceSrc = new Piece();
            Piece backupPieceDst = new Piece();
            backupPieceSrc.Copy(piece);
            backupPieceDst.Copy(GetPieceAt(row, col));

            // set the checked piece to empty, to check if it is None, whether the King is under attacked
            SetPieceAt(piece.RowIdx, piece.ColIdx, new Piece(), true);
            SetPieceAt(row, col, backupPieceSrc, true);

            if (IsKingUnderAttack(_kings[0, (int)backupPieceSrc.Color]))
            {
                ret = true;
            }

            SetPieceAt(backupPieceSrc.RowIdx, backupPieceSrc.ColIdx, backupPieceSrc, true);
            SetPieceAt(row, col, backupPieceDst, true);

            return ret;
        }

        private bool CanKingMoveToThisPosition(int row, int col)
        {
            bool ret = false;
            int selectedKing = IsWhiteTurn ? (int)EnumPieceColor.White : (int)EnumPieceColor.Black;
            Piece backupPieceSrc = new Piece();
            Piece backupPieceDst = new Piece();
            backupPieceSrc.Copy(_kings[0, selectedKing]);
            backupPieceDst.Copy(GetPieceAt(row, col));

            // set the checked piece to empty, to check if it is None, whether the King is under attacked
            SetPieceAt(_kings[0, selectedKing].RowIdx, _kings[0, selectedKing].ColIdx, new Piece(), true);
            SetPieceAt(row, col, _kings[0, selectedKing], true);

            if (!IsKingUnderAttack(_kings[0, (int)backupPieceSrc.Color]))
            {
                ret = true;
            }

            SetPieceAt(backupPieceSrc.RowIdx, backupPieceSrc.ColIdx, backupPieceSrc, true);
            SetPieceAt(row, col, backupPieceDst, true);

            return ret;
        }

        /// <summary>
        /// Sets the visibility of all circles in the chess board to false.
        /// </summary>
        /// <remarks>
        /// This method iterates over all the pieces on the chess board and sets the CircleVisible property to false.
        /// This is typically used to clear the visual indicators of possible moves on the chess board.
        /// </remarks>
        private void InvisibleAllCircle()
        {
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    ChessPiecesForDisplaying[i][j].CircleVisible = false;
                }
            }
        }

        /// <summary>
        /// Validates if a move for pawn, or knight is valid according to chess rules.
        /// </summary>
        /// <param name="move">The move to validate, containing start and end positions.</param>
        /// <returns>True if the move is valid, otherwise false.</returns>
        /// <remarks>
        /// This function checks the piece at the start position and validates the move based on the type of piece (King, Pawn, Knight).
        /// For pawns, the function checks for normal moves, capture moves, and initial double step moves.
        /// For knights, it validates the L-shaped moves.
        /// The function returns false if the move is not valid or if it's not the correct turn for the piece's color.
        /// </remarks>
        private bool IsValidMovePawnKnight(Move move)
        {
            bool ret = false;
            Piece piece = GetPieceAt(move.StartRow, move.StartCol);
            Piece pieceDest = GetPieceAt(move.EndRow, move.EndCol);
            if (piece != null && (IsWhiteTurn == (piece.Color == EnumPieceColor.White)))
            {
                ret = true; // valid piece and valid turn
            }

            if (ret)
            {
                ret = false;
                int rowDelta = move.EndRow - move.StartRow;
                int colDelta = move.EndCol - move.StartCol;

                switch (piece.Type)
                {
                    case EnumPieceType.Pawn:
                        // Normal move forward
                        if ((piece.Color == EnumPieceColor.White && rowDelta == (int)_whiteSide * (-1) && colDelta == 0 && pieceDest.Type == EnumPieceType.None) ||
                            (piece.Color == EnumPieceColor.Black && rowDelta == (int)_whiteSide && colDelta == 0 && pieceDest.Type == EnumPieceType.None))
                            ret = true;

                        // Capture move
                        if ((piece.Color == EnumPieceColor.White && rowDelta == (int)_whiteSide * (-1) && Math.Abs(colDelta) == 1 && pieceDest.Color == EnumPieceColor.Black) ||
                            (piece.Color == EnumPieceColor.Black && rowDelta == (int)_whiteSide && Math.Abs(colDelta) == 1 && pieceDest.Color == EnumPieceColor.White))
                            ret = true;

                        // Double step move from initial position
                        if ((piece.Color == EnumPieceColor.White && rowDelta == (int)_whiteSide * (-2) && colDelta == 0 && pieceDest.Type == EnumPieceType.None && piece.HasNotMoved) ||
                            (piece.Color == EnumPieceColor.Black && rowDelta == (int)_whiteSide * 2 && colDelta == 0 && pieceDest.Type == EnumPieceType.None && piece.HasNotMoved))
                            ret = true;

                        break;

                    case EnumPieceType.Knight:
                        ret = ((Math.Abs(rowDelta) == 2 && Math.Abs(colDelta) == 1) || (Math.Abs(rowDelta) == 1 && Math.Abs(colDelta) == 2)) &&
                               (pieceDest.Color != piece.Color);
                        break;
                    default:
                        ret = false;
                        break;
                }
            }
            return ret;
        }

        /// <summary>
        /// Visualizes possible moves for the current rook piece on the chess board.
        /// </summary>
        /// <remarks>
        /// This method iterates through the rows and columns from the current position of the rook to visualize all valid moves.
        /// It sets the CircleVisible property to true for all valid move destinations until it encounters a piece of the same color or the edge of the board.
        /// </remarks>
        private void VisualizePossibleMoveCurrentRook()
        {
            // check move through row direction from the current selected piece
            for (int step = -1; step <= 1; step += 2)
            {
                for (int i = _currentCell.RowIdx + step; i >= 0 && i < Size; i += step)
                {
                    var checkPiece = ChessPiecesForDisplaying[i][_currentCell.ColIdx];
                    if (checkPiece.Type == EnumPieceType.None || _currentCell.Color != checkPiece.Color) // empty cell or opponent's piece
                    {
                        EnableCurrentPiecePossibleMove(i, _currentCell.ColIdx);
                        if (checkPiece.Type != EnumPieceType.None)
                        {
                            break;
                        }
                    }
                    else // same color piece
                    {
                        break;
                    }
                }
            }

            // check move through column direction from the current selected piece
            for (int step = -1; step <= 1; step += 2)
            {
                for (int j = _currentCell.ColIdx + step; j >= 0 && j < Size; j += step)
                {
                    var currentPiece = ChessPiecesForDisplaying[_currentCell.RowIdx][j];
                    if (currentPiece.Type == EnumPieceType.None || _currentCell.Color != currentPiece.Color) // empty cell or opponent's piece
                    {
                        EnableCurrentPiecePossibleMove(_currentCell.RowIdx, j);
                        if (currentPiece.Type != EnumPieceType.None)
                        {
                            break;
                        }
                    }
                    else // same color piece
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Visualizes all possible diagonal moves for the currently selected bishop piece on the chess board.
        /// </summary>
        /// <remarks>
        /// This method iterates through all four diagonal directions from the current position of the bishop.
        /// It continues in each direction until it encounters the edge of the board or a piece of the same color,
        /// marking all valid move destinations by setting the CircleVisible property to true for each accessible chess square.
        /// If an opponent's piece is encountered, that position is also marked as a valid move (capture move),
        /// but the visualization stops beyond this point in that particular direction.
        /// </remarks>
        private void VisualizePossibleMoveCurrentBishop()
        {
            // check move through diagonal direction from the current selected piece
            for (int stepRow = -1; stepRow <= 1; stepRow += 2)
            {
                for (int stepCol = -1; stepCol <= 1; stepCol += 2)
                {
                    int i = _currentCell.RowIdx + stepRow;
                    int j = _currentCell.ColIdx + stepCol;
                    while (i >= 0 && i < Size && j >= 0 && j < Size)
                    {
                        var currentPiece = ChessPiecesForDisplaying[i][j];
                        if (currentPiece.Type == EnumPieceType.None || _currentCell.Color != currentPiece.Color) // empty cell or opponent's piece
                        {
                            EnableCurrentPiecePossibleMove(i, j);
                            if (currentPiece.Type != EnumPieceType.None)
                            {
                                break;
                            }
                        }
                        else // same color piece
                        {
                            break;
                        }
                        i += stepRow;
                        j += stepCol;
                    }
                }
            }
        }

        /// <summary>
        /// Visualizes all possible moves for the currently selected queen piece on the chess board.
        /// </summary>
        /// <remarks>
        /// This method combines the movement capabilities of both the rook and the bishop to visualize possible moves.
        /// It calls the visualization methods for both the rook and the bishop, reflecting the queen's ability to move
        /// in straight lines (horizontally, vertically) and diagonally without restriction, unless blocked by other pieces.
        /// </remarks>
        private void VisualizePossibleMoveCurrentQueen()
        {
            // queen is the combination of rook and bishop
            VisualizePossibleMoveCurrentRook();
            VisualizePossibleMoveCurrentBishop();
        }

        /// <summary>
        /// Visualizes possible moves for the currently selected king piece on the chess board.
        /// </summary>
        /// <remarks>
        /// This method checks for all potential king moves from the current position. The king can move one square
        /// in any direction: vertically, horizontally, or diagonally. It visualizes these moves by setting the CircleVisible
        /// property to true for all valid destinations that are either unoccupied or occupied by an opponent's piece.
        /// The method ensures not to mark the square where the king currently resides.
        /// </remarks>
        private void VisualizePossibleMoveCurrentKing()
        {
            // check move through diagonal direction and row, column direction from the current selected piece
            for (int stepRow = -1; stepRow <= 1; stepRow += 1)
            {
                for (int stepCol = -1; stepCol <= 1; stepCol += 1)
                {
                    int i = _currentCell.RowIdx + stepRow;
                    int j = _currentCell.ColIdx + stepCol;

                    if (i == stepRow && j == stepCol) // not visualize itself
                    {
                        continue;
                    }

                    if (i >= 0 && i <= 7 && j >= 0 && j <= 7 && _currentCell.Color != ChessPiecesForDisplaying[i][j].Color)
                    {
                        // check if after moving King to this position, the King is under attacked or not
                        if (CanKingMoveToThisPosition(i, j))
                        {
                            EnableCurrentPiecePossibleMove(i, j);
                        }
                    }
                }
            }

            // Castling logic
            if (_currentCell.Type == EnumPieceType.King)
            {
                int kingRow = (_currentCell.Color == EnumPieceColor.White && _whiteSide == EnumWhiteSide.Bottom) || (_currentCell.Color == EnumPieceColor.Black && _whiteSide == EnumWhiteSide.Top) ? 7 : 0;

                // Kingside castling
                if (CanCastle(_currentCell, EnumKingOrQueenSide.KingSide))
                {
                    ChessPiecesForDisplaying[kingRow][6].CircleVisible = true;
                }
                // Queenside castling
                if (CanCastle(_currentCell, EnumKingOrQueenSide.QueenSide))
                {
                    ChessPiecesForDisplaying[kingRow][2].CircleVisible = true;
                }
            }

        }

        /// <summary>
        /// Restores the state of the chess board from a snapshot.
        /// </summary>
        /// <param name="snapshot">A snapshot of the chess board. This is a two-dimensional list where each element is an ObservableCollection of Piece objects representing a row on the chess board.</param>
        /// <remarks>
        /// This method iterates over each row and column of the chess board, and sets the piece at each position to the corresponding piece from the snapshot.
        /// </remarks>
        private void RestoreFromSnapshot(int index)
        {
            var snapshot = (index >= _branchingMoveAtCount) && _isBranching ? _snapShotSubs[index] : _snapShots[index];
            StringBuilder fenBuilder = new StringBuilder();
            int emptySquares = 0;

            // this is for 64 cells of the chess board
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    SetPieceAt(i, j, snapshot[i][j], false, true);
                    // FEN generation logic for piece placement
                    if (snapshot[i][j].Type == EnumPieceType.None)
                    {
                        emptySquares++;
                    }
                    else
                    {
                        if (emptySquares > 0)
                        {
                            fenBuilder.Append(emptySquares);
                            emptySquares = 0;
                        }
                        char pieceChar = GetFenCharForPiece(snapshot[i][j]);
                        fenBuilder.Append(pieceChar);
                    }
                }

                if (emptySquares > 0)
                {
                    fenBuilder.Append(emptySquares);
                    emptySquares = 0;
                }

                if (i < Size - 1)
                {
                    fenBuilder.Append('/');
                }
            }

            // This is for chess pieces ( white/black kings, queens, rooks, bishops, knights, pawns )
            RestorePiecesFromSnapshotAt(index);

            // Append active player's turn
            fenBuilder.Append(IsWhiteTurn ? " b " : " w ");

            // Append castling rights
            StringBuilder castlingRights = new StringBuilder();
            if (CanCastle(_kings[0, (int)EnumPieceColor.White], EnumKingOrQueenSide.KingSide)) castlingRights.Append("K");
            if (CanCastle(_kings[0, (int)EnumPieceColor.White], EnumKingOrQueenSide.QueenSide)) castlingRights.Append("Q");
            if (CanCastle(_kings[0, (int)EnumPieceColor.Black], EnumKingOrQueenSide.KingSide)) castlingRights.Append("k");
            if (CanCastle(_kings[0, (int)EnumPieceColor.Black], EnumKingOrQueenSide.QueenSide)) castlingRights.Append("q");
            if (castlingRights.Length == 0) castlingRights.Append("-");
            fenBuilder.Append(castlingRights + " ");

            // Append placeholders for en passant target, halfmove clock, and fullmove number
            // You'll need to replace these with actual game state information if available
            fenBuilder.Append("- 0 ");

            fenBuilder.Append((_moveCount + 1) / 2 + 1);

            // Append other parts of the FEN string (placeholders for now)
            FenString = fenBuilder.ToString();

        }

        private async Task AskStockFishCommandExecute()
        {
            if(_fenString == null)
            {
                return;
            }

            // Assuming _fenString is already set
            string output = AskStockFishAbourBestMove(_fenString);

            var outputArr = output.Split(' ');
            string bestMoveStr = "";

            Debug.WriteLine($"Best Move {output} from Stockfish");

            if (outputArr.Length >= 2)
            {
                bestMoveStr = outputArr[1].Replace("\n", "");
            }

            if (bestMoveStr != "")
            {
                BestMove = bestMoveStr;

                // simulate a next move as the bestmove from Stockfish
                AskStockFishAbourBestMove(_fenString + $" moves {bestMoveStr}");

                // evaluate the Board after the best move by stockfish
                var outputEval = AskStockFishEvaluateBoard();

                var outputEvalArr = outputEval.Split(' ');

                //string StockfishEvaluationPre = StockfishEvaluation;

                if (outputEvalArr.Length >= 9)
                {
                    bool canConvertToDouble = double.TryParse(outputEvalArr[8], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result);
                    if (canConvertToDouble)
                    {
                        _stockfishEvaluationResult = result;
                        StockfishEvaluation = Utils.CentipawnToWinProbability(result).ToString();
                        if (_isNewMove)
                        {
                            _isNewMove = false;
                            _bestMoveAvailable = true;
                        }
                    }
                }
            }

        }

        private async Task AskGoogleGeminiCommandExecute()
        {
            if (_question_for_gemnini != "") { 
                var gemini = GeminiApiClient.Instance;
                try
                {
                    var result = await gemini.GenerateContentAsync(_question_for_gemnini);
                    GeminiStringResult = result;
                    // Display the result in a label or any other UI element
                    Debug.WriteLine(result);
                }
                catch (Exception ex)
                {
                    // Handle the error appropriately
                    Debug.WriteLine("Error", ex.Message, "OK");
                }
            }
        }

        private Piece[,] CreateSnapShotForPiece(Piece[,] pieces)
        {
            var snapshot = new Piece[pieces.GetLength(0), pieces.GetLength(1)];
            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(1); j++)
                {
                    snapshot[i, j] = new Piece();
                    snapshot[i, j].Copy(pieces[i, j]);
                }
            }
            return snapshot;
        }

        private void CreateSnapshotForPieces()
        {
            if (_isBranching)
            {
                _kingsSnapshotSub.Add(CreateSnapShotForPiece(Kings));
                _queensSnapshotSub.Add(CreateSnapShotForPiece(Queens));
                _rooksSnapshotSub.Add(CreateSnapShotForPiece(Rooks));
                _knightsSnapshotSub.Add(CreateSnapShotForPiece(Knights));
                _bishopsSnapshotSub.Add(CreateSnapShotForPiece(Bishops));
                _pawnsSnapshotSub.Add(CreateSnapShotForPiece(Pawns));
            }
            else
            {
                _kingsSnapshot.Add(CreateSnapShotForPiece(Kings));
                _queensSnapshot.Add(CreateSnapShotForPiece(Queens));
                _rooksSnapshot.Add(CreateSnapShotForPiece(Rooks));
                _knightsSnapshot.Add(CreateSnapShotForPiece(Knights));
                _bishopsSnapshot.Add(CreateSnapShotForPiece(Bishops));
                _pawnsSnapshot.Add(CreateSnapShotForPiece(Pawns));
            }
        }

        private void ClearSnapshotsForPieces()
        {
            _kingsSnapshot.Clear();
            _queensSnapshot.Clear();
            _rooksSnapshot.Clear();
            _knightsSnapshot.Clear();
            _bishopsSnapshot.Clear();
            _pawnsSnapshot.Clear();

            _kingsSnapshotSub.Clear();
            _queensSnapshotSub.Clear();
            _rooksSnapshotSub.Clear();
            _knightsSnapshotSub.Clear();
            _bishopsSnapshotSub.Clear();
            _pawnsSnapshotSub.Clear();
        }

        private void RestorePiecesFromSnapshotAt(int index)
        {
            var kingsSnapshot = _isBranching ? _kingsSnapshotSub : _kingsSnapshot;
            var queensSnapshot = _isBranching ? _queensSnapshotSub : _queensSnapshot;
            var rooksSnapshot = _isBranching ? _rooksSnapshotSub : _rooksSnapshot;
            var bishopsSnapshot = _isBranching ? _bishopsSnapshotSub : _bishopsSnapshot;
            var knightsSnapshot = _isBranching ? _knightsSnapshotSub : _knightsSnapshot;
            var pawnsSnapshot = _isBranching ? _pawnsSnapshotSub : _pawnsSnapshot;

            // Ensure the index is within the bounds of the snapshot lists
            if (index < 0 || index >= kingsSnapshot.Count || index >= queensSnapshot.Count ||
                index >= rooksSnapshot.Count || index >= knightsSnapshot.Count ||
                index >= bishopsSnapshot.Count || index >= pawnsSnapshot.Count)
            {
                Debug.WriteLine("Index out of range for restoring snapshots.");
                return;
            }

            _animateTime = 200;

            // Restore the state of each piece array from its snapshot
            MovePieces(_kings, kingsSnapshot[index]);
            MovePieces(_queens, queensSnapshot[index]);
            MovePieces(_rooks, rooksSnapshot[index]);
            MovePieces(_knights, knightsSnapshot[index]);
            MovePieces(_bishops, bishopsSnapshot[index]);
            MovePieces(_pawns, pawnsSnapshot[index]);
        }

        /// <summary>
        /// Moves pieces from a source 2D array to a destination 2D array.
        /// </summary>
        /// <remarks>
        /// This method iterates over each element in the source and destination arrays. For each piece in the source array,
        /// it performs a translation to the position specified by the corresponding piece in the destination array,
        /// updates the piece's alive status and visibility, and sets its new row and column indices.
        /// 
        /// It's assumed that both source and destination arrays have the same dimensions and contain non-null pieces
        /// at corresponding positions. The method includes basic null checks for source and destination pieces.
        /// 
        /// Note: If the TranslateTo method is asynchronous, this implementation does not await the animation completion
        /// due to the method's void return type. Consider modifying the method to async and awaiting the TranslateTo call
        /// if animation completion needs to be awaited.
        /// </remarks>
        /// <param name="src">The source 2D array of pieces to move from.</param>
        /// <param name="dest">The destination 2D array of pieces to move to.</param>
        private void MovePieces(Piece[,] src, Piece[,] dest)
        {
            int rows = src.GetLength(0); // Number of rows in the 2D array
            int cols = src.GetLength(1); // Number of columns in the 2D array

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (src[i, j] != null && dest[i, j] != null)
                    {
                        // Assuming TranslateTo is an asynchronous method but not awaited here due to the method signature
                        src[i, j].TranslateTo(dest[i, j].RowIdx, dest[i, j].ColIdx, (uint)_animateTime);
                        src[i, j].IsAlive = dest[i, j].IsAlive;
                        src[i, j].ImageVisible = dest[i, j].IsAlive;
                        src[i, j].RowIdx = dest[i, j].RowIdx;
                        src[i, j].ColIdx = dest[i, j].ColIdx;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the properties of the pieces in the given piece array based on the board representation.
        /// </summary>
        /// <remarks>
        /// This method iterates over each element in the piece array and updates the properties of each piece
        /// based on the corresponding piece in the board representation.
        /// </remarks>
        /// <param name="pieceArray">The array of pieces to update.</param>
        /// <param name="board">The board representation to use for updating the pieces.</param>
        private void UpdatePieces(Piece[,] pieceArray, List<ObservableCollection<Piece>> board)
        {
            for (int i = 0; i < pieceArray.GetLength(0); i++)
            {
                for (int j = 0; j < pieceArray.GetLength(1); j++)
                {
                    var piece = pieceArray[i, j];
                    if (piece != null)
                    {
                        var boardPiece = board[piece.RowIdx][piece.ColIdx];
                        if (boardPiece != null && boardPiece.Type == piece.Type && boardPiece.Color == piece.Color)
                        {
                            piece.RowIdx = boardPiece.RowIdx;
                            piece.ColIdx = boardPiece.ColIdx;
                            piece.IsAlive = true;
                        }
                        else
                        {
                            piece.IsAlive = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Flips the given chess piece array to mirror the board's layout, simulating a view from the opposite side.
        /// This method creates a temporary array representing the flipped state of the board, where each piece's
        /// position is inverted relative to the center of the board. The MovePieces method is then used to apply
        /// this flipped state, updating the positions and properties of the pieces accordingly.
        /// </summary>
        /// <param name="pieces">The 2D array of Piece objects representing the chess pieces on the board.</param>
        /// <remarks>
        /// The flipping process involves calculating the new positions for each piece as if the board were viewed
        /// from the opposite side. This is achieved by reversing the indices of each piece in the array. The MovePieces
        /// method is utilized to animate the transition of pieces from their original positions to their new, flipped positions.
        /// It's important to ensure that the Piece objects in the destination array are properly initialized with the correct
        /// properties before calling MovePieces, as this method relies on these properties to perform the move and update operations.
        /// </remarks>
        private void FlipPieceArray(Piece[,] pieces)
        {
            int row = pieces.GetLength(0);
            int col = pieces.GetLength(1);
            Piece[,] flipped = new Piece[row, col];

            // Prepare the flipped state in a temporary array
            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    if (pieces[i, j] != null)
                    {
                        flipped[i, j] = new Piece
                        {
                            // Copy or transform the properties of pieces[i, j] as needed
                            // For example:
                            RowIdx = 7 - pieces[i,j].RowIdx,
                            ColIdx = 7 - pieces[i,j].ColIdx,
                            IsAlive = pieces[i, j].IsAlive,
                            // Continue copying or transforming other necessary properties
                        };
                    }
                }
            }

            MovePieces(pieces, flipped);
        }

        private void FlipPieceSnapshotArray(Piece[,] pieces)
        {
            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(1); j++)
                {
                    pieces[i,j].RowIdx = 7 - pieces[i,j].RowIdx;
                    pieces[i,j].ColIdx = 7 - pieces[i,j].ColIdx;
                }
            }
        }

        private void FlipSnapshot()
        {
            for (int i = 0; i  < _snapShots.Count; i++)
            {
                FlipPieceSnapshotArray(_pawnsSnapshot[i]);
                FlipPieceSnapshotArray(_rooksSnapshot[i]);
                FlipPieceSnapshotArray(_knightsSnapshot[i]);
                FlipPieceSnapshotArray(_bishopsSnapshot[i]);
                FlipPieceSnapshotArray(_queensSnapshot[i]);
                FlipPieceSnapshotArray(_kingsSnapshot[i]);

                FlipPieceSnapshotArray(_pawnsSnapshotSub[i]);
                FlipPieceSnapshotArray(_rooksSnapshotSub[i]);
                FlipPieceSnapshotArray(_knightsSnapshotSub[i]);
                FlipPieceSnapshotArray(_bishopsSnapshotSub[i]);
                FlipPieceSnapshotArray(_queensSnapshotSub[i]);
                FlipPieceSnapshotArray(_kingsSnapshotSub[i]);

                for (int j = 0; j < Size; j++)
                {
                    for(int k=0; k<Size; k++)
                    {
                        _snapShots[i][j][k].RowIdx = 7 - _snapShots[i][j][k].RowIdx;
                        _snapShots[i][j][k].ColIdx = 7 - _snapShots[i][j][k].ColIdx;
                    }
                }
            }
        }
        #endregion

        #region public Commands
        public ICommand AskStockFishCommand { get; private set; }
        public ICommand AskGoogleGeminiCommand { get; private set; }
        public ICommand ReconstructHistoryMoves => new Command<int>(index =>
        {
            Debug.WriteLine($"History move label {index} was clicked");
            Device.StartTimer(TimeSpan.FromMilliseconds(10), () =>
            {
                bool ret = true;
                // Code to execute on each tick
                if (MoveCount == index + 1)
                {
                    ret = false;
                }
                else
                {
                    if (MoveCount < index + 1)
                    {
                        NextMove();
                    }
                    else
                    {
                        PreviousMove();
                    }
                }
                // Return true to keep the timer running, false to stop it
                return ret;
            });
        });
        public ICommand ReconstructHistorySubMoves => new Command<int>(index =>
        {
            Debug.WriteLine($"History move label {index} was clicked");
            // Add more code here to handle the label click
        });
        #endregion

        #region Public Method
        /// <summary>
        /// Handles tap events on chess pieces, either selecting a piece or attempting a move.
        /// </summary>
        /// <param name="cell">The piece that was tapped.</param>
        /// <remarks>
        /// This method is responsible for two primary actions based on the state of _currentCell:
        /// 1. If _currentCell is null or the tapped piece has the same color, it checks if it's the correct turn for the piece's color,
        ///    selects the piece, and visualizes possible moves.
        /// 2. If a different piece is already selected (_currentCell is not null), it checks if the tap is on a valid destination cell.
        ///    If valid, it moves the piece; if not, it clears the current selection and visual indicators.
        /// This function ensures that only valid moves according to game rules and turn order are allowed.
        /// </remarks>
        public void OnPieceTap(Piece cell)
        {
            _animateTime = 200;
            if (_currentCell == null || (_currentCell.Color == cell.Color) ) // first tab, select the piece
            {
                if (cell.Type != EnumPieceType.None &&
                    (cell.Color == EnumPieceColor.White && IsWhiteTurn==true)||
                    (cell.Color == EnumPieceColor.Black && IsWhiteTurn==false)
                )
                {
                    _currentCell = cell;
                    _currentCell.ImageVisible = false;
                    _currentPiece = GetChessPiece(cell);
                    _currentPiece.ImageVisible = true;
                    // Remove all Circle before visualize new possible move
                    InvisibleAllCircle();
                    VisualizePossibleMoveCurrentPiece();
                }
            }
            else // second tab, move the piece to valid cell or not if the cell is not valid
            {
                // check if this is a valid tap
                // opponent cell or empty cell
                if ((cell.Color != _currentCell.Color) && 
                    // not the same cell
                    ((cell.RowIdx != _currentCell.RowIdx) || (cell.ColIdx != _currentCell.ColIdx)) &&
                    // a valid cell
                    (cell.CircleVisible == true) )
                {
                    //MovePiece(new Move(_currentCell.RowIdx, _currentCell.ColIdx, piece.RowIdx, piece.ColIdx));
                    MoveCurrentPieceTo(cell.RowIdx, cell.ColIdx);
                    InvisibleAllCircle();
                    _currentCell = null;
                }
                else if(cell.CircleVisible == false)
                {
                    InvisibleAllCircle();
                    _currentCell = null;
                }
            }
        }
        /// <summary>
        /// Initiates the drag action for a selected piece if it is a valid move according to the game's turn and rules.
        /// </summary>
        /// <param name="cell">The piece that the drag action started on.</param>
        /// <remarks>
        /// This method checks if the piece is not of type None and whether it's the correct turn for the piece's color.
        /// If valid, it sets the piece as the current piece, clears any existing move visualizations, and visualizes possible moves for the current piece.
        /// This prepares the game for a potential piece movement following the drag.
        /// </remarks>
        public void OnPieceDragStarted(Piece cell)
        {
            _currentCell = cell;
            _currentCell.ImageVisible = true;
            _currentPiece = GetChessPiece(cell);
            _currentPiece.ImageVisible = false;
            if (cell.Type != EnumPieceType.None &&
                (cell.Color == EnumPieceColor.White && IsWhiteTurn == true) ||
                (cell.Color == EnumPieceColor.Black && IsWhiteTurn == false)
            )
            {
                InvisibleAllCircle();
                VisualizePossibleMoveCurrentPiece();
            }
        }
        /// <summary>
        /// Flips the board, swapping pieces between the top and bottom to simulate a board rotation.
        /// </summary>
        /// <remarks>
        /// This method iterates over half the rows and all columns of the chess board,
        /// swapping each piece with its corresponding piece on the opposite side of the board.
        /// This function is useful for changing perspectives, especially in two-player games or to simulate a board turn.
        /// </remarks>
        public void FlipBoard()
        {
            _animateTime = 0;
            _whiteSide = _whiteSide == EnumWhiteSide.Bottom ? EnumWhiteSide.Top : EnumWhiteSide.Bottom;

            bool isWhiteAtBottom = _whiteSide == EnumWhiteSide.Bottom;

            for (int i = 0; i < BoardCells.Count; i++)
            {
                for (int j = 0; j < BoardCells[i].Count; j++)
                {
                    var piece = BoardCells[i][j];
                    if (isWhiteAtBottom)
                    {
                        // White at the bottom
                        piece.TopLeftNumber = (j == 0) ? (Size - i).ToString() : "";
                        piece.BottomRightLetter = (i == Size - 1) ? ((char)('a' + j)).ToString() : "";
                    }
                    else
                    {
                        // White at the top
                        piece.TopLeftNumber = (j == 0) ? (i + 1).ToString() : "";
                        piece.BottomRightLetter = (i == Size - 1) ? ((char)('h' - j)).ToString() : "";
                    }
                    piece.IsTopLeftNumberVisible = (j == 0);
                    piece.IsBottomRightLetterVisible = (i == Size - 1);

                    // Update the piece in the ObservableCollection if necessary
                    BoardCells[i][j] = piece;
                }
            }

            // Flip each piece array
            FlipPieceArray(_pawns);
            FlipPieceArray(_rooks);
            FlipPieceArray(_knights);
            FlipPieceArray(_bishops);
            FlipPieceArray(_queens);
            FlipPieceArray(_kings);

            var flipped = new List<ObservableCollection<Piece>>(Size);

            // Reverse the order of rows to flip the board
            for (int i = Size - 1; i >= 0; i--)
            {
                var row = new ObservableCollection<Piece>();
                for (int j = 0; j < Size; j++)
                {
                    var flipPiece  = new Piece();
                    flipPiece.CopyContent(ChessPiecesForDisplaying[i][Size - 1 - j]);
                    // Also need to flip the pieces in each row to maintain left-right order
                    row.Add(flipPiece);
                }
                flipped.Add(row);
            }

            // Reverse the order of rows to flip the board
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    SetPieceAt(i, j, flipped[i][j], false, true);
                }
            }

            FlipSnapshot();

            InvisibleAllCircle();

        }

        public void PreviousMove()
        {
            if (MoveCount > 0)
            {
                MoveCount--;

                RestoreFromSnapshot(MoveCount);
                IsWhiteTurn = !IsWhiteTurn;
            }
        }

        public void NextMove()
        {
            int limitCounter = _isBranching ? _branchingMoveAtCount + _snapShotSubs.Count - 1 : _snapShots.Count - 1;

            if (MoveCount < limitCounter)
            {
                MoveCount++;

                RestoreFromSnapshot(MoveCount);
                IsWhiteTurn = !IsWhiteTurn;
            }
        }

        /// <summary>
        /// Reset board layout, moves, move snapshots, ... to start a new game
        /// </summary>
        public void NewBoardSetup()
        {
            // Flip to the original side before starting a new game
            if(_whiteSide == EnumWhiteSide.Top)
            {
                FlipBoard();
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                RestoreFromSnapshot(0);
                _fenString = null;
                _bestMove = "";
                _stockfishEvaluationResult = 0.0;
                _stockfishEvaluation = "";
                MoveCount = 0;
                WhiteWinPercentage = 0.0;
                BlackWinPercentage = 0.0;
                GeminiStringResult = "";
                // Remove all moves after the current move from MoveList and SnapShots
                MoveList.Clear();
                _snapShots.Clear();
                ClearSnapshotsForPieces();

                for (int i = 0; i < BoardCells.Count; i++)
                {
                    for (int j = 0; j < BoardCells[i].Count; j++)
                    {
                        var piece = BoardCells[i][j];
                        // White at the bottom
                        piece.TopLeftNumber = (j == 0) ? (Size - i).ToString() : "";
                        piece.BottomRightLetter = (i == Size - 1) ? ((char)('a' + j)).ToString() : "";
                        piece.IsTopLeftNumberVisible = (j == 0);
                        piece.IsBottomRightLetterVisible = (i == Size - 1);

                        // Update the piece in the ObservableCollection if necessary
                        BoardCells[i][j] = piece;
                    }
                }

                IsWhiteTurn = true;

                _snapShots.Add(CreateSnapshot());

                CreateSnapshotForPieces();
            });
        }

        public static BoardViewModel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BoardViewModel();
                }
                return _instance;
            }
        }
        #endregion
    }
}
