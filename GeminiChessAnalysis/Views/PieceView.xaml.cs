using GeminiChessAnalysis.Models;
using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GeminiChessAnalysis.Views
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PieceView : Frame
    {
        private const double DragScaleFactor = 2;

        private double _scale_org = 4.0;

        private Point _initialTouch;

        public static readonly BindableProperty ImageSourceProperty =
            BindableProperty.Create(
                nameof(ImageSource),
                typeof(ImageSource),
                typeof(PieceView),
                default(ImageSource),
                propertyChanged: OnImageSourceChanged);
        public ImageSource ImageSource
        {
            get => (ImageSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public static readonly BindableProperty ImageScaleProperty =
            BindableProperty.Create(
                nameof(ImageScale), // Property name
                typeof(double),   // Property type
                typeof(PieceView), // Declaring type
                defaultValue: 1.0);  // Default value

        public double ImageScale
        {
            get { return (double)GetValue(ImageScaleProperty); }
            set { SetValue(ImageScaleProperty, value); }
        }

        public static readonly BindableProperty CircleVisibleProperty =
                BindableProperty.Create(
                nameof(CircleVisible),
                typeof(bool),
                typeof(PieceView),
                defaultValue: true,
                propertyChanged: OnCircleVisibleChanged);

        public bool CircleVisible
        {
            get { return (bool)GetValue(CircleVisibleProperty); }
            set { SetValue(CircleVisibleProperty, value); }
        }

        public static readonly BindableProperty ImageVisibleProperty =
        BindableProperty.Create(
        nameof(ImageVisible),
        typeof(bool),
        typeof(PieceView),
        defaultValue: true);

        public bool ImageVisible
        {
            get { return (bool)GetValue(ImageVisibleProperty); }
            set { SetValue(ImageVisibleProperty, value); }
        }

        public static readonly BindableProperty MoveInfoProperty =
        BindableProperty.Create(
        nameof(MoveInfo),
        typeof(MovePixel),
        typeof(PieceView),
        default(MovePixel));

        public MovePixel MoveInfo
        {
            get { return (MovePixel)GetValue(MoveInfoProperty); }
            set { SetValue(MoveInfoProperty, value); }
        }

        public static readonly BindableProperty TranslationRequestProperty =
        BindableProperty.Create(
        nameof(TranslationRequest),
        typeof(MovePixelWithTime),
        typeof(PieceView),
        default(MovePixelWithTime),
        propertyChanged: OnTranslationRequestChanged);

        public MovePixelWithTime TranslationRequest
        {
            get { return (MovePixelWithTime)GetValue(TranslationRequestProperty); }
            set { SetValue(TranslationRequestProperty, value); }
        }

        public static readonly BindableProperty DragCommandProperty =
        BindableProperty.Create(
        nameof(DragCommand),
        typeof(ICommand),
        typeof(PieceView),
        default(ICommand));

        public ICommand DragCommand
        {
            get => (ICommand)GetValue(DragCommandProperty);
            set => SetValue(DragCommandProperty, value);
        }

        public static readonly BindableProperty DropCommandProperty =
            BindableProperty.Create(
            nameof(DropCommand),
            typeof(ICommand),
            typeof(PieceView),
            default(ICommand));

        public ICommand DropCommand
        {
            get => (ICommand)GetValue(DropCommandProperty);
            set => SetValue(DropCommandProperty, value);
        }

        public static readonly BindableProperty TapCommandProperty =
            BindableProperty.Create(
            nameof(TapCommand),
            typeof(ICommand),
            typeof(PieceView),
            default(ICommand));

        public ICommand TapCommand
        {
            get => (ICommand)GetValue(TapCommandProperty);
            set => SetValue(TapCommandProperty, value);
        }

        public PieceView()
        {
            InitializeComponent();
            PanGestureRecognizer = new PanGestureRecognizer
            {
                TouchPoints = 1
            };
            PanGestureRecognizer.PanUpdated += PanGestureRecognizer_PanUpdated;
            GestureRecognizers.Add(PanGestureRecognizer);
        }

        public PanGestureRecognizer PanGestureRecognizer { get; set; }

        private void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (ImageSource != null && !ImageSource.IsEmpty)
            {
                AbsoluteLayout _absoluteLayout = GetParentAbsoluteLayout();

                switch (e.StatusType)
                {
                    case GestureStatus.Started:
                        // Save the initial touch point
                        _initialTouch = new Point(TranslationX, TranslationY);
                        // resize the image with some scaling factor
                        _scale_org = this.ChessPieceImage.Scale;
                        this.ChessPieceImage.Scale = DragScaleFactor * _scale_org;
                        if (_absoluteLayout != null)
                        {
                            _absoluteLayout.RaiseChild(this);
                        }
                        //Execute the drag command
                        if (DragCommand != null && DragCommand.CanExecute(null))
                        {
                            DragCommand.Execute(null);
                        }
                        break;
                    case GestureStatus.Running:
                        // Move the view
                        TranslationX = (Device.RuntimePlatform == Device.Android ? TranslationX : 0) + e.TotalX;
                        TranslationY = (Device.RuntimePlatform == Device.Android ? TranslationY : 0) + e.TotalY;
                        MoveInfo.TranslateX = TranslationX;
                        MoveInfo.TranslateY = TranslationY;
                        break;
                    case GestureStatus.Completed:
                        // Execute the drop command
                        // resize the image to the original scale
                        this.ChessPieceImage.Scale = _scale_org;
                        //Execute the drag command
                        if (DropCommand != null && DropCommand.CanExecute(null))
                        {
                            DropCommand.Execute(null);
                        }
                        this.TranslateTo(0, 0, 50);
                        break;
                    case GestureStatus.Canceled:
                        // Execute the drop command
                        // resize the image to the original scale
                        this.ChessPieceImage.Scale = _scale_org;
                        this.TranslateTo(0, 0, 200);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        private AbsoluteLayout GetParentAbsoluteLayout()
        {
            Element parent = this.Parent;
            while (parent != null && !(parent is AbsoluteLayout))
            {
                parent = parent.Parent;
            }
            return parent as AbsoluteLayout;
        }

        private static void OnImageSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (PieceView)bindable;
            control.ChessPieceImage.Source = (ImageSource)newValue;
        }

        private static void OnCircleVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = (PieceView)bindable;
            control.Circle.IsVisible = (bool)newValue;
        }

        private static async void OnTranslationRequestChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var translateRequest = (MovePixelWithTime)newValue;
            var control = (PieceView)bindable;
            await control.TranslateTo(translateRequest.TranslateX, translateRequest.TranslateY, (uint)translateRequest.Duration.TotalMilliseconds, Easing.CubicOut);
        }
    }
}