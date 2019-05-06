#pragma warning disable 1591

using System.Collections.Generic;
using Shinden.Models;
using Discord;
using Sanakan.Services;
using System;

namespace Sanakan.Extensions
{
    public static class ITitleInfoExtension
    {
        public static Embed ToEmbed(this ITitleInfo info)
        {
            if (info is IAnimeTitleInfo iam)
            {
                return iam.ToEmbed();
            }
            return (info as IMangaTitleInfo)?.ToEmbed();
        }

        public static Embed ToEmbed(this IAnimeTitleInfo info)
        {
            return new EmbedBuilder()
            {
                Title = info.Title.TrimToLength(EmbedBuilder.MaxTitleLength),
                Description = info.Description.Content.TrimToLength(1000),
                ThumbnailUrl = info.CoverUrl,
                Color = EMType.Info.Color(),
                Fields = info.GetFields(),
                Footer = info.GetFooter(),
                Url = info.AnimeUrl,
            }.Build();
        }

        public static EmbedFooterBuilder GetFooter(this ITitleInfo info)
        {
            string start = "";
            string finish = "";
            var def = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).ToLocalTime();
            if (info.FinishDate.IsSpecified)
            {
                finish = info.FinishDate.Date == def.Date ? "" : $" - {info.FinishDate.Date.ToShortDateString()}";
            }

            if (info.StartDate.IsSpecified)
            {
                start = info.StartDate.Date.ToShortDateString();
            }

            return new EmbedFooterBuilder()
            {
                Text = $"{start}{finish}",
            };
        }

        public static string GetPrecisonDate(this DateTime date, ulong precision)
        {
            switch (precision)
            {
                case 1: return date.ToString("yyyy");
                case 2: return date.ToString("yyyy - MM");
                default: return date.ToShortDateString();
            }
        }

        public static string ToName(this AnimeStatus status)
        {
            switch (status)
            {
                case AnimeStatus.CurrentlyAiring: return "Wychodzi";
                case AnimeStatus.FinishedAiring: return "Zakończone";
                case AnimeStatus.Proposal:
                case AnimeStatus.NotYetAired: return "Zapowiedź";
                default: return "Niesprecyzowane";
            }
        }

        public static string ToName(this MangaStatus status)
        {
            switch (status)
            {
                case MangaStatus.Publishing: return "Wychodzi";
                case MangaStatus.Finished: return "Zakończone";
                default: return "Niesprecyzowane";
            }
        }

        public static string ToName(this AnimeType type)
        {
            switch (type)
            {
                case AnimeType.Movie: return "Film";
                case AnimeType.Music: return "Teledysk";
                case AnimeType.Ona: return "ONA";
                case AnimeType.Ova: return "OVA";
                case AnimeType.Tv: return "TV";
                case AnimeType.Special: return "Odcinek specjalny";
                default: return "Niesprecyzowane";
            }
        }

        public static string ToName(this MangaType type)
        {
            switch (type)
            {
                case MangaType.LightNovel: return "Light novel";
                case MangaType.Doujinshi: return "Doujinshi";
                case MangaType.Manga: return "Manga";
                case MangaType.Manhua: return "Manhua";
                case MangaType.Manhwa: return "Manhwa";
                case MangaType.OneShot: return "One shot";
                default: return "Niesprecyzowane";
            }
        }

        public static List<EmbedFieldBuilder> GetFields(this IAnimeTitleInfo info)
        {
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            if (info.AlternativeTitles.Count > 0)
            {
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = "Tytuły alternatywne",
                    Value = string.Join(", ", info.AlternativeTitles).TrimToLength(EmbedFieldBuilder.MaxFieldValueLength),
                    IsInline = false
                });
            }

            foreach(var tagType in info.TagCategories)
            {
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = tagType.Name.TrimToLength(EmbedFieldBuilder.MaxFieldNameLength),
                    Value = string.Join(", ", tagType.Tags).TrimToLength(EmbedFieldBuilder.MaxFieldValueLength),
                    IsInline = false
                });
            }

            fields.Add(new EmbedFieldBuilder()
            {
                Name = "Typ",
                Value = info.Type.ToName(),
                IsInline = true
            });

            fields.Add(new EmbedFieldBuilder()
            {
                Name = "Id",
                Value = info.Id,
                IsInline = true
            });

            if (info.EpisodesCount.HasValue)
            {
                if (info.EpisodesCount > 0)
                {
                    fields.Add(new EmbedFieldBuilder()
                    {
                        Name = "Epizody",
                        Value = info.EpisodesCount,
                        IsInline = true
                    });
                }
            }

            fields.Add(new EmbedFieldBuilder()
            {
                Name = "Status",
                Value = info.Status.ToName(),
                IsInline = true
            });

            if (info.TotalRating.HasValue)
            {
                if (info.TotalRating > 0)
                {
                    fields.Add(new EmbedFieldBuilder()
                    {
                        Name = "Ocena ogólna",
                        Value = info.TotalRating.Value.ToString("0.0"),
                        IsInline = true
                    });
                }
            }
            return fields;
        }

        public static Embed ToEmbed(this IMangaTitleInfo info)
        {
            return new EmbedBuilder()
            {
                Title = info.Title.TrimToLength(EmbedBuilder.MaxTitleLength),
                Description = info.Description.Content.TrimToLength(1000),
                ThumbnailUrl = info.CoverUrl,
                Color = EMType.Info.Color(),
                Fields = info.GetFields(),
                Footer = info.GetFooter(),
                Url = info.MangaUrl,
            }.Build();
        }

        public static List<EmbedFieldBuilder> GetFields(this IMangaTitleInfo info)
        {
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            if (info.AlternativeTitles.Count > 0)
            {
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = "Tytuły alternatywne",
                    Value = string.Join(", ", info.AlternativeTitles).TrimToLength(EmbedFieldBuilder.MaxFieldValueLength),
                    IsInline = false
                });
            }

            foreach (var tagType in info.TagCategories)
            {
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = tagType.Name.TrimToLength(EmbedFieldBuilder.MaxFieldNameLength),
                    Value = string.Join(", ", tagType.Tags).TrimToLength(EmbedFieldBuilder.MaxFieldValueLength),
                    IsInline = false
                });
            }

            fields.Add(new EmbedFieldBuilder()
            {
                Name = "Typ",
                Value = info.Type.ToName(),
                IsInline = true
            });

            fields.Add(new EmbedFieldBuilder()
            {
                Name = "Id",
                Value = info.Id,
                IsInline = true
            });

            if (info.ChaptersCount.HasValue)
            {
                if (info.ChaptersCount > 0)
                {
                    fields.Add(new EmbedFieldBuilder()
                    {
                        Name = "Rozdziały",
                        Value = info.ChaptersCount,
                        IsInline = true
                    });
                }
            }

            fields.Add(new EmbedFieldBuilder()
            {
                Name = "Status",
                Value = info.Status.ToName(),
                IsInline = true
            });

            if (info.TotalRating.HasValue)
            {
                if (info.TotalRating > 0)
                {
                    fields.Add(new EmbedFieldBuilder()
                    {
                        Name = "Ocena ogólna",
                        Value = info.TotalRating.Value.ToString("0.0"),
                        IsInline = true
                    });
                }
            }
            
            return fields;
        }
    }
}