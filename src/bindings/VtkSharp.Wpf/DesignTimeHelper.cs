using System.Windows;
using System.Windows.Media;

namespace VtkSharp.Wpf;

internal static class DesignTimeHelper
{
    public static void DrawDesignTimeContent(DrawingContext drawingContext, double width, double height)
    {
        drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30)), null, new Rect(0, 0, width, height));
        drawingContext.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(0x5C, 0x5C, 0x60)), 1), new Rect(0, 0, width, height));

        if (width < 60 || height < 40) return;

        // VTK icon placeholder — a simple triangle/cone shape in the center
        var iconSize = Math.Min(width, height) * 0.25;
        var centerX = width / 2;
        var centerY = height / 2 - iconSize * 0.15;
        var iconBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x6B, 0x70));

        var iconPen = new Pen(iconBrush, 1.5);
        drawingContext.DrawEllipse(iconBrush, null, new Point(centerX, centerY - iconSize * 0.35), iconSize * 0.25, iconSize * 0.25);
        drawingContext.DrawLine(iconPen, new Point(centerX, centerY - iconSize * 0.35), new Point(centerX - iconSize * 0.45, centerY + iconSize * 0.5));
        drawingContext.DrawLine(iconPen, new Point(centerX, centerY - iconSize * 0.35), new Point(centerX + iconSize * 0.45, centerY + iconSize * 0.5));
        drawingContext.DrawLine(iconPen, new Point(centerX - iconSize * 0.45, centerY + iconSize * 0.5), new Point(centerX + iconSize * 0.45, centerY + iconSize * 0.5));
    }
}