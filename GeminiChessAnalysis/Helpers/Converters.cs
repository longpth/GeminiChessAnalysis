using GeminiChessAnalysis.ViewModels;
using System;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Shapes;
using Rectangle = Xamarin.Forms.Rectangle;
public class ArrowHeadPathConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ArrowViewModel arrowViewModel)
        {
            // Calculate the position and orientation of the arrowhead
            // This is a simplified example. You'll need to adjust the calculations
            // based on the arrow's end position and rotation.
            var endX = arrowViewModel.ArrowEndX;
            var endY = arrowViewModel.ArrowEndY;
            var size = 10; // Size of the arrowhead

            var points = new PointCollection
            {
                new Point(endX, endY),
                new Point(endX - size, endY - size),
                new Point(endX + size, endY - size)
            };

            // Create a PathGeometry for the arrowhead
            var pathFigure = new PathFigure { StartPoint = points[0] };
            pathFigure.Segments.Add(new LineSegment { Point = points[1] });
            pathFigure.Segments.Add(new LineSegment { Point = points[2] });
            pathFigure.Segments.Add(new LineSegment { Point = points[0] });

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);

            return pathGeometry;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
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

    public class ArrowPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ArrowViewModel arrowViewModel && arrowViewModel.ArrowVisible)
            {
                var startX = arrowViewModel.ArrowStartX;
                var startY = arrowViewModel.ArrowStartY;
                var endX = arrowViewModel.ArrowEndX;
                var endY = arrowViewModel.ArrowEndY;

                var length = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
                // Calculate the width of the arrow based on your design. This example uses a fixed width of 2.
                return new Rectangle(startX, startY, length, 10);
            }
            return Rectangle.Zero;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ArrowRotationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ArrowViewModel arrowViewModel)
            {
                var startX = arrowViewModel.ArrowStartX;
                var startY = arrowViewModel.ArrowStartY;
                var endX = arrowViewModel.ArrowEndX;
                var endY = arrowViewModel.ArrowEndY;

                var angleRadians = Math.Atan2(endY - startY, endX - startX);
                var angleDegrees = angleRadians * (180 / Math.PI);

                return angleDegrees;
            }
            return 0; // Default rotation
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ArrowHeadPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ArrowViewModel arrowViewModel)
            {
                var startX = arrowViewModel.ArrowStartX;
                var startY = arrowViewModel.ArrowStartY;
                var endX = arrowViewModel.ArrowEndX;
                var endY = arrowViewModel.ArrowEndY;
                var size = 20; // Size of the arrowhead

                // Calculate the angle of the arrow
                var angleRadians = Math.Atan2(endY - startY, endX - startX);

                // Calculate the points for the arrowhead based on the angle
                // Adjust these calculations to get the correct orientation and size of the arrowhead
                var point1 = new Point(endX, endY);
                var point2 = new Point(endX - size * Math.Cos(angleRadians - Math.PI / 6), endY - size * Math.Sin(angleRadians - Math.PI / 6));
                var point3 = new Point(endX - size * Math.Cos(angleRadians + Math.PI / 6), endY - size * Math.Sin(angleRadians + Math.PI / 6));

                var points = new PointCollection { point1, point2, point3 };

                // Create a PathGeometry for the arrowhead
                var pathFigure = new PathFigure { StartPoint = point1 };
                pathFigure.Segments.Add(new LineSegment { Point = point2 });
                pathFigure.Segments.Add(new LineSegment { Point = point3 });
                pathFigure.Segments.Add(new LineSegment { Point = point1 });

                var pathGeometry = new PathGeometry();
                pathGeometry.Figures.Add(pathFigure);

                return pathGeometry;
            }
            return null;
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
