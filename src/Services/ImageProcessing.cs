#pragma warning disable 1591

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Shinden.Models;
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

        private Font GetFontSize(FontFamily fontFamily, float size, string text, float maxWidth)
        {
            var font = new Font(fontFamily, size);
            var measured = TextMeasurer.Measure(text, new RendererOptions(font));

            while (measured.Width > maxWidth)
            {
                if (--size < 1) break;
                font = new Font(fontFamily, size);
                measured = TextMeasurer.Measure(text, new RendererOptions(font));
            }

            return font;
        }

        private async Task<Image<Rgba32>> GetSiteStatisticUserBadge(string avatarUrl, string name, string color)
        {
            var font = GetFontSize(_latoBold, 32, name, 360);

            var badge = new Image<Rgba32>(450, 65);
            badge.Mutate(x => x.DrawText(name, font, Rgba32.FromHex("#A4A4A4"), new Point(72, 6 + (int)((58 - font.Size) / 2))));

            using (var border = new Image<Rgba32>(3, 57))
            {
                border.Mutate(x => x.BackgroundColor(Rgba32.FromHex(color)));
                badge.Mutate(x => x.DrawImage(border,  new Point(63, 5), 1));
            }

            using (var stream = await GetImageFromUrlAsync(avatarUrl))
            {
                if (stream == null)
                    return badge;

                using (var avatar = Image.Load(stream))
                {
                    avatar.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Crop,
                        Size = new Size(57, 57)
                    }));
                    badge.Mutate(x => x.DrawImage(avatar, new Point(6, 5), 1));
                }
            }

            return badge;
        }

        // private Image<Rgba32> GetStats(ISeriesStatus status, string path)
        // {

        // }

        public async Task<Image<Rgba32>> GetSiteStatisticAsync(IUserInfo shindenInfo, Discord.Color color, List<ILastReaded> lastRead = null, List<ILastWatched> lastWatch = null)
        {
            var baseImg = new Image<Rgba32>(500, 320);
            baseImg.Mutate(x => x.BackgroundColor(Rgba32.FromHex("#36393e")));

            using (var template = Image.Load("./Pictures/siteStatsBody.png"))
            {
                baseImg.Mutate(x => x.DrawImage(template, new Point(0, 0), 1));
            }

            using (var avatar = await GetSiteStatisticUserBadge(shindenInfo.AvatarUrl, shindenInfo.Name, color.RawValue.ToString("X")))
            {
                baseImg.Mutate(x => x.DrawImage(avatar, new Point(0, 0), 1));
            }

            // using (var image = new Image<Rgba32>(325, 248))
            // {
            //     if (shindenInfo?.ListStats?.AnimeStatus != null)
            //     {
            //         using (var stats = GetStats(shindenInfo?.ListStats?.AnimeStatus,
            //             "./Pictures/statsAnime.png", shindenInfo.GetMoreSeriesStats(false)))
            //         {
            //             image.Mutate(x => x.DrawImage(stats, new Point(0, 0), 1));
            //         }
            //     }
            //     if (shindenInfo?.ListStats?.MangaStatus != null)
            //     {
            //         using (var stats = GetStats(shindenInfo?.ListStats?.MangaStatus,
            //             "./Pictures/statsManga.png", shindenInfo.GetMoreSeriesStats(true)))
            //         {
            //             image.Mutate(x => x.DrawImage(stats, new Point(0, 128), 1));
            //         }
            //     }
            //     baseImg.Mutate(x => x.DrawImage(image, new Point(5, 71), 1));
            // }

            return baseImg;
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

                using (var stream = await GetImageFromUrlAsync(avatarUrl))
                {
                    if (stream == null)
                        return baseImg;

                    using (var avatar = Image.Load(stream))
                    {
                        avatar.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Crop,
                            Size = new Size(80, 80)
                        }));
                        baseImg.Mutate(x => x.DrawImage(avatar, new Point(10, 10), 1));
                    }
                }
            }

            return baseImg;
        }
    }
}