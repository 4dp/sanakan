#pragma warning disable 1591

using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Sanakan.Extensions
{
    public static class ImageExtension
    {
        private static IImageEncoder _pngEncoder = new PngEncoder();
        private static IImageEncoder _jpgEncoder = new JpegEncoder()
        {
            Quality = 85
        };
        private static IImageEncoder _webpEncoder = new WebpEncoder()
        {
            FileFormat = WebpFileFormatType.Lossy,
            Quality = 85,
        };

        public static Stream ToJpgStream(this Image img)
        {
            var stream = new MemoryStream();
            img.Save(stream, _jpgEncoder);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static Stream ToPngStream(this Image img)
        {
            var stream = new MemoryStream();
            img.Save(stream, _pngEncoder);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static Stream ToWebpStream(this Image img)
        {
            var stream = new MemoryStream();
            img.Save(stream, _webpEncoder);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static string SaveToPath(this Image img, string path)
        {
            var extension = path.Split(".").Last().ToLower();
            var encoder = extension switch
            {
                "webp" => _webpEncoder,
                "png" => _pngEncoder,
                _ => _jpgEncoder
            };
            img.Save(path, encoder);
            return path;
        }

        public static string SaveToPath(this Image img, string path, int width, int height = 0)
        {
            img.Mutate(x => x.Resize(new Size(width, height)));
            return SaveToPath(img, path);
        }

        public static Image<T> ResizeAsNew<T>(this Image<T> img, int width, int height = 0) where T : unmanaged, IPixel<T>
        {
            var nImg = img.Clone();
            nImg.Mutate(x => x.Resize(new Size(width, height)));
            return nImg;
        }

        public static void Round(this IImageProcessingContext img, float radius)
        {
            var size = img.GetCurrentSize();
            var gOptions = new DrawingOptions{ GraphicsOptions = new GraphicsOptions { AlphaCompositionMode = PixelAlphaCompositionMode.DestOut } };
            img.Fill(gOptions, Color.Black, BuildCorners(size.Width, size.Height, radius));
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
