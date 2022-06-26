#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Sanakan.Database.Models;
using Sanakan.Extensions;
using Sanakan.Services.PocketWaifu;
using Shinden;
using Shinden.Models;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Numerics;

namespace Sanakan.Services
{
    public class ImageProcessing
    {
        private FontFamily _digital = new FontCollection().Add("Fonts/Digital.ttf");
        private FontFamily _latoBold = new FontCollection().Add("Fonts/Lato-Bold.ttf");
        private FontFamily _latoLight = new FontCollection().Add("Fonts/Lato-Light.ttf");
        private FontFamily _latoRegular = new FontCollection().Add("Fonts/Lato-Regular.ttf");

        private readonly ShindenClient _shclient;
        private Dictionary<(FontFamily, float), Font> _fonts;
        private Dictionary<string, Color> _colors;
        private readonly string[] _extensions = new[] { "png", "jpg", "jpeg", "gif", "webp" };

        public ImageProcessing(ShindenClient shinden)
        {
            _shclient = shinden;
            _fonts = new Dictionary<(FontFamily, float), Font>();
            _colors = new Dictionary<string, Color>();
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
                        foreach (var ext in _extensions)
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
                    return Stream.Null;
                }
            }

            return null;
        }

        private Font GetOrCreateFont(FontFamily family, float size)
        {
            if (_fonts.ContainsKey((family, size)))
                return _fonts[(family, size)];
            else
            {
                var font = new Font(family, size);
                _fonts.Add((family, size), font);
                return font;
            }
        }

        private Color GetOrCreateColor(string hex)
        {
            if (_colors.ContainsKey(hex))
                return _colors[hex];
            else
            {
                var color = Color.ParseHex(hex);
                _colors.Add(hex, color);
                return color;
            }
        }

        private Font GetFontSize(FontFamily fontFamily, float size, string text, float maxWidth)
        {
            var font = new Font(fontFamily, size);
            var measured = TextMeasurer.Measure(text, new TextOptions(font));

            while (measured.Width > maxWidth)
            {
                if (--size < 1) break;
                font = GetOrCreateFont(fontFamily, size);
                measured = TextMeasurer.Measure(text, new TextOptions(font));
            }

            return font;
        }

        private void CheckProfileImageSize(Image image, Size size, bool strech)
        {
            if (image.Width > size.Width || image.Height > size.Height)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = size
                }));

                return;
            }

            if (!strech)
                return;

            if (image.Width < size.Width || image.Height < size.Height)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Stretch,
                    Size = size
                }));
            }
        }

        public async Task SaveImageFromUrlAsync(string url, string path)
            => await SaveImageFromUrlAsync(url, path, Size.Empty);

        public async Task SaveImageFromUrlAsync(string url, string path, Size size, bool strech = false)
        {
            using (var stream = await GetImageFromUrlAsync(url, true))
            {
                using (var image = Image.Load(stream))
                {
                    if (size.Height > 0 || size.Width > 0)
                        CheckProfileImageSize(image, size, strech);

                    image.SaveToPath(path);
                }
            }
        }

        public async Task<Image<Rgba32>> GetUserProfileAsync(IUserInfo shindenUser, User botUser, string avatarUrl, long topPos, string nickname, Discord.Color color)
        {
            if (color == Discord.Color.Default)
                color = Discord.Color.DarkerGrey;

            string rangName = shindenUser?.Rank ?? "";
            string colorRank = color.RawValue.ToString("X6");

            var nickFont = GetFontSize(_latoBold, 28, nickname, 290);
            var rangFont = GetOrCreateFont(_latoRegular, 16);
            var levelFont = GetOrCreateFont(_latoBold, 40);

            var template = Image.Load("./Pictures/profileBody.png");
            var profilePic = new Image<Rgba32>(template.Width, template.Height);

            if (!File.Exists(botUser.BackgroundProfileUri))
                botUser.BackgroundProfileUri = "./Pictures/defBg.png";

            using (var userBg = Image.Load(botUser.BackgroundProfileUri))
            {
                profilePic.Mutate(x => x.DrawImage(userBg, new Point(0, 0), 1));
                profilePic.Mutate(x => x.DrawImage(template, new Point(0, 0), 1));

                template.Dispose();
            }

            using (var avatar = Image.Load(await GetImageFromUrlAsync(avatarUrl)))
            {
                using (var avBack = new Image<Rgba32>(82, 82))
                {
                    avBack.Mutate(x => x.BackgroundColor(GetOrCreateColor(colorRank)));
                    avBack.Mutate(x => x.Round(42));

                    profilePic.Mutate(x => x.DrawImage(avBack, new Point(20, 115), 1));
                }

                avatar.Mutate(x => x.Resize(new Size(80, 80)));
                avatar.Mutate(x => x.Round(42));

                profilePic.Mutate(x => x.DrawImage(avatar, new Point(21, 116), 1));
            }

            var defFontColor = GetOrCreateColor("#7f7f7f");
            var posColor = GetOrCreateColor("#FFD700");

            if (topPos == 2)
                posColor = GetOrCreateColor("#c0c0c0");
            else if (topPos == 3)
                posColor = GetOrCreateColor("#cd7f32");
            else if (topPos > 3)
                posColor = defFontColor;

            profilePic.Mutate(x => x.DrawText(nickname, nickFont, GetOrCreateColor("#a7a7a7"), new Point(132, 147 + (int)((30 - nickFont.Size) / 2))));
            profilePic.Mutate(x => x.DrawText(rangName, rangFont, defFontColor, new Point(132, 177)));

            var mLevel = TextMeasurer.Measure($"{botUser.Level}", new TextOptions(levelFont));
            profilePic.Mutate(x => x.DrawText($"{botUser.Level}", levelFont, defFontColor, new Point((int)(125 - mLevel.Width) / 2, 203)));

            var mTopPos = TextMeasurer.Measure($"{topPos}", new TextOptions(levelFont));
            profilePic.Mutate(x => x.DrawText($"{topPos}", levelFont, posColor, new Point((int)(125 - mTopPos.Width) / 2, 281)));

            var mScOwn = TextMeasurer.Measure($"{botUser.ScCnt}", new TextOptions(rangFont));
            profilePic.Mutate(x => x.DrawText($"{botUser.ScCnt}", rangFont, defFontColor, new Point((int)(125 - mScOwn.Width) / 2, 362)));

            var mTcOwn = TextMeasurer.Measure($"{botUser.TcCnt}", new TextOptions(rangFont));
            profilePic.Mutate(x => x.DrawText($"{botUser.TcCnt}", rangFont, defFontColor, new Point((int)(125 - mTcOwn.Width) / 2, 401)));

            var mMsg = TextMeasurer.Measure($"{botUser.MessagesCnt}", new TextOptions(rangFont));
            profilePic.Mutate(x => x.DrawText($"{botUser.MessagesCnt}", rangFont, defFontColor, new Point((int)(125 - mMsg.Width) / 2, 442)));

            if (botUser.GameDeck.Waifu != 0 && botUser.ShowWaifuInProfile)
            {
                var tChar = botUser.GameDeck.Cards.OrderBy(x => x.Rarity).FirstOrDefault(x => x.Character == botUser.GameDeck.Waifu);
                if (tChar != null)
                {
                    using (var cardImage = await GetWaifuInProfileCardAsync(tChar))
                    {
                        cardImage.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(105, 0)
                        }));
                        profilePic.Mutate(x => x.DrawImage(cardImage, new Point(10, 350), 1));
                    }
                }
            }

            var prevLvlExp = ExperienceManager.CalculateExpForLevel(botUser.Level);
            var nextLvlExp = ExperienceManager.CalculateExpForLevel(botUser.Level + 1);
            var expOnLvl = botUser.ExpCnt - prevLvlExp;
            var lvlExp = nextLvlExp - prevLvlExp;

            if (expOnLvl < 0) expOnLvl = 0;
            if (lvlExp < 0) lvlExp = expOnLvl + 1;

            int progressBarLength = (int)(305f * ((double)expOnLvl / (double)lvlExp));
            if (progressBarLength > 0)
            {
                using (var progressBar = new Image<Rgba32>(progressBarLength, 19))
                {
                    progressBar.Mutate(x => x.BackgroundColor(GetOrCreateColor("#828282")));
                    profilePic.Mutate(x => x.DrawImage(progressBar, new Point(135, 201), 1));
                }
            }

            string expText = $"EXP: {expOnLvl} / {lvlExp}";
            var mExp = TextMeasurer.Measure(expText, new TextOptions(rangFont));
            profilePic.Mutate(x => x.DrawText(expText, rangFont, GetOrCreateColor("#ffffff"), new Point(135 + ((int)(305 - mExp.Width) / 2), 201)));

            using (var inside = GetProfileInside(shindenUser, botUser))
            {
                profilePic.Mutate(x => x.DrawImage(inside, new Point(125, 228), 1));
            }

            return profilePic;
        }

        private Image<Rgba32> GetProfileInside(IUserInfo shindenUser, User botUser)
        {
            var image = new Image<Rgba32>(325, 272);

            if (!File.Exists(botUser.StatsReplacementProfileUri))
            {
                if ((botUser.ProfileType == ProfileType.Img || botUser.ProfileType == ProfileType.StatsWithImg))
                    botUser.ProfileType = ProfileType.Stats;
            }

            switch (botUser.ProfileType)
            {
                case ProfileType.Stats:
                case ProfileType.StatsWithImg:
                    if (shindenUser != null)
                    {
                        if (shindenUser?.ListStats?.AnimeStatus != null)
                        {
                            using (var stats = GetRWStats(shindenUser?.ListStats?.AnimeStatus,
                                "./Pictures/statsAnime.png", shindenUser.GetMoreSeriesStats(false)))
                            {
                                image.Mutate(x => x.DrawImage(stats, new Point(0, 2), 1));
                            }
                        }
                        if (shindenUser?.ListStats?.MangaStatus != null)
                        {
                            using (var stats = GetRWStats(shindenUser?.ListStats?.MangaStatus,
                                "./Pictures/statsManga.png", shindenUser.GetMoreSeriesStats(true)))
                            {
                                image.Mutate(x => x.DrawImage(stats, new Point(0, 142), 1));
                            }
                        }

                        if (botUser.ProfileType == ProfileType.StatsWithImg)
                            goto case ProfileType.Img;
                    }
                    break;

                case ProfileType.Cards:
                    {
                        using (var cardsBg = GetCardsProfileImage(botUser).Result)
                        {
                            image.Mutate(x => x.DrawImage(cardsBg, new Point(0, 0), 1));
                        }
                    }
                    break;

                case ProfileType.Img:
                    {
                        using (var userBg = Image.Load(botUser.StatsReplacementProfileUri))
                        {
                            image.Mutate(x => x.DrawImage(userBg, new Point(0, 0), 1));
                        }
                    }
                    break;
            }

            return image;
        }

        private async Task<Image<Rgba32>> GetCardsProfileImage(User botUser)
        {
            var profilePic = new Image<Rgba32>(325, 272);

            if (botUser.GameDeck.Waifu != 0)
            {
                var tChar = botUser.GameDeck.Cards.OrderBy(x => x.Rarity).FirstOrDefault(x => x.Character == botUser.GameDeck.Waifu);
                if (tChar != null)
                {
                    using (var cardImage = await GetWaifuInProfileCardAsync(tChar))
                    {
                        cardImage.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(0, 260)
                        }));
                        profilePic.Mutate(x => x.DrawImage(cardImage, new Point(10, 6), 1));
                    }
                }
            }

            var sss = $"{botUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.SSS)}";
            var ss = $"{botUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.SS)}";
            var s = $"{botUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.S)}";
            var a = $"{botUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.A)}";
            var b = $"{botUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.B)}";
            var c = $"{botUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.C)}";
            var d = $"{botUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.D)}";
            var e = $"{botUser.GameDeck.Cards.Count(x => x.Rarity == Rarity.E)}";

            int jumpY = 18;
            int row2X = 45;
            int startY = 9;
            int startX = 205;
            var font1 = GetFontSize(_latoBold, 18, $"SUM", 100);
            var font2 = GetFontSize(_latoLight, 18, "10000", 130);

            profilePic.Mutate(x => x.DrawText("SSS", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText(sss, font2, GetOrCreateColor("#A4A4A4"), new Point(startX + row2X, startY)));
            startY += jumpY;

            profilePic.Mutate(x => x.DrawText("SS", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText(ss, font2, GetOrCreateColor("#A4A4A4"), new Point(startX + row2X, startY)));
            startY += jumpY;

            profilePic.Mutate(x => x.DrawText("S", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText(s, font2, GetOrCreateColor("#A4A4A4"), new Point(startX + row2X, startY)));
            startY += jumpY;

            profilePic.Mutate(x => x.DrawText("A", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText(a, font2, GetOrCreateColor("#A4A4A4"), new Point(startX + row2X, startY)));
            startY += jumpY;

            profilePic.Mutate(x => x.DrawText("B", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText(b, font2, GetOrCreateColor("#A4A4A4"), new Point(startX + row2X, startY)));
            startY += jumpY;

            profilePic.Mutate(x => x.DrawText("C", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText(c, font2, GetOrCreateColor("#A4A4A4"), new Point(startX + row2X, startY)));
            startY += jumpY;

            profilePic.Mutate(x => x.DrawText("D", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText(d, font2, GetOrCreateColor("#A4A4A4"), new Point(startX + row2X, startY)));
            startY += jumpY;

            profilePic.Mutate(x => x.DrawText("E", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText(e, font2, GetOrCreateColor("#A4A4A4"), new Point(startX + row2X, startY)));
            startY += jumpY;

            profilePic.Mutate(x => x.DrawText("SUM", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText($"{botUser.GameDeck.Cards.Count}", font2, GetOrCreateColor("#A4A4A4"), new Point(startX + row2X, startY)));
            startY += jumpY * 4;

            profilePic.Mutate(x => x.DrawText("CT", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText($"{botUser.GameDeck.CTCnt}", font2, GetOrCreateColor("#A4A4A4"), new Point(startX + row2X, startY)));
            startY += jumpY;

            profilePic.Mutate(x => x.DrawText("K", font1, GetOrCreateColor("#A4A4A4"), new Point(startX, startY)));
            profilePic.Mutate(x => x.DrawText(botUser.GameDeck.Karma.ToString("F"), font2, GetOrCreateColor("#A4A4A4"), new Point(startX + 15, startY)));

            return profilePic;
        }

        private async Task<Image<Rgba32>> GetSiteStatisticUserBadge(string avatarUrl, string name, string color)
        {
            var font = GetFontSize(_latoBold, 32, name, 360);

            var badge = new Image<Rgba32>(450, 65);
            badge.Mutate(x => x.DrawText(name, font, GetOrCreateColor("#A4A4A4"), new Point(72, 3 + (int)((58 - font.Size) / 2))));

            using (var border = new Image<Rgba32>(3, 57))
            {
                border.Mutate(x => x.BackgroundColor(GetOrCreateColor(color)));
                badge.Mutate(x => x.DrawImage(border, new Point(63, 5), 1));
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

        private Image GetRWStats(ISeriesStatus status, string path, MoreSeriesStatus more)
        {
            int startPointX = 7;
            int startPointY = 3;
            var baseImg = Image.Load(path);

            if (status.Total.HasValue && status.Total > 0)
            {
                using (var bar = GetStatusBar(status.Total.Value, status.InProgress.Value, status.Completed.Value,
                     status.Skipped.Value, status.OnHold.Value, status.Dropped.Value, status.InPlan.Value))
                {
                    bar.Mutate(x => x.Round(5));
                    baseImg.Mutate(x => x.DrawImage(bar, new Point(startPointX, startPointY), 1));
                }
            }

            startPointY += 21;
            startPointX += 110;
            int ySecondStart = startPointY;
            int fontSizeAndInterline = 10 + 6;
            var font = GetOrCreateFont(_latoBold, 13);
            int xSecondRow = startPointX + 200;
            var fontColor = GetOrCreateColor("#727272");

            ulong?[] rowArr = { status?.InProgress, status?.Completed, status?.Skipped, status?.OnHold, status?.Dropped, status?.InPlan };
            for (int i = 0; i < rowArr.Length; i++)
            {
                baseImg.Mutate(x => x.DrawText($"{rowArr[i]}", font, fontColor, new Point(startPointX, startPointY)));
                startPointY += fontSizeAndInterline;
            }

            var gOptions = new TextOptions(font) { HorizontalAlignment = HorizontalAlignment.Right };
            gOptions.Origin = new Point(xSecondRow, ySecondStart);

            baseImg.Mutate(x => x.DrawText(gOptions, $"{more?.Score?.Rating.Value.ToString("0.0")}", fontColor));
            ySecondStart += fontSizeAndInterline;
            gOptions.Origin = new Point(xSecondRow, ySecondStart);

            baseImg.Mutate(x => x.DrawText(gOptions, $"{status?.Total}", fontColor));
            ySecondStart += fontSizeAndInterline;
            gOptions.Origin = new Point(xSecondRow, ySecondStart);

            baseImg.Mutate(x => x.DrawText(gOptions, $"{more?.Count}", fontColor));
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
                gOptions.Origin = new Point(xSecondRow, ySecondStart);
                baseImg.Mutate(x => x.DrawText(gOptions, $"{fs} {sc}", fontColor));

                ySecondStart += fontSizeAndInterline;
                gOptions.Origin = new Point(xSecondRow, ySecondStart);
                baseImg.Mutate(x => x.DrawText(gOptions, $"{string.Join<string>(" ", listTime)}", fontColor));
            }
            else
            {
                baseImg.Mutate(x => x.DrawText(gOptions, $"{string.Join<string>(" ", listTime)}", fontColor));
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
                        thisBar.Mutate(x => x.BackgroundColor(GetOrCreateColor(colors[i])));
                        bar.Mutate(x => x.DrawImage(thisBar, new Point(offset, 0), 1));
                        offset += arrLength[i];
                    }
                }
            }

            return bar;
        }

        private Image GetLastRWListCover(Stream imageStream)
        {
            if (imageStream == null) return null;

            var cover = Image.Load(imageStream);
            cover.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(20, 50)
            }));

            return cover;
        }

        private async Task<Image<Rgba32>> GetLastRWList(List<ILastReaded> lastRead, List<ILastWatched> lastWatch)
        {
            var titleFont = GetOrCreateFont(_latoBold, 10);
            var nameFont = GetOrCreateFont(_latoBold, 16);
            var fColor = GetOrCreateColor("#9A9A9A");
            int startY = 24;

            var image = new Image<Rgba32>(175, 250);
            image.Mutate(x => x.DrawText($"Ostatnio obejrzane:", nameFont, fColor, new Point(0, 4)));
            if (lastWatch != null)
            {
                int max = -1;
                foreach (var last in lastWatch)
                {
                    if (++max >= 3) break;
                    using (var stream = await GetImageFromUrlAsync(last.AnimeCoverUrl, true))
                    {
                        using (var cover = GetLastRWListCover(stream))
                        {
                            if (cover != null)
                                image.Mutate(x => x.DrawImage(cover, new Point(0, startY + (35 * max)), 1));
                        }
                    }

                    image.Mutate(x => x.DrawText($"{last.AnimeTitle.TrimToLength(29)}", titleFont, fColor, new Point(25, startY + (35 * max))));
                    image.Mutate(x => x.DrawText($"{last.EpisodeNo} / {last.EpisodesCnt}", titleFont, fColor, new Point(25, startY + 11 + (35 * max))));
                }
            }

            startY += 128;
            image.Mutate(x => x.DrawText($"Ostatnio przeczytane:", nameFont, fColor, new Point(0, 131)));
            if (lastRead != null)
            {
                int max = -1;
                foreach (var last in lastRead)
                {
                    if (++max >= 3) break;
                    using (var stream = await GetImageFromUrlAsync(last.MangaCoverUrl, true))
                    {
                        using (var cover = GetLastRWListCover(stream))
                        {
                            if (cover != null)
                                image.Mutate(x => x.DrawImage(cover, new Point(0, startY + (35 * max)), 1));
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
            if (color == Discord.Color.Default)
                color = Discord.Color.DarkerGrey;

            var baseImg = new Image<Rgba32>(500, 320);
            baseImg.Mutate(x => x.BackgroundColor(GetOrCreateColor("#36393e")));

            using (var template = Image.Load("./Pictures/siteStatsBody.png"))
            {
                baseImg.Mutate(x => x.DrawImage(template, new Point(0, 0), 1));
            }

            using (var avatar = await GetSiteStatisticUserBadge(shindenInfo.AvatarUrl, shindenInfo.Name, color.RawValue.ToString("X6")))
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
            if (color == Discord.Color.Default)
                color = Discord.Color.DarkerGrey;

            var msgText1 = "POZIOM";
            var msgText2 = "Awansuje na:";

            var textFont = GetOrCreateFont(_latoRegular, 16);
            var nickNameFont = GetOrCreateFont(_latoBold, 22);
            var lvlFont = GetOrCreateFont(_latoBold, 36);

            var msgText1Length = TextMeasurer.Measure(msgText1, new TextOptions(textFont));
            var msgText2Length = TextMeasurer.Measure(msgText2, new TextOptions(textFont));
            var nameLength = TextMeasurer.Measure(name, new TextOptions(nickNameFont));
            var lvlLength = TextMeasurer.Measure($"{ulvl}", new TextOptions(lvlFont));

            var textLength = lvlLength.Width + msgText1Length.Width > nameLength.Width ? lvlLength.Width + msgText1Length.Width : nameLength.Width;
            var estimatedLength = 106 + (int)(textLength > msgText2Length.Width ? textLength : msgText2Length.Width);

            var nickNameColor = color.RawValue.ToString("X6");
            var baseImg = new Image<Rgba32>((int)estimatedLength, 100);

            baseImg.Mutate(x => x.BackgroundColor(GetOrCreateColor("#36393e")));
            baseImg.Mutate(x => x.DrawText(msgText1, textFont, Color.Gray, new Point(98 + (int)lvlLength.Width, 75)));
            baseImg.Mutate(x => x.DrawText(name, nickNameFont, GetOrCreateColor(nickNameColor), new Point(98, 5)));
            baseImg.Mutate(x => x.DrawText(msgText2, textFont, Color.Gray, new Point(98, 30)));
            baseImg.Mutate(x => x.DrawText($"{ulvl}", lvlFont, Color.Gray, new Point(96, 55)));

            using (var colorRec = new Image<Rgba32>(82, 82))
            {
                colorRec.Mutate(x => x.BackgroundColor(GetOrCreateColor(nickNameColor)));
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

        public Image<Rgba32> GetFColorsView(SCurrency currency)
        {
            var message = GetOrCreateFont(_latoRegular, 16);
            var firstColumnMaxLength = TextMeasurer.Measure("A", new TextOptions(message));
            var secondColumnMaxLength = TextMeasurer.Measure("A", new TextOptions(message));

            var arrayOfColours = Enum.GetValues(typeof(FColor));
            var inFirstColumn = arrayOfColours.Length / 2;

            for (int i = 0; i < arrayOfColours.Length; i++)
            {
                var val = (uint)arrayOfColours.GetValue(i);

                var thisColor = (FColor)val;
                if (thisColor == FColor.None) continue;

                var name = $"{thisColor.ToString()} ({thisColor.Price(currency)} {currency.ToString().ToUpper()})";
                var nLen = TextMeasurer.Measure(name, new TextOptions(message));

                if (i < inFirstColumn + 1)
                {
                    if (firstColumnMaxLength.Width < nLen.Width)
                        firstColumnMaxLength = nLen;
                }
                else
                {
                    if (secondColumnMaxLength.Width < nLen.Width)
                        secondColumnMaxLength = nLen;
                }
            }

            int posY = 2;
            int posX = 0;
            int realWidth = (int)(firstColumnMaxLength.Width + secondColumnMaxLength.Width + 20);
            int realHeight = (int)(firstColumnMaxLength.Height + 2) * (inFirstColumn + 1);

            var imgBase = new Image<Rgba32>(realWidth, realHeight);
            imgBase.Mutate(x => x.BackgroundColor(GetOrCreateColor("#36393e")));
            imgBase.Mutate(x => x.DrawText("Lista:", message, GetOrCreateColor("#000000"), new Point(0, 0)));

            for (int i = 0; i < arrayOfColours.Length; i++)
            {
                if (inFirstColumn + 1 == i)
                {
                    posY = 2;
                    posX = (int)firstColumnMaxLength.Width + 10;
                }

                var val = (uint)arrayOfColours.GetValue(i);

                var thisColor = (FColor)val;
                if (thisColor == FColor.None) continue;

                posY += (int)firstColumnMaxLength.Height + 2;
                var tname = $"{thisColor.ToString()} ({thisColor.Price(currency)} {currency.ToString().ToUpper()})";
                imgBase.Mutate(x => x.DrawText(tname, message, GetOrCreateColor(val.ToString("X6")), new Point(posX, posY)));
            }

            return imgBase;
        }

        private async Task<Image> GetCharacterPictureAsync(string characterUrl, bool ultimate)
        {
            var characterImg = Image.Load($"./Pictures/PW/empty.png");
            if (ultimate)
            {
                characterImg = new Image<Rgba32>(475, 667);
            }

            using (var stream = await GetImageFromUrlAsync(characterUrl ?? "http://cdn.shinden.eu/cdn1/other/placeholders/title/225x350.jpg", true))
            {
                if (stream == null)
                    return characterImg;

                using (var image = Image.Load(stream))
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(characterImg.Width, 0)
                    }));

                    int startY = 0;
                    if (characterImg.Height > image.Height)
                        startY = (characterImg.Height / 2) - (image.Height / 2);

                    characterImg.Mutate(x => x.DrawImage(image, new Point(0, startY), 1));
                }
            }

            return characterImg;
        }

        private bool HasCustomBorderString(Card card)
        {
            switch (card.Quality)
            {
                case Quality.Gamma: return true;
                default: return false;
            }
        }

        private string GetCustomBorderString(Card card)
        {
            switch (card.Quality)
            {
                case Quality.Gamma:
                    {
                        var totalPower = card.GetHealthWithPenalty();
                        totalPower += card.GetDefenceWithBonus();
                        totalPower += card.GetAttackWithBonus();

                        if (totalPower > 5000) return $"./Pictures/PW/CG/{card.Quality}/Border_2.png";
                        if (totalPower > 2000) return $"./Pictures/PW/CG/{card.Quality}/Border_1.png";
                        return $"./Pictures/PW/CG/{card.Quality}/Border_0.png";
                    }

                default: return $"./Pictures/PW/CG/{card.Quality}/Border.png";
            }
        }

        private Image GenerateBorder(Card card)
        {
            var borderStr = $"./Pictures/PW/{card.Rarity}.png";
            var dereStr = $"./Pictures/PW/{card.Dere}.png";

            if (card.FromFigure)
            {
                borderStr = $"./Pictures/PW/CG/{card.Quality}/Border.png";
                dereStr = $"./Pictures/PW/CG/{card.Quality}/Dere/{card.Dere}.png";

                if (HasCustomBorderString(card))
                    borderStr = GetCustomBorderString(card);
            }

            var img = Image.Load(borderStr);

            using (var dere = Image.Load(dereStr))
            {
                img.Mutate(x => x.DrawImage(dere, new Point(0, 0), 1));
            }

            return img;
        }

        private async Task<Image> LoadCustomBorderAsync(Card card)
        {
            if (!card.HasCustomBorder())
                return GenerateBorder(card);

            using (var stream = await GetImageFromUrlAsync(card.CustomBorder))
            {
                if (stream == null)
                    return GenerateBorder(card);

                return Image.Load(stream);
            }
        }

        private void ApplyAlphaStats(Image<Rgba32> image, Card card)
        {
            var adFont = GetOrCreateFont(_latoBold, 36);
            var hpFont = GetOrCreateFont(_latoBold, 32);

            int hp = card.GetHealthWithPenalty();
            int def = card.GetDefenceWithBonus();
            int atk = card.GetAttackWithBonus();

            using (var hpImg = new Image<Rgba32>(120, 40))
            {
                hpImg.Mutate(x => x.DrawText($"{hp}", hpFont, GetOrCreateColor("#356231"), new Point(1)));
                hpImg.Mutate(x => x.Rotate(-18));

                image.Mutate(x => x.DrawImage(hpImg, new Point(320, 522), 1));
            }

            image.Mutate(x => x.DrawText($"{atk}", adFont, GetOrCreateColor("#522b4d"), new Point(43, 597)));
            image.Mutate(x => x.DrawText($"{def}", adFont, GetOrCreateColor("#00527f"), new Point(337, 597)));
        }

        private void ApplyBetaStats(Image<Rgba32> image, Card card)
        {
            var adFont = GetOrCreateFont(_latoBold, 36);
            var hpFont = GetOrCreateFont(_latoBold, 29);

            int hp = card.GetHealthWithPenalty();
            int def = card.GetDefenceWithBonus();
            int atk = card.GetAttackWithBonus();

            using (var hpImg = new Image<Rgba32>(120, 40))
            {
                hpImg.Mutate(x => x.DrawText($"{hp}", hpFont, GetOrCreateColor("#006633"), new Point(1)));
                hpImg.Mutate(x => x.Rotate(18));

                image.Mutate(x => x.DrawImage(hpImg, new Point(342, 332), 1));
            }

            using (var defImg = new Image<Rgba32>(120, 40))
            {
                defImg.Mutate(x => x.DrawText($"{def}", adFont, GetOrCreateColor("#1154b8"), new Point(1)));
                defImg.Mutate(x => x.Rotate(19));

                image.Mutate(x => x.DrawImage(defImg, new Point(28, 169), 1));
            }

            using (var atkImg = new Image<Rgba32>(120, 40))
            {
                atkImg.Mutate(x => x.DrawText($"{atk}", adFont, GetOrCreateColor("#7d0e0e"), new Point(1)));
                atkImg.Mutate(x => x.Rotate(-16));

                image.Mutate(x => x.DrawImage(atkImg, new Point(50, 496), 1));
            }
        }

        private void ApplyGammaStats(Image<Rgba32> image, Card card)
        {
            var aphFont = GetOrCreateFont(_latoBold, 26);

            int hp = card.GetHealthWithPenalty();
            int def = card.GetDefenceWithBonus();
            int atk = card.GetAttackWithBonus();

            var ops = new TextOptions(aphFont) { HorizontalAlignment = HorizontalAlignment.Center };
            ops.Origin = new Point(145, 587);
            image.Mutate(x => x.DrawText(ops, $"{atk}", GetOrCreateColor("#c9282c")));
            ops.Origin = new Point(185, 559);
            image.Mutate(x => x.DrawText(ops, $"{def}", GetOrCreateColor("#1d64d5")));
            ops.Origin = new Point(345, 587);
            image.Mutate(x => x.DrawText(ops, $"{hp}", GetOrCreateColor("#318b19")));
        }

        private void ApplyDeltaStats(Image<Rgba32> image, Card card)
        {
            var hpFont = GetOrCreateFont(_latoBold, 34);
            var adFont = GetOrCreateFont(_latoBold, 26);

            int hp = card.GetHealthWithPenalty();
            int def = card.GetDefenceWithBonus();
            int atk = card.GetAttackWithBonus();

            var drOps = new DrawingOptions() { Transform = Matrix3x2.CreateRotation(-0.46f) };
            var hpOps = new TextOptions(hpFont) { HorizontalAlignment = HorizontalAlignment.Center, Origin = new Point(114, 627) };

            var brush = Brushes.Solid(GetOrCreateColor("#356231"));
            var pen = Pens.Solid(GetOrCreateColor("#0000"), 0.1f);

            image.Mutate(x => x.DrawText(drOps, hpOps, $"{hp}", brush, pen));

            var ops = new TextOptions(adFont) { HorizontalAlignment = HorizontalAlignment.Center };
            ops.Origin = new Point(92, 594);
            image.Mutate(x => x.DrawText(ops, $"{atk}", GetOrCreateColor("#78261a")));
            ops.Origin = new Point(382, 594);
            image.Mutate(x => x.DrawText(ops, $"{def}", GetOrCreateColor("#00527f")));
        }

        private void ApplyEpsilonStats(Image<Rgba32> image, Card card)
        {
            var aphFont = GetOrCreateFont(_latoBold, 28);

            int hp = card.GetHealthWithPenalty();
            int def = card.GetDefenceWithBonus();
            int atk = card.GetAttackWithBonus();
            var ops = new TextOptions(aphFont) { KerningMode = KerningMode.Normal, Dpi = 80 };
            using (var defImg = new Image<Rgba32>(120, 40))
            {
                ops.Origin = new Point(1);
                defImg.Mutate(x => x.DrawText(ops, $"{hp}", GetOrCreateColor("#40ff40")));
                defImg.Mutate(x => x.Rotate(-4));

                image.Mutate(x => x.DrawImage(defImg, new Point(59, 352), 1));
            }

            ops.Origin = new Point(70, 428);
            image.Mutate(x => x.DrawText(ops, $"{atk}", GetOrCreateColor("#da4e00")));
            ops.Origin = new Point(54, 481);
            image.Mutate(x => x.DrawText(ops, $"{def}", GetOrCreateColor("#00a4ff")));
        }

        private void ApplyZetaStats(Image<Rgba32> image, Card card)
        {
            var aphFont = GetOrCreateFont(_digital, 28);

            int hp = card.GetHealthWithPenalty();
            int def = card.GetDefenceWithBonus();
            int atk = card.GetAttackWithBonus();

            var ops = new TextOptions(aphFont) { HorizontalAlignment = HorizontalAlignment.Center, Dpi = 80 };
            ops.Origin = new Point(370, 537);
            image.Mutate(x => x.DrawText(ops, atk.ToString("D4"), GetOrCreateColor("#da4e00")));
            ops.Origin = new Point(370, 564);
            image.Mutate(x => x.DrawText(ops, def.ToString("D4"), GetOrCreateColor("#00a4ff")));
            ops.Origin = new Point(363, 592);
            image.Mutate(x => x.DrawText(ops, hp.ToString("D5"), GetOrCreateColor("#40ff40")));
        }

        private String GetJotaStatColorString(Card card)
        {
            switch (card.Dere)
            {
                case Database.Models.Dere.Bodere:
                    return "#de1218";
                case Database.Models.Dere.Dandere:
                    return "#00ff7d";
                case Database.Models.Dere.Deredere:
                    return "#032ee0";
                case Database.Models.Dere.Kamidere:
                    return "#75d400";
                case Database.Models.Dere.Kuudere:
                    return "#008cff";
                case Database.Models.Dere.Mayadere:
                    return "#dc0090";
                case Database.Models.Dere.Raito:
                    return "#dfdfdf";
                case Database.Models.Dere.Tsundere:
                    return "#ff0056";
                case Database.Models.Dere.Yami:
                    return "#898989";
                case Database.Models.Dere.Yandere:
                    return "#f2c400";
                case Database.Models.Dere.Yato:
                    return "#5e5e5e";
                default:
                    return "#ffffff";
            }
        }

        private void ApplyJotaStats(Image<Rgba32> image, Card card)
        {
            var aphFont = GetOrCreateFont(_latoBold, 22);

            int hp = card.GetHealthWithPenalty();
            int def = card.GetDefenceWithBonus();
            int atk = card.GetAttackWithBonus();

            var jotaColor = GetJotaStatColorString(card);

            using (var atkImg = new Image<Rgba32>(120, 40))
            {
                atkImg.Mutate(x => x.DrawText($"{atk}", aphFont, GetOrCreateColor(jotaColor), new Point(1)));
                atkImg.Mutate(x => x.Rotate(-10));

                image.Mutate(x => x.DrawImage(atkImg, new Point(106, 540), 1));
            }

            using (var defImg = new Image<Rgba32>(120, 40))
            {
                defImg.Mutate(x => x.DrawText($"{def}", aphFont, GetOrCreateColor(jotaColor), new Point(1)));
                defImg.Mutate(x => x.Rotate(10));

                image.Mutate(x => x.DrawImage(defImg, new Point(310, 552), 1));
            }

            var ops = new TextOptions(aphFont) { HorizontalAlignment = HorizontalAlignment.Center, Origin = new Point(238, 580) };
            image.Mutate(x => x.DrawText(ops, $"{hp}", GetOrCreateColor(jotaColor)));
        }

        private void ApplyLambdaStats(Image<Rgba32> image, Card card)
        {
            var aphFont = GetOrCreateFont(_latoBold, 28);

            int hp = card.GetHealthWithPenalty();
            int def = card.GetDefenceWithBonus();
            int atk = card.GetAttackWithBonus();

            using (var hpImg = new Image<Rgba32>(120, 40))
            {
                hpImg.Mutate(x => x.DrawText($"{hp}", aphFont, GetOrCreateColor("#6bedc8"), new Point(1)));
                hpImg.Mutate(x => x.Rotate(-19));

                image.Mutate(x => x.DrawImage(hpImg, new Point(57, 549), 1));
            }

            using (var atkImg = new Image<Rgba32>(120, 40))
            {
                atkImg.Mutate(x => x.DrawText($"{atk}", aphFont, GetOrCreateColor("#fda9fd"), new Point(1)));
                atkImg.Mutate(x => x.Rotate(34));

                image.Mutate(x => x.DrawImage(atkImg, new Point(80, 479), 1));
            }

            image.Mutate(x => x.DrawText($"{def}", aphFont, GetOrCreateColor("#49deff"), new Point(326, 570)));
        }

        private String GetThetaStatColorString(Card card)
        {
            switch (card.Dere)
            {
                case Database.Models.Dere.Bodere:
                    return "#ff2700";
                case Database.Models.Dere.Dandere:
                    return "#00fd8b";
                case Database.Models.Dere.Deredere:
                    return "#003bff";
                case Database.Models.Dere.Kamidere:
                    return "#f6f901";
                case Database.Models.Dere.Kuudere:
                    return "#008fff";
                case Database.Models.Dere.Mayadere:
                    return "#ff00df";
                case Database.Models.Dere.Raito:
                    return "#ffffff";
                case Database.Models.Dere.Tsundere:
                    return "#ff0072";
                case Database.Models.Dere.Yami:
                    return "#565656";
                case Database.Models.Dere.Yandere:
                    return "#ffa100";
                case Database.Models.Dere.Yato:
                    return "#ffffff";
                default:
                    return "#ffffff";
            }
        }

        private void ApplyThetaStats(Image<Rgba32> image, Card card)
        {
            var aphFont = GetOrCreateFont(_digital, 28);

            int hp = card.GetHealthWithPenalty();
            int def = card.GetDefenceWithBonus();
            int atk = card.GetAttackWithBonus();

            var thetaColor = GetThetaStatColorString(card);

            var ops = new TextOptions(aphFont) { KerningMode = KerningMode.Normal, Dpi = 80, HorizontalAlignment = HorizontalAlignment.Right };
            ops.Origin = new Point(410, 515);
            image.Mutate(x => x.DrawText(ops, $"{atk}", GetOrCreateColor(thetaColor)));
            ops.Origin = new Point(410, 552);
            image.Mutate(x => x.DrawText(ops, $"{def}", GetOrCreateColor(thetaColor)));
            ops.Origin = new Point(410, 589);
            image.Mutate(x => x.DrawText(ops, $"{hp}", GetOrCreateColor(thetaColor)));
        }

        private string GetStatsString(Card card)
        {
            switch (card.Quality)
            {
                case Quality.Jota: return $"./Pictures/PW/CG/{card.Quality}/Stats/{card.Dere}_Stats.png";
                case Quality.Theta: return $"./Pictures/PW/CG/{card.Quality}/{card.Dere}_Stats.png";
                default: return $"./Pictures/PW/CG/{card.Quality}/Stats.png";
            }
        }

        private string GetBorderBackString(Card card)
        {
            switch (card.Quality)
            {
                case Quality.Jota: return $"./Pictures/PW/CG/{card.Quality}/Border/{card.Dere}_Border.png";
                default: return  $"./Pictures/PW/CG/{card.Quality}/BorderBack.png";
            }
        }

        private void ApplyUltimateStats(Image<Rgba32> image, Card card)
        {
            var statsStr = GetStatsString(card);
            if (File.Exists(statsStr))
            {
                using (var stats = Image.Load(statsStr))
                {
                    image.Mutate(x => x.DrawImage(stats, new Point(0, 0), 1));
                }
            }

            switch (card.Quality)
            {
                case Quality.Alpha:
                    ApplyAlphaStats(image, card);
                    break;
                case Quality.Beta:
                    ApplyBetaStats(image, card);
                    break;
                case Quality.Gamma:
                    ApplyGammaStats(image, card);
                    break;
                case Quality.Delta:
                    ApplyDeltaStats(image, card);
                    break;
                case Quality.Epsilon:
                    ApplyEpsilonStats(image, card);
                    break;
                case Quality.Zeta:
                    ApplyZetaStats(image, card);
                    break;
                case Quality.Jota:
                    ApplyJotaStats(image, card);
                    break;
                case Quality.Lambda:
                    ApplyLambdaStats(image, card);
                    break;
                case Quality.Theta:
                    ApplyThetaStats(image, card);
                    break;

                default:
                    break;
            }
        }

        private bool AllowStatsOnNoStatsImage(Card card)
        {
            switch (card.Quality)
            {
                case Quality.Zeta:
                    if (card.HasCustomBorder())
                        return false;
                    return true;

                default:
                    return false;
            }
        }

        private void ApplyStats(Image<Rgba32> image, Card card, bool applyNegativeStats = false)
        {
            int health = card.GetHealthWithPenalty();
            int defence = card.GetDefenceWithBonus();
            int attack = card.GetAttackWithBonus();

            using (var shield = Image.Load($"./Pictures/PW/heart.png"))
            {
                image.Mutate(x => x.DrawImage(shield, new Point(0, 0), 1));
            }

            using (var shield = Image.Load($"./Pictures/PW/shield.png"))
            {
                image.Mutate(x => x.DrawImage(shield, new Point(0, 0), 1));
            }

            using (var fire = Image.Load($"./Pictures/PW/fire.png"))
            {
                image.Mutate(x => x.DrawImage(fire, new Point(0, 0), 1));
            }

            var starType = card.GetCardStarType();
            var starCnt = card.GetCardStarCount();

            var starX = 239 - (18 * starCnt);
            for (int i = 0; i < starCnt; i++)
            {
                using (var fire = Image.Load($"./Pictures/PW/stars/{starType}_{card.StarStyle}.png"))
                {
                    image.Mutate(x => x.DrawImage(fire, new Point(starX, 30), 1));
                }

                starX += 36;
            }

            int startXDef = 390;
            if (defence < 10) startXDef += 15;
            if (defence > 99) startXDef -= 15;

            int startXAtk = 390;
            if (attack < 10) startXAtk += 15;
            if (attack > 99) startXAtk -= 15;

            int startXHp = 380;
            if (health < 10) startXHp += 15;
            if (health > 99) startXHp -= 15;

            var numFont = GetOrCreateFont(_latoBold, 54);
            image.Mutate(x => x.DrawText($"{health}", numFont, GetOrCreateColor("#000000"), new Point(startXHp, 178)));
            image.Mutate(x => x.DrawText($"{attack}", numFont, GetOrCreateColor("#000000"), new Point(startXAtk, 308)));
            image.Mutate(x => x.DrawText($"{defence}", numFont, GetOrCreateColor("#000000"), new Point(startXDef, 428)));

            if (applyNegativeStats)
            {
                using (var neg = Image.Load($"./Pictures/PW/neg.png"))
                {
                    image.Mutate(x => x.DrawImage(neg, new Point(0, 0), 1));
                }
            }
        }

        private void ApplyBorderBack(Image<Rgba32> image, Card card)
        {
            var isFromFigureOriginalBorder = !card.HasCustomBorder() && card.FromFigure;
            var backBorderStr = GetBorderBackString(card);

            if (isFromFigureOriginalBorder && File.Exists(backBorderStr))
            {
                using (var back = Image.Load(backBorderStr))
                {
                    image.Mutate(x => x.DrawImage(back, new Point(0, 0), 1));
                }
            }
        }

        private async Task<Image<Rgba32>> GetWaifuCardNoStatsAsync(Card card)
        {
            var image = new Image<Rgba32>(475, 667);

            ApplyBorderBack(image, card);

            using (var chara = await GetCharacterPictureAsync(card.GetImage(), card.FromFigure))
            {
                var mov = card.FromFigure ? 0 : 13;
                image.Mutate(x => x.DrawImage(chara, new Point(mov, mov), 1));
            }

            using (var bord = GenerateBorder(card))
            {
                image.Mutate(x => x.DrawImage(bord, new Point(0, 0), 1));
            }

            if (AllowStatsOnNoStatsImage(card))
            {
                ApplyUltimateStats(image, card);
            }

            return image;
        }

        public async Task<Image<Rgba32>> GetWaifuInProfileCardAsync(Card card)
        {
            var image = new Image<Rgba32>(475, 667);

            ApplyBorderBack(image, card);

            using (var chara = await GetCharacterPictureAsync(card.GetImage(), card.FromFigure))
            {
                var mov = card.FromFigure ? 0 : 13;
                image.Mutate(x => x.DrawImage(chara, new Point(mov, mov), 1));
            }

            using (var bord = await LoadCustomBorderAsync(card))
            {
                image.Mutate(x => x.DrawImage(bord, new Point(0, 0), 1));
            }

            if (AllowStatsOnNoStatsImage(card))
            {
                ApplyUltimateStats(image, card);
            }

            return image;
        }

        public Image GetDuelCardImage(DuelInfo info, DuelImage image, Image<Rgba32> win, Image<Rgba32> los)
        {
            int Xiw = 76;
            int Yt = 780;
            int Yi = 131;
            int Xil = 876;

            if (info.Side == DuelInfo.WinnerSide.Right)
            {
                Xiw = 876;
                Xil = 76;
            }

            var nameFont = GetOrCreateFont(_latoBold, 34);
            var img = (image != null) ? Image.Load(image.Uri((int)info.Side)) : Image.Load((DuelImage.DefaultUri((int)info.Side)));

            win.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(450, 0)
            }));

            los.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(450, 0)
            }));

            if (info.Side != DuelInfo.WinnerSide.Draw)
                los.Mutate(x => x.Grayscale());

            img.Mutate(x => x.DrawImage(win, new Point(Xiw, Yi), 1));
            img.Mutate(x => x.DrawImage(los, new Point(Xil, Yi), 1));

            var options = new TextOptions(nameFont) { HorizontalAlignment = HorizontalAlignment.Center, WrappingLength = win.Width, Origin = new Point(Xiw, Yt)};
            img.Mutate(x => x.DrawText(options, info.Winner.Name, GetOrCreateColor(image != null ? image.Color : DuelImage.DefaultColor())));
            options.Origin = new Point(Xil, Yt);
            img.Mutate(x => x.DrawText(options, info.Loser.Name, GetOrCreateColor(image != null ? image.Color : DuelImage.DefaultColor())));

            return img;
        }

        public Image GetCatchThatWaifuImage(Image card, string pokeImg, int xPos, int yPos)
        {
            var image = Image.Load(pokeImg);
            image.Mutate(x => x.DrawImage(card, new Point(xPos, yPos), 1));
            return image;
        }

        public async Task<Image> GetWaifuCardAsync(string url, Card card)
        {
            if (url == null)
                return await GetWaifuCardAsync(card);

            return Image.Load(url);
        }

        public async Task<Image> GetWaifuCardAsync(Card card)
        {
            var image = await GetWaifuCardNoStatsAsync(card);

            if (card.FromFigure)
            {
                ApplyUltimateStats(image, card);
            }
            else
            {
                ApplyStats(image, card, !card.HasImage());
            }

            return image;
        }
    }
}