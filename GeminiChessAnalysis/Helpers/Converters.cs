using GeminiChessAnalysis.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace GeminiChessAnalysis.Helpers
{
    public class IndexMatchToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int moveIndex && parameter is int currentIndex)
            {
                return moveIndex == currentIndex;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PercentageToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double width = 0;
            var totalWidth = BoardViewModel.Instance.BoardWidth;
            if (value is double percentage)
            {
                width = (percentage / 100.0) * totalWidth;
            }
            return width;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Utils
    {
        public static double CentipawnToWinProbability(double eval, double k=0.5)
        {
            return 1 / (1 + Math.Exp(-k * eval));
        }
    }

}
