#pragma warning disable 1591

using System.Collections.Generic;
using Shinden.Models;
using Discord;

namespace Sanakan.Extensions
{
    public static class ICharacterInfoExtension
    {
        public static Embed ToEmbed(this ICharacterInfo info)
        {
            return new EmbedBuilder()
            {
                Title = $"{info} ({info.Id})".TrimToLength(EmbedBuilder.MaxTitleLength),
                Description = info?.Biography?.Content?.TrimToLength(1000),
                Color = EMType.Info.Color(),
                ImageUrl = info.PictureUrl,
                Fields = info.GetFields(),
                Url = info.CharacterUrl,
            }.Build();
        }

        public static List<EmbedFieldBuilder> GetFields(this ICharacterInfo info)
        {
            var fields = new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder
                {
                    Name = "Płeć",
                    Value = info.Gender.ToModel(),
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Wiek",
                    Value = info.Age.GetQMarksIfEmpty(),
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Wzrost",
                    Value = info.Height.GetQMarksIfEmpty(),
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Waga",
                    Value = info.Weight.GetQMarksIfEmpty(),
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Grupa krwii",
                    Value = info.Bloodtype.GetQMarksIfEmpty(),
                    IsInline = true
                },
                new EmbedFieldBuilder
                {
                    Name = "Historyczna",
                    Value = info.IsReal ? "Tak" : "Nie",
                    IsInline = true
                }
            };

            if (info.Gender == Sex.Female)
            {
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = "Biust",
                    Value = info.Bust.GetQMarksIfEmpty(),
                    IsInline = true
                });

                fields.Add(new EmbedFieldBuilder()
                {
                    Name = "Talia",
                    Value = info.Waist.GetQMarksIfEmpty(),
                    IsInline = true
                });

                fields.Add(new EmbedFieldBuilder()
                {
                    Name = "Biodra",
                    Value = info.Hips.GetQMarksIfEmpty(),
                    IsInline = true
                });
            }

            return fields;
        }

        public static string ToModel(this Sex s)
        {
            switch(s)
            {
                case Sex.Other: return "Helikopter bojowy";
                case Sex.Female: return "Kobieta";
                case Sex.Male: return "Mężczyzna";
                default: return "Homoniewiadomo";
            }
        }
    }
}