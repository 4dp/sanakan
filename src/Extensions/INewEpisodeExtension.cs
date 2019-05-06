using System.Collections.Generic;
using Discord;
using Shinden.Models;

namespace Sanakan.Extensions
{
    public static class INewEpisodeExtension
    {
        public static Embed ToEmbed(this INewEpisode ep)
        {
            return new EmbedBuilder()
            {
                Title = ep.AnimeTitle.TrimToLength(EmbedBuilder.MaxTitleLength),
                ThumbnailUrl = ep.AnimeCoverUrl,
                Color = EMType.Info.Color(),
                Fields = ep.GetFields(),
                Url = ep.AnimeUrl,
            }.Build();
        }

        public static string ToName(this Language lang)
        {
            switch (lang)
            {
                case Language.English: return "Angielski";
                case Language.Korean: return "Koreański";
                case Language.Chinese: return "Chiński";
                case Language.Polish: return "Polski";

                case Language.NotSpecified:
                default: return "--";
            }
        }

        public static List<EmbedFieldBuilder> GetFields(this INewEpisode ep)
        {
            return new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    Name = "Numer epizodu",
                    Value = ep.EpisodeNumber,
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                     Name = "Czas trwania",
                     Value = ep.EpisodeLength,
                     IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Język napisów",
                    Value = ep.SubtitlesLanguage.ToName(),
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Link",
                    Value = ep.EpisodeUrl,
                    IsInline = false
                },
                new EmbedFieldBuilder
                {
                    Name = "Data dodania",
                    Value = ep.AddDate.ToShortDateString(),
                    IsInline = false
                }
            };
        }
    }
}