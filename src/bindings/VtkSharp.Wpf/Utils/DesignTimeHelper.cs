using System.Windows;
using System.Windows.Media;

namespace VtkSharp.Wpf.Utils;

internal static class DesignTimeHelper
{
    public static void DrawDesignTimePlaceholder(DrawingContext drawingContext, double width, double height)
    {
        if (width <= 0 || height <= 0) return;

        drawingContext.DrawRectangle(Brushes.SlateGray, null, new Rect(0, 0, width, height));
        drawingContext.DrawLine(new Pen(Brushes.Red, 1), new Point(0, 0), new Point(width, height));
        drawingContext.DrawLine(new Pen(Brushes.Green, 1), new Point(0, height), new Point(width, 0));
    }
}