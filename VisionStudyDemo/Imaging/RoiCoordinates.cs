using System.Windows;
using System.Windows.Media.Imaging;

namespace VisionStudyDemo.Imaging;

/// <summary>仅做坐标换算：WPF 显示位置 ↔ 原图像素。X 是列方向，Y 是行方向。</summary>
public static class RoiCoordinates
{
    public static Rect GetImageBounds(BitmapSource image, Size viewport)
    {
        double scale = Math.Min(viewport.Width / image.Width, viewport.Height / image.Height);
        double width = image.Width * scale;
        double height = image.Height * scale;
        return new Rect((viewport.Width - width) / 2, (viewport.Height - height) / 2, width, height);
    }

    public static Point Clamp(Point point, Rect bounds) => new(
        Math.Clamp(point.X, bounds.Left, bounds.Right),
        Math.Clamp(point.Y, bounds.Top, bounds.Bottom));

    public static Int32Rect ToPixels(Rect selection, Rect bounds, int width, int height)
    {
        // 左上向下取整，右下向上取整，使选中的像素完整包含在 ROI 内。
        int left = Math.Clamp((int)Math.Floor((selection.Left - bounds.Left) * width / bounds.Width), 0, width);
        int top = Math.Clamp((int)Math.Floor((selection.Top - bounds.Top) * height / bounds.Height), 0, height);
        int right = Math.Clamp((int)Math.Ceiling((selection.Right - bounds.Left) * width / bounds.Width), left, width);
        int bottom = Math.Clamp((int)Math.Ceiling((selection.Bottom - bounds.Top) * height / bounds.Height), top, height);
        return new Int32Rect(left, top, right - left, bottom - top);
    }

    public static Rect ToDisplay(Int32Rect roi, Rect bounds, int width, int height) => new(
        bounds.Left + roi.X * bounds.Width / width,
        bounds.Top + roi.Y * bounds.Height / height,
        roi.Width * bounds.Width / width,
        roi.Height * bounds.Height / height);
}
