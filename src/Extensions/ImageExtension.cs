#pragma warning disable 1591

using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;

namespace Sanakan.Extensions
{
    public static class ImageExtension
    {
        public static Stream ToJpgStream<T>(this Image<T> img) where T : struct, IPixel<T>
        {
            var stream = new MemoryStream();
            img.SaveAsJpeg(stream, new JpegEncoder() { Quality = 93 });
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static Stream ToPngStream<T>(this Image<T> img) where T : struct, IPixel<T>
        {
            var stream = new MemoryStream();
            img.SaveAsPng(stream, new PngEncoder());
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static Image<Rgba32> CloneAndRound(this Image<Rgba32> img, float radius)
        {
            var roundedImage = img.Clone();
            roundedImage.Round(radius);
            return roundedImage;
        }

        public static void Round(this Image<Rgba32> img, float radius)
        {
            var gOptions = new GraphicsOptions(true) { AlphaCompositionMode = PixelAlphaCompositionMode.DestOut };
            img.Mutate(x => x.Fill(gOptions, Rgba32.Black, BuildCorners(img.Width, img.Height, radius)));
        }

        private static IPathCollection BuildCorners(int imageWidth, int imageHeight, float cornerRadius)
        {
            var rect = new RectangularPolygon(-0.5f, -0.5f, cornerRadius, cornerRadius);

            IPath cornerToptLeft = rect.Clip(new EllipsePolygon(cornerRadius - 0.5f, cornerRadius - 0.5f, cornerRadius));

            float rightPos = imageWidth - cornerToptLeft.Bounds.Width + 1;
            float bottomPos = imageHeight - cornerToptLeft.Bounds.Height + 1;

            IPath cornerTopRight = cornerToptLeft.RotateDegree(90).Translate(rightPos, 0);
            IPath cornerBottomLeft = cornerToptLeft.RotateDegree(-90).Translate(0, bottomPos);
            IPath cornerBottomRight = cornerToptLeft.RotateDegree(180).Translate(rightPos, bottomPos);

            return new PathCollection(cornerToptLeft, cornerBottomLeft, cornerTopRight, cornerBottomRight);
        }
    }
}
