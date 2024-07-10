using GeminiChessAnalysis.Models;
using GeminiChessAnalysis.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Point = Xamarin.Forms.Point;
using Color = Xamarin.Forms.Color;
using System.ComponentModel;
using GeminiChessAnalysis.Services;

namespace GeminiChessAnalysis.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BoardView : ContentView
    {
        private SKCanvasView ArrowCanvas;
        public BoardView()
        {
            InitializeComponent();
            
            InitializeCanvas();
            
            this.BindingContextChanged += OnBindingContextChanged;
            
            this.BindingContext = BoardViewModel.Instance;
            
            InitializeChessBoardGUI();
            
            BoardViewModel.Instance.ScrollToLatestItem += BoardViewModel_ScrollToLatestMoveItem;
            MessageService.Instance.Subscribe(OnScrollToItemRequested);
        }

        private void OnScrollToItemRequested(string message)
        {
            if (message == MessageKeys.ScrollToFirst)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    var delayTask = Task.Delay(500); // Create a delay task
                    delayTask.ContinueWith(t =>
                    {
                        moveScrollView.ScrollToAsync(0, 0, true); // Scrolls to the top
                    }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensure continuation runs on UI thread
                });
            }
            else
            {
                string[] moveInfo = message.Split(',');
                if (moveInfo[0] == "MoveCount")
                {
                    int moveIndex = int.Parse(moveInfo[1]) - 1;
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        var delayTask = Task.Delay(500); // Create a delay task
                        delayTask.ContinueWith(t =>
                        {
                            moveScrollView.ScrollToAsync(moveStackLayout.Children.ElementAt(moveIndex), ScrollToPosition.MakeVisible, true); // Scrolls to the top
                        }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensure continuation runs on UI thread
                    });
                }
            }
        }

        private void InitializeCanvas()
        {
            ArrowCanvas = new SKCanvasView();
            AbsoluteLayout.SetLayoutBounds(ArrowCanvas, new Rectangle(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(ArrowCanvas, AbsoluteLayoutFlags.All);
            ArrowCanvas.PaintSurface += OnCanvasViewPaintSurface; // Subscribe to the PaintSurface event
            ArrowCanvas.BackgroundColor = Color.Transparent;

            // Add the ArrowCanvas to the BoardViewAbsoluteLayout
            BoardViewAbsoluteLayout.Children.Add(ArrowCanvas);
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
                    AbsoluteLayout.SetLayoutBounds(bottomRightLetterLabel, new Xamarin.Forms.Rectangle (piece.LayoutBounds.X + 0.8 * cell.CellWidth,
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
            BoardViewAbsoluteLayout.RaiseChild(ArrowCanvas);
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

        private async void BoardViewModel_ScrollToLatestMoveItem(object sender, EventArgs e)
        {
            await Task.Delay(50); // Delay to ensure layout updates

            // Assuming MoveIndex is accessible here and is the index of the item you want to scroll to
            var viewModel = BindingContext as BoardViewModel;
            if (viewModel == null) return;

            int moveIndex = viewModel.MoveIndex; // Get the MoveIndex from your ViewModel

            // Ensure the index is within the bounds of the children collection
            if (moveStackLayout.Children.Count > moveIndex && moveIndex >= 0)
            {
                var targetItem = moveStackLayout.Children.ElementAt(moveIndex);
                if (targetItem != null)
                {
                    await moveScrollView.ScrollToAsync(targetItem, ScrollToPosition.MakeVisible, true);
                }
            }
        }

        private void OnBindingContextChanged(object sender, EventArgs e)
        {
            if (BindingContext is BoardViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
                // Assuming BestMoveArrowViewModel also notifies its changes
                viewModel.BestMoveArrowViewModel.PropertyChanged += BestMoveArrowViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BoardViewModel.BestMoveArrowViewModel))
            {
                ArrowCanvas.InvalidateSurface();
            }
        }

        private void BestMoveArrowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ArrowCanvas.InvalidateSurface();
        }

        private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            // Assuming the BindingContext of BoardView is set to BoardViewModel
            var viewModel = BindingContext as BoardViewModel; // Adjust the type to your actual ViewModel
            if (viewModel?.BestMoveArrowViewModel?.ArrowVisible == true)
            {
                var arrowViewModel = viewModel.BestMoveArrowViewModel;
                DrawArrow(canvas, new Point(arrowViewModel.ArrowStartX, arrowViewModel.ArrowStartY), new Point(arrowViewModel.ArrowEndX, arrowViewModel.ArrowEndY));
            }
        }

        private void DrawArrow(SKCanvas canvas, Point startPoint, Point endPoint)
        {
            // Convert Points to SKPoint
            var start = new SKPoint((float)startPoint.X, (float)startPoint.Y);
            var end = new SKPoint((float)endPoint.X, (float)endPoint.Y);

            // Convert Points to SKPoint
            var scale = ArrowCanvas.CanvasSize.Width / (float)ArrowCanvas.Width;
            var scaledStart = new SKPoint(start.X * scale, start.Y * scale);
            var scaledEnd = new SKPoint(end.X * scale, end.Y * scale);

            // Draw arrow line
            using (var paint = new SKPaint { Color = SKColors.Navy, StrokeWidth = 10 })
            {
                canvas.DrawLine(scaledStart, scaledEnd, paint);
            }

            // Calculate and draw arrow head
            var headSize = 30;
            var angle = Math.Atan2(scaledEnd.Y - scaledStart.Y, scaledEnd.X - scaledStart.X);

            // Calculate the new starting point for the arrowhead, offset outside the original endpoint
            var delta = 10; // Distance to offset the arrowhead start point from the arrow line's end point
            var arrowHeadStartX = scaledEnd.X + delta * (float)Math.Cos(angle);
            var arrowHeadStartY = scaledEnd.Y + delta * (float)Math.Sin(angle);

            var arrowHead = new SKPath();
            arrowHead.MoveTo(arrowHeadStartX, arrowHeadStartY); // Start drawing the arrowhead from the new offset point
            arrowHead.LineTo(arrowHeadStartX - headSize * (float)Math.Cos(angle - Math.PI / 6), arrowHeadStartY - headSize * (float)Math.Sin(angle - Math.PI / 6));
            arrowHead.LineTo(arrowHeadStartX - headSize * (float)Math.Cos(angle + Math.PI / 6), arrowHeadStartY - headSize * (float)Math.Sin(angle + Math.PI / 6));
            arrowHead.Close();

            using (var paint = new SKPaint { Color = SKColors.Navy, IsAntialias = true })
            {
                canvas.DrawPath(arrowHead, paint);
            }
        }
    }
}