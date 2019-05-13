#pragma warning disable 1591

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace Sanakan.Services
{
    public class ImageProcessing
    {
        private FontFamily _latoBold = new FontCollection().Install("Fonts/Lato-Bold.ttf");
        private FontFamily _latoLight = new FontCollection().Install("Fonts/Lato-Light.ttf");
        private FontFamily _latoRegular = new FontCollection().Install("Fonts/Lato-Regular.ttf");

        public ImageProcessing()
        {
            if (!Directory.Exists("GOut")) Directory.CreateDirectory("GOut");
        }

        private async Task<Stream> GetImageFromUrlAsync(string url)
        {
            using (var client = new HttpClient())
            {
                var res = await client.GetAsync(url);
                if (res.IsSuccessStatusCode) 
                    return await res.Content.ReadAsStreamAsync();
            }
            return null;
        }

        public async Task<Image<Rgba32>> GetLevelUpBadgeAsync(string name, long ulvl, string avatarUrl, Discord.Color color)
        {
            var msgText1 = "POZIOM";
            var msgText2 = "Awansuje na:";

            var textFont = new Font(_latoRegular, 16);
            var nickNameFont = new Font(_latoBold, 22);
            var lvlFont = new Font(_latoBold, 36);

            var msgText1Length = TextMeasurer.Measure(msgText1, new RendererOptions(textFont));
            var msgText2Length = TextMeasurer.Measure(msgText2, new RendererOptions(textFont));
            var nameLength = TextMeasurer.Measure(name, new RendererOptions(nickNameFont));
            var lvlLength = TextMeasurer.Measure($"{ulvl}", new RendererOptions(lvlFont));

            var textLength = lvlLength.Width + msgText1Length.Width > nameLength.Width ? lvlLength.Width + msgText1Length.Width : nameLength.Width;
            var estimatedLength = 106 + (int)(textLength > msgText2Length.Width ? textLength : msgText2Length.Width);
        
            var nickNameColor = color.RawValue.ToString("X");
            var baseImg = new Image<Rgba32>((int)estimatedLength, 100);

            baseImg.Mutate(x => x.BackgroundColor(Rgba32.FromHex("#36393e")));
            baseImg.Mutate(x => x.DrawText(msgText1, textFont, Rgba32.Gray, new Point(98 + (int)lvlLength.Width, 75)));
            baseImg.Mutate(x => x.DrawText(name, nickNameFont, Rgba32.FromHex(nickNameColor), new Point(98, 10)));
            baseImg.Mutate(x => x.DrawText(msgText2, textFont, Rgba32.Gray, new Point(98, 30)));
            baseImg.Mutate(x => x.DrawText($"{ulvl}", lvlFont, Rgba32.Gray, new Point(96, 61)));

            using (var colorRec = new Image<Rgba32>(82, 82))
            {
                colorRec.Mutate(x => x.BackgroundColor(Rgba32.FromHex(nickNameColor)));
                baseImg.Mutate(x => x.DrawImage(colorRec, new Point(9, 9), 1));

                var stream = await GetImageFromUrlAsync(avatarUrl);
                if (stream != null)
                {
                    using (var avatar = Image.Load(stream))
                    {
                        avatar.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Crop,
                            Size = new Size(80, 80)
                        }));

                        baseImg.Mutate(x => x.DrawImage(avatar, new Point(10, 10), 1));
                    }
                    stream.Dispose();
                }
            }

            return baseImg;
        }
    }
}