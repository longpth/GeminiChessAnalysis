using GeminiChessAnalysis.Helpers;
using GeminiChessAnalysis.Models;
using GeminiChessAnalysis.Views;
using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace GeminiChessAnalysis.ViewModels
{
    public class GameViewModel : BaseViewModel
    {
        public ICommand NewGameCommand { get; private set; }
        public ICommand FlipSideCommand { get; private set; }
        public ICommand PreviousMoveCommand { get; private set; }
        public ICommand NextMoveCommand { get; private set; }
        public ICommand LoadFenCommand { get; private set; }

        public BoardViewModel BoardViewModel
        {
            get { return _boardViewModel; }
            private set { }
        }

        private BoardViewModel _boardViewModel;

        public GameViewModel()
        {
            NewGameCommand      = new RelayCommand(StartNewGame);
            FlipSideCommand     = new RelayCommand(FlipSide);
            PreviousMoveCommand = new RelayCommand(PreviousMove);
            NextMoveCommand     = new RelayCommand(NextMove);
            LoadFenCommand      = new RelayCommand(LoadFen);
            _boardViewModel     = BoardViewModel.Instance;

            //string initialFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            //string pgn = "1. e4 d5 2. exd5 Qxd5 3. Nc3 Qd8 4. Nf3";

            //var chessGame = new ChessGame(initialFEN);
            //chessGame.ApplyMovesFromPGN(pgn);

        }

        private void StartNewGame(object parameter)
        {
            _boardViewModel.NewBoardSetup();
        }

        private void FlipSide(object parameter)
        {
            if(_boardViewModel != null)
            {
                _boardViewModel.FlipBoard();
            }
        }

        private void PreviousMove(object parameter)
        {
            if (_boardViewModel != null)
            {
                _boardViewModel.PreviousMove();
            }
        }

        private void NextMove(object parameter)
        {
            if (_boardViewModel != null)
            {
                _boardViewModel.NextMove();
            }
        }

        private void LoadFen(object parameter)
        {
            if (_boardViewModel != null)
            {
                _boardViewModel.LoadFen();
            }
        }
    }
}