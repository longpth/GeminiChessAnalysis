using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GeminiChessAnalysis.ViewModels
{
    public class ArrowViewModel : BaseViewModel
    {
        private bool _arrowVisible;
        private double _arrowStartX;
        private double _arrowStartY;
        private double _arrowEndX;
        private double _arrowEndY;

        public bool ArrowVisible
        {
            get => _arrowVisible;
            set
            {
                _arrowVisible = value;
                OnPropertyChanged(nameof(ArrowVisible));
            }
        }

        public double ArrowStartX
        {
            get => _arrowStartX;
            set
            {
                _arrowStartX = value;
                OnPropertyChanged(nameof(ArrowStartX));
            }
        }

        public double ArrowStartY
        {
            get => _arrowStartY;
            set
            {
                _arrowStartY = value;
                OnPropertyChanged(nameof(ArrowStartY));
            }
        }

        public double ArrowEndX
        {
            get => _arrowEndX;
            set
            {
                _arrowEndX = value;
                OnPropertyChanged(nameof(ArrowEndX));
            }
        }

        public double ArrowEndY
        {
            get => _arrowEndY;
            set
            {
                _arrowEndY = value;
                OnPropertyChanged(nameof(ArrowEndY));
            }
        }

        // Method to update arrow position and visibility
        public void UpdateArrowPosition(double startX, double startY, double endX, double endY, bool visible)
        {
            ArrowStartX = startX;
            ArrowStartY = startY;
            ArrowEndX = endX;
            ArrowEndY = endY;
            ArrowVisible = visible;
        }

        // Method to clear the arrow
        public void ClearArrow()
        {
            ArrowVisible = false;
        }
    }
}