#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Sanakan.Extensions;
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

        private async Task<Stream> GetImageFromUrlAsync(string url, bool fixExt = false)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var res = await client.GetAsync(url);
                    if (res.IsSuccessStatusCode) 
                        return await res.Content.ReadAsStreamAsync();

                    if (fixExt)
                    {
                        var splited = url.Split(".");
                        var exts = new[] { "png", "jpeg", "gif", "jpg" };
                        foreach (var ext in exts)
                        {
                            splited[splited.Length - 1] = ext;
                            res = await client.GetAsync(string.Join(".", splited));

                            if (res.IsSuccessStatusCode)
                                return await res.Content.ReadAsStreamAsync();
                        }
                    }
                }
                catch (Exception)
                {
                    return null;
                }
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

        private Image<Rgba32> GetRWStats(ISeriesStatus status, string path, MoreSeriesStatus more)
        {
            int startPointX = 7;
            int startPointY = 3;
            var baseImg = Image.Load(path);

            if (status.Total.HasValue && status.Total > 0)
            {
                using (var bar = GetStatusBar(status.Total.Value, status.InProgress.Value, status.Completed.Value,
                     status.Skipped.Value, status.OnHold.Value, status.Dropped.Value, status.InPlan.Value))
                {
                    bar.Round(5);
                    baseImg.Mutate(x => x.DrawImage(bar, new Point(startPointX, startPointY), 1));
                }
            }

            startPointY += 24;
            startPointX += 110;
            int ySecondStart = startPointY;
            int fontSizeAndInterline = 10 + 6;
            var font = new Font(_latoBold, 13);
            int xSecondRow = startPointX + 200;
            var fontColor = Rgba32.FromHex("#727272");

            ulong?[] rowArr = { status?.InProgress, status?.Completed, status?.Skipped, status?.OnHold, status?.Dropped, status?.InPlan };
            for (int i = 0; i < rowArr.Length; i++)
            {
                baseImg.Mutate(x => x.DrawText($"{rowArr[i]}", font, fontColor, new Point(startPointX, startPointY)));
                startPointY += fontSizeAndInterline;
            }

            var gOptions = new TextGraphicsOptions { HorizontalAlignment = HorizontalAlignment.Right };

            baseImg.Mutate(x => x.DrawText(gOptions, $"{more?.Score?.Rating.Value.ToString("0.0")}", font, fontColor, new Point(xSecondRow, ySecondStart)));
            ySecondStart += fontSizeAndInterline;

            baseImg.Mutate(x => x.DrawText(gOptions, $"{status?.Total}", font, fontColor, new Point(xSecondRow, ySecondStart)));
            ySecondStart += fontSizeAndInterline;

            baseImg.Mutate(x => x.DrawText(gOptions, $"{more?.Count}", font, fontColor, new Point(xSecondRow, ySecondStart)));
            ySecondStart += fontSizeAndInterline;


            var listTime = new List<string>();
            if (more.Time != null)
            {
                if (more.Time.Years != 0) listTime.Add($"{more?.Time?.Years} lat");
                if (more.Time.Months != 0) listTime.Add($"{more?.Time?.Months} mies.");
                if (more.Time.Days != 0) listTime.Add($"{more?.Time?.Days} dni");
                if (more.Time.Hours != 0) listTime.Add($"{more?.Time?.Hours} h");
                if (more.Time.Minutes != 0) listTime.Add($"{more?.Time?.Minutes} m");
            }

            ySecondStart += fontSizeAndInterline;

            if (listTime.Count > 2)
            {
                string fs = listTime.First(); listTime.Remove(fs);
                string sc = listTime.First(); listTime.Remove(sc);
                baseImg.Mutate(x => x.DrawText(gOptions, $"{fs} {sc}", font, fontColor, new Point(xSecondRow, ySecondStart)));

                ySecondStart += fontSizeAndInterline;
                baseImg.Mutate(x => x.DrawText(gOptions, $"{string.Join<string>(" ", listTime)}", font, fontColor, new Point(xSecondRow, ySecondStart)));
            }
            else
            {
                baseImg.Mutate(x => x.DrawText(gOptions, $"{string.Join<string>(" ", listTime)}", font, fontColor, new Point(xSecondRow, ySecondStart)));
            }

            return baseImg;
        }

        private Image<Rgba32> GetStatusBar(ulong all, ulong green, ulong blue, ulong purple, ulong yellow, ulong red, ulong grey)
        {
            int offset = 0;
            int length = 311;
            int fixedLength = 0;

            var arrLength = new int[6];
            var arrProcent = new double[6];
            double[] arrValues = { green, blue, purple, yellow, red, grey };
            var colors = new[] { "#2db039", "#26448f", "#9966ff", "#f9d457", "#a12f31", "#c3c3c3" };

            for (int i = 0; i < arrValues.Length; i++)
            {
                if (arrValues[i] != 0)
                {
                    arrProcent[i] = arrValues[i] / all;
                    arrLength[i] = (int)((length * arrProcent[i]) + 0.5);
                    fixedLength += arrLength[i];
                }
            }

            if (fixedLength > length)
            {
                var res = arrLength.OrderByDescending(x => x).FirstOrDefault();
                arrLength[arrLength.ToList().IndexOf(res)] -= fixedLength - length;
            }

            var bar = new Image<Rgba32>(length, 17);
            for (int i = 0; i < arrValues.Length; i++)
            {
                if (arrValues[i] != 0)
                {
                    using (var thisBar = new Image<Rgba32>(arrLength[i] < 1 ? 1 : arrLength[i], 17))
                    {
                        thisBar.Mutate(x => x.BackgroundColor(Rgba32.FromHex(colors[i])));
                        bar.Mutate(x => x.DrawImage(thisBar, new Point(offset, 0), 1));
                        offset += arrLength[i];
                    }
                }
            }

            return bar;
        }

        private async Task<Image<Rgba32>> GetLastRWList(List<ILastReaded> lastRead, List<ILastWatched> lastWatch)
        {
            var titleFont = new Font(_latoBold, 10);
            var nameFont = new Font(_latoBold, 16);
            var fColor = Rgba32.FromHex("#9A9A9A");
            int startY = 25;

            var image = new Image<Rgba32>(175, 248);
            image.Mutate(x => x.DrawText($"Ostatnio obejrzane:", nameFont, fColor, new Point(0, 5)));
            if (lastWatch != null)
            {
                int max = -1;
                foreach (var last in lastWatch)
                {
                    if (++max >= 3) break;
                    using (var stream = await GetImageFromUrlAsync(last.AnimeCoverUrl, true))
                    {
                        if (stream != null)
                        {
                            using (var cover = Image.Load(stream))
                            {
                                cover.Mutate(x => x.Resize(new ResizeOptions
                                {
                                    Mode = ResizeMode.Max,
                                    Size = new Size(20, 50)
                                }));
                                image.Mutate(x => x.DrawImage(cover, new Point(0, startY + (35 * max)), 1));
                            }
                        }
                    }

                    image.Mutate(x => x.DrawText($"{last.AnimeTitle.TrimToLength(29)}", titleFont, fColor, new Point(25, startY + (35 * max))));
                    image.Mutate(x => x.DrawText($"{last.EpisodeNo} / {last.EpisodesCnt}", titleFont, fColor, new Point(25, startY + 11 + (35 * max))));
                }
            }

            startY += 128;
            image.Mutate(x => x.DrawText($"Ostatnio przeczytane:", nameFont, fColor, new Point(0, 133)));
            if (lastRead != null)
            {
                int max = -1;
                foreach (var last in lastRead)
                {
                    if (++max >= 3) break;
                    using (var stream = await GetImageFromUrlAsync(last.MangaCoverUrl, true))
                    {
                        if (stream != null)
                        {
                            using (var cover = Image.Load(stream))
                            {
                                cover.Mutate(x => x.Resize(new ResizeOptions
                                {
                                    Mode = ResizeMode.Max,
                                    Size = new Size(20, 50)
                                }));
                                image.Mutate(x => x.DrawImage(cover, new Point(0, startY + (35 * max)), 1));
                            }
                        }
                    }

                    image.Mutate(x => x.DrawText($"{last.MangaTitle.TrimToLength(29)}", titleFont, fColor, new Point(25, startY + (35 * max))));
                    image.Mutate(x => x.DrawText($"{last.ChapterNo} / {last.ChaptersCnt}", titleFont, fColor, new Point(25, startY + 11 + (35 * max))));
                }
            }

            return image;
        }

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

            using (var image = new Image<Rgba32>(325, 248))
            {
                if (shindenInfo?.ListStats?.AnimeStatus != null)
                {
                    using (var stats = GetRWStats(shindenInfo?.ListStats?.AnimeStatus,
                        "./Pictures/statsAnime.png", shindenInfo.GetMoreSeriesStats(false)))
                    {
                        image.Mutate(x => x.DrawImage(stats, new Point(0, 0), 1));
                    }
                }
                if (shindenInfo?.ListStats?.MangaStatus != null)
                {
                    using (var stats = GetRWStats(shindenInfo?.ListStats?.MangaStatus,
                        "./Pictures/statsManga.png", shindenInfo.GetMoreSeriesStats(true)))
                    {
                        image.Mutate(x => x.DrawImage(stats, new Point(0, 128), 1));
                    }
                }
                baseImg.Mutate(x => x.DrawImage(image, new Point(5, 71), 1));
            }

            using (var image = await GetLastRWList(lastRead, lastWatch))
            {
                baseImg.Mutate(x => x.DrawImage(image, new Point(330, 69), 1));
            }

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