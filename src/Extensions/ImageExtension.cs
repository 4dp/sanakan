using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

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
    }
}
