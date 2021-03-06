#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Discord;
using Microsoft.AspNetCore.Mvc;

namespace Sanakan.Extensions
{
    public static class StringExtension
    {
        private static readonly string[] _bbCodes =
        {
            "list", "quote", "code", "spoiler", "chk", "size", "color", "bg", "center", "right",
            "left", "font", "align", "mail", "img", "small", "sub", "sup", "p", "gvideo", "bull",
            "copyright", "registered", "tm", "indent", "iframe", "url", "youtube", "i", "b", "s",
            "u", "color", "size"
        };

        public static EmbedBuilder ToEmbedMessage(this string message, EMType type = EMType.Neutral, bool icon = false)
        {
            return new EmbedBuilder().WithColor(type.Color()).WithDescription($"{type.Emoji(!icon)}{message}");
        }

        public static string TrimToLength(this string s, int length)
        {
            if (s == null) return "";
            if (s.Length <= length) return s;

            var charAr = s.ToCharArray();
            for (int i = 1; i < 4; i++) charAr[length - i] = '.';
            charAr[length] = '\0';

            return new String(charAr, 0, Array.IndexOf(charAr, '\0'));
        }

        public static string ConvertBBCodeToMarkdown(this string s)
        {
            s = s.Replace("[*]", "— ").Replace("[i]", "*").Replace("[/i]", "*").Replace("[b]", "**").Replace("[/b]", "**")
                .Replace("[u]", "__").Replace("[/u]", "__").Replace("[s]", "~~").Replace("[/s]", "~~")
                .Replace("[code]", "```").Replace("[/code]", "```").Replace("[youtube]", "https://www.youtube.com/watch?v=");

            s = new Regex(@"\[url=['""]?([^\['""]+)['""]?\]([^\[]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase).Replace(s, "[$2]($1)");

            return new Regex($@"\[/?({string.Join('|', _bbCodes)})(=[^\]]*)?\]", RegexOptions.Compiled | RegexOptions.IgnoreCase).Replace(s, "");
        }

        public static bool IsURLToImage(this string s)
        {
            var http = s.Split(':').FirstOrDefault();
            bool hasHttp = !(http == null || (!http.Equals("http") && !http.Equals("https")));

            bool hasRightExt = false;
            var ext = s.Split('.').LastOrDefault();
            var extensions = new string[] { "png", "jpg", "jpeg", "gif"};
            if (ext != null)
            {
                ext = ext.ToLower();
                hasRightExt = extensions.Any(x => x.Equals(ext));
            }

            return hasHttp && hasRightExt;
        }

        public static string GetQMarksIfEmpty(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return "??";

            if (string.IsNullOrWhiteSpace(s))
                return "??";

            return s;
        }

        public static int CountEmotesTextLenght(this IReadOnlyCollection<Discord.ITag> tags)
        {
            return tags.Where(tag => tag.Type == Discord.TagType.Emoji).Sum(x => x.Value.ToString().Length);
        }

        public static bool IsEmotikunEmote(this string message)
        {
            return new Regex(@"\B-\w+", RegexOptions.Compiled).Matches(message).Count > 0;
        }

        public static int CountQuotedTextLength(this string message)
        {
            return new Regex(@"(^>[ ][^\n]*\n)|(\n>[ ][^\n]*\n)|(\n>[ ][^\n]*$)", RegexOptions.Compiled).Matches(message).Sum(x => x.Length);
        }

        public static int CountLinkTextLength(this string message)
        {
            return new Regex("(http|ftp|https)://[\\w-]+(\\.[\\w-]+)+([\\w.,@?^=%&:/~+#-]*[\\w@?^=%&/~+#-])?", RegexOptions.Compiled).Matches(message).Sum(x => x.Length);
        }

        public static bool IsAColorInHEX(this string message)
        {
            return new Regex("^#(?:[0-9a-fA-F]{3}){1,2}$", RegexOptions.Compiled).IsMatch(message);
        }

        public static bool IsCommand(this string message, string prefix)
        {
            prefix = prefix.Replace(".", @"\.").Replace("?", @"\?");
            return new Regex($@"^{prefix}\w+", RegexOptions.Compiled).Matches(message).Count > 0;
        }

        public static IActionResult ToResponse(this string str, int Code = 200)
        {
            return new ObjectResult(new { message = str, success = (Code == 200) }) { StatusCode = Code };
        }

        public static IActionResult ToResponseRich(this string str, ulong msgId)
        {
            return new ObjectResult(new { message = str, success = true, id = msgId }) { StatusCode = 200 };
        }

        public static IActionResult ToResponseRich(this string str, List<ulong> msgId)
        {
            return new ObjectResult(new { message = str, success = true, ids = msgId }) { StatusCode = 200 };
        }
    }
}
