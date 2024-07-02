using GeminiChessAnalysis.Models;
using GeminiChessAnalysis.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GeminiChessAnalysis.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BoardView : ContentView
    {
        public BoardView()
        {
            InitializeComponent();
            this.BindingContext = BoardViewModel.Instance;
            InitializeChessBoardGUI();
        }
        private void InitializeChessBoardGUI()
        {
            var viewModel = BindingContext as BoardViewModel;
            if (viewModel == null)
                return;

            CreateAndOverlaySpecificPieceViews(viewModel.Queens);
            CreateAndOverlaySpecificPieceViews(viewModel.Kings);
            CreateAndOverlaySpecificPieceViews(viewModel.Rooks);
            CreateAndOverlaySpecificPieceViews(viewModel.Knights);
            CreateAndOverlaySpecificPieceViews(viewModel.Bishops);
            CreateAndOverlaySpecificPieceViews(viewModel.Pawns);

            foreach (var row in viewModel.ChessPiecesForDisplaying)
            {
                foreach (var piece in row)
                {
                    var pieceView = new PieceView
                    {
                        BindingContext = piece,
                    };

                    pieceView.SetBinding(PieceView.ImageSourceProperty, "ImagePath");
                    pieceView.SetBinding(PieceView.DragCommandProperty, "DragCommand");
                    pieceView.SetBinding(PieceView.DropCommandProperty, "DropCommand");
                    pieceView.SetBinding(PieceView.TapCommandProperty, "TapCommand");
                    pieceView.SetBinding(PieceView.ImageScaleProperty, "ImageScale");
                    pieceView.SetBinding(PieceView.BackgroundColorProperty, "BackgroundColor");
                    pieceView.SetBinding(PieceView.CircleVisibleProperty, "CircleVisible");
                    pieceView.SetBinding(PieceView.MoveInfoProperty, "MoveInfo");

                    AbsoluteLayout.SetLayoutBounds(pieceView, piece.LayoutBounds);
                    AbsoluteLayout.SetLayoutFlags(pieceView, AbsoluteLayoutFlags.None);

                    BoardViewAbsoluteLayout.Children.Add(pieceView);

                    var cellView = new Frame
                    {
                        HeightRequest = piece.CellWidth,
                        WidthRequest = piece.CellWidth,
                        BackgroundColor = piece.CellColorIndex % 2 == 0 ? Color.White : Color.DarkGreen,
                    };
                    AbsoluteLayout.SetLayoutBounds(cellView, piece.LayoutBounds);
                    AbsoluteLayout.SetLayoutFlags(cellView, AbsoluteLayoutFlags.None);

                    BoardViewAbsoluteLayout.Children.Add(cellView);

                    var cell = viewModel.BoardCells[piece.RowIdx][piece.ColIdx];

                    var topLeftNumberLabel = new Label
                    {
                        FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                        TextColor = Color.Black,
                        IsVisible = cell.IsTopLeftNumberVisible
                    };
                    // Bind the Text property of the label to the TopLeftNumber property of the cell
                    topLeftNumberLabel.SetBinding(Label.TextProperty, new Binding("TopLeftNumber", source: cell));
                    AbsoluteLayout.SetLayoutBounds(topLeftNumberLabel, piece.LayoutBounds);
                    AbsoluteLayout.SetLayoutFlags(topLeftNumberLabel, AbsoluteLayoutFlags.None);
                    BoardViewAbsoluteLayout.Children.Add(topLeftNumberLabel);

                    var bottomRightLetterLabel = new Label
                    {
                        FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                        TextColor = Color.Black,
                        IsVisible = cell.IsBottomRightLetterVisible
                    };
                    // Bind the Text property of the label to the BottomRightLetter property of the cell
                    bottomRightLetterLabel.SetBinding(Label.TextProperty, new Binding("BottomRightLetter", source: cell));
                    AbsoluteLayout.SetLayoutBounds(bottomRightLetterLabel, new Rectangle (piece.LayoutBounds.X + 0.8 * cell.CellWidth,
                        piece.LayoutBounds.Y,
                        piece.LayoutBounds.Width,
                        piece.LayoutBounds.Height));
                    AbsoluteLayout.SetLayoutFlags(bottomRightLetterLabel, AbsoluteLayoutFlags.None);
                    BoardViewAbsoluteLayout.Children.Add(bottomRightLetterLabel);

                    BoardViewAbsoluteLayout.LowerChild(bottomRightLetterLabel);
                    BoardViewAbsoluteLayout.LowerChild(topLeftNumberLabel);
                    BoardViewAbsoluteLayout.LowerChild(cellView);
                }
            }
        }

        private void CreateAndOverlaySpecificPieceViews(Piece[,] pieces)
        {
            for (int i = 0; i < pieces.GetLength(0); i++)
            {
                for (int j = 0; j < pieces.GetLength(1); j++)
                {
                    var piece = pieces[i, j];
                    if (piece == null) continue; // Skip null pieces

                    var pieceView = new PieceView
                    {
                        BindingContext = piece,
                    };

                    // Assuming PieceView has a property for setting the image source directly
                    pieceView.ImageSource = piece.ImagePath;

                    pieceView.SetBinding(PieceView.ImageVisibleProperty, "ImageVisible");
                    pieceView.SetBinding(PieceView.TranslationRequestProperty, "Translation");

                    // Set the layout bounds based on the piece's row and column indices
                    AbsoluteLayout.SetLayoutBounds(pieceView, piece.LayoutBounds);
                    AbsoluteLayout.SetLayoutFlags(pieceView, AbsoluteLayoutFlags.None);

                    // Add the piece view to the board, overlaying any existing piece at the same position
                    BoardViewAbsoluteLayout.Children.Add(pieceView);
                    // Optionally, bring the newly added piece view to the front if needed
                    BoardViewAbsoluteLayout.RaiseChild(pieceView);
                }
            }
        }

    }
}