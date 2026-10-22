using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BimTools.SpxExporter.Infrastructure
{
    internal static class IconFactory
    {
        public static BitmapSource Create(string glyph, Color background, int size)
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                double radius = size * 0.18;
                Brush fill = new SolidColorBrush(background);
                fill.Freeze();
                dc.DrawRoundedRectangle(fill, null, new Rect(0, 0, size, size), radius, radius);

                double fontSize = glyph.Length > 2 ? size * 0.34 : size * 0.46;
                FormattedText text = new FormattedText(
                    glyph,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    fontSize,
                    Brushes.White,
                    1.0);
                dc.DrawText(text, new Point((size - text.Width) / 2.0, (size - text.Height) / 2.0));
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }
    }
}
