using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace GeminiChessAnalysis.Models
{
    public enum EnumPieceType { Pawn, Knight, Bishop, Rook, Queen, King, None }
    public enum EnumPieceColor { White, Black, None }

    public static class PieceBackgroundColor
    {
        public static Color SelectedColor = Color.LightGreen;
        public static Color UnselectedColor = Color.Transparent;
    }

    public class Piece : BaseModel
    {
        #region constant
        private const double ImageScaleNormalDisplay = 2;
        #endregion

        #region Properties
        private EnumPieceType _type;
        private EnumPieceColor _color;
        private string _imagePath;
        private double _imageScale;
        private int _rowIdx;
        private int _colIdx;
        private bool _setOriginalCenter=false;
        private Point _originalCenter;

        public EnumPieceType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public EnumPieceColor Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public string ImagePath
        {
            get => _imagePath;
            set { _imagePath = value; OnPropertyChanged(); }
        }
        public int RowIdx
        {
            get => _rowIdx;
            set
            {
                if (_rowIdx != value)
                {
                    _rowIdx = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LayoutBounds));
                }
            }
        }
        public int ColIdx
        {
            get => _colIdx;
            set
            {
                if (_colIdx != value)
                {
                    _colIdx = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LayoutBounds));
                }
            }
        }
        public bool HasNotMoved { get; set; }

        public int CellColorIndex
        {
            get { return (RowIdx + ColIdx) % 2; }
        }

        public double ImageScale
        {
            get => _imageScale;
            set { _imageScale = value; OnPropertyChanged(); }
        }

        private double _cellWidth;
        public double CellWidth
        {
            get => _cellWidth;
            set
            {
                _cellWidth = value;
                OnPropertyChanged(nameof(CellWidth));
            }
        }

        private double _cellHeight;
        public double CellHeight
        {
            get => _cellHeight;
            set
            {
                _cellHeight = value;
                OnPropertyChanged(nameof(CellHeight));
            }
        }

        private double _cellSize;
        public double CellSize
        {
            get => _cellSize;
            set
            {
                _cellSize = value;
                OnPropertyChanged(nameof(CellSize));
                OnPropertyChanged(nameof(LayoutBounds));
            }
        }

        private Color _backgroundColor;
        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                _backgroundColor = value;
                OnPropertyChanged(nameof(BackgroundColor));
            }
        }

        private bool _circleVisible;
        public bool CircleVisible
        {
            get => _circleVisible;
            set
            {
                _circleVisible = value;
                OnPropertyChanged(nameof(CircleVisible));
            }
        }

        private bool _imageVisible = true;
        public bool ImageVisible
        {
            get => _imageVisible;
            set
            {
                _imageVisible = value;
                OnPropertyChanged(nameof(ImageVisible));
            }
        }

        private double CalculateHeightSize()
        {
            var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            double height = mainDisplayInfo.Height / mainDisplayInfo.Density;

            // Calculate cell size based on the smaller dimension
            double cellHeight = height / 8;
            return cellHeight;
        }

        private double CalculateWidthSize()
        {
            var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
            double width = mainDisplayInfo.Width / mainDisplayInfo.Density;

            // Calculate cell size based on the smaller dimension
            double cellWidth = width / 8;
            return cellWidth;
        }

        public double ScreenWidth
        {
            get
            {
                var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
                return mainDisplayInfo.Width / mainDisplayInfo.Density;
            }
        }

        public double ScreenHeight
        {
            get
            {
                var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
                return mainDisplayInfo.Height / mainDisplayInfo.Density;
            }
        }

        private MovePixel _moveInfo;
        public MovePixel MoveInfo
        {
            get => _moveInfo;
            set
            {
                if (_moveInfo != value)
                {
                    _moveInfo = value;
                    OnPropertyChanged(nameof(MoveInfo));
                }
            }
        }

        private MovePixelWithTime _translation;
        public MovePixelWithTime Translation
        {
            get => _translation;
            set
            {
                if (_translation != value)
                {
                    _translation = value;
                    OnPropertyChanged(nameof(Translation));
                }
            }
        }

        public Rectangle Rectangle
        {
            get => new Rectangle(ColIdx * CellWidth, RowIdx * CellWidth, CellWidth, CellWidth);
        }
        public Point Center
        {
            get => (new Rectangle(ColIdx * CellWidth, RowIdx * CellWidth, CellWidth, CellWidth)).Center;
        }

        private string _topLeftNumber;
        public string TopLeftNumber
        {
            get => _topLeftNumber;
            set
            {
                if (_topLeftNumber != value)
                {
                    _topLeftNumber = value;
                    OnPropertyChanged(nameof(TopLeftNumber));
                }
            }
        }

        private bool _isTopLeftNumberVisible;
        public bool IsTopLeftNumberVisible
        {
            get => _isTopLeftNumberVisible;
            set
            {
                if (_isTopLeftNumberVisible != value)
                {
                    _isTopLeftNumberVisible = value;
                    OnPropertyChanged(nameof(IsTopLeftNumberVisible));
                }
            }
        }

        private string _bottomRightLetter;
        public string BottomRightLetter
        {
            get => _bottomRightLetter;
            set
            {
                if (_bottomRightLetter != value)
                {
                    _bottomRightLetter = value;
                    OnPropertyChanged(nameof(BottomRightLetter));
                }
            }
        }

        private bool _isBottomRightLetterVisible;
        public bool IsBottomRightLetterVisible
        {
            get => _isBottomRightLetterVisible;
            set
            {
                if (_isBottomRightLetterVisible != value)
                {
                    _isBottomRightLetterVisible = value;
                    OnPropertyChanged(nameof(IsBottomRightLetterVisible));
                }
            }
        }

        public Rectangle LayoutBounds => new Rectangle(ColIdx * CellWidth, RowIdx * CellWidth, CellWidth, CellWidth);

        public bool IsAlive { get; set; } = true;

        public int Index { get; set; } = 0;

        #endregion

        #region RelayCommand
        public ICommand DragCommand { get; set; }
        public ICommand DropCommand { get; set; }
        public ICommand TapCommand { get; set; }
        #endregion

        #region Constructor
        public Piece(EnumPieceType type = EnumPieceType.None, EnumPieceColor color = EnumPieceColor.None)
        {
            Type = type;
            Color = color;
            BackgroundColor = PieceBackgroundColor.UnselectedColor;
            ImagePath = GetImagePath(type, color);
            ImageScale = ImageScaleNormalDisplay * DeviceDisplay.MainDisplayInfo.Density;
            CellWidth = CalculateWidthSize();
            CellHeight = CalculateHeightSize();
            CellSize = Math.Min(CellWidth, CellHeight)/ ImageScale;
            HasNotMoved = true;
            MoveInfo = new MovePixel();
            Translation = new MovePixelWithTime();
        }
        #endregion

        #region Private Method

        private string GetImagePath(EnumPieceType type, EnumPieceColor color)
        {
            string prefix = color == EnumPieceColor.White ? "white_" : "black_";
            string path = "";

            switch (type)
            {
                case EnumPieceType.Pawn:
                    path = $"{prefix}pawn.png";
                    break;
                case EnumPieceType.Knight:
                    path = $"{prefix}knight.png";
                    break;
                case EnumPieceType.Bishop:
                    path = $"{prefix}bishop.png";
                    break;
                case EnumPieceType.Rook:
                    path = $"{prefix}rook.png";
                    break;
                case EnumPieceType.Queen:
                    path = $"{prefix}queen.png";
                    break;
                case EnumPieceType.King:
                    path = $"{prefix}king.png";
                    break;
                case EnumPieceType.None:
                    path = "";
                    break;
            }

            return path;
        }

        #endregion

        #region Public Method


        public void Copy(Piece other)
        {
            Type = other.Type;
            Color = other.Color;
            BackgroundColor = other.BackgroundColor;
            ImagePath = other.ImagePath;
            ImageScale = other.ImageScale;
            CellWidth = other.CellWidth;
            CellHeight = other.CellHeight;
            CellSize = other.CellSize;
            Index = other.Index;
            MoveInfo.TranslateX = other.MoveInfo.TranslateX;
            MoveInfo.TranslateY = other.MoveInfo.TranslateY;
            HasNotMoved = other.HasNotMoved;
            RowIdx = other.RowIdx;
            ColIdx = other.ColIdx;
            IsAlive = other.IsAlive;
        }

        public void CopyContent(Piece other)
        {
            Type = other.Type;
            Color = other.Color;
            ImagePath = other.ImagePath;
            ImageScale = other.ImageScale;
            HasNotMoved = other.HasNotMoved;
            Index = other.Index;
            IsAlive = other.IsAlive;
        }

        public void TranslateTo(int row, int column, double time)
        {
            if (_setOriginalCenter == false)
            {
                _originalCenter = new Rectangle(ColIdx * CellWidth, RowIdx * CellWidth, CellWidth, CellWidth).Center;
                _setOriginalCenter = true;
            }
            Point newCenter = new Rectangle(column * CellWidth, row * CellWidth, CellWidth, CellWidth).Center;
            double delta_x = newCenter.X - _originalCenter.X;
            double delta_y = newCenter.Y - _originalCenter.Y;
            Translation = new MovePixelWithTime { TranslateX = delta_x, TranslateY = delta_y, Duration = TimeSpan.FromMilliseconds(time) };
        }
        #endregion
    }
}
