#pragma warning disable 1591

using Discord;

namespace Sanakan.Extensions
{
    public enum EMType
    {
        Neutral, Warning, Success, Error, Info, Bot
    }

    public static class EMTypeExtension
    {
        public static string Emoji(this EMType type, bool hide = false)
        {
            if (hide) return "";

            switch (type)
            {
                case EMType.Bot:
                    return "🐙";

                case EMType.Info:
                    return "ℹ";

                case EMType.Error:
                    return "🚫";

                case EMType.Success:
                    return "✅";

                case EMType.Warning:
                    return "⚠";

                default:
                case EMType.Neutral:
                    return "";
            }
        }

        public static Color Color(this EMType type)
        {
            switch (type)
            {
                case EMType.Bot:
                    return new Color(158, 62, 211);

                case EMType.Error:
                    return new Color(255, 0, 0);

                case EMType.Info:
                    return new Color(0, 122, 204);

                case EMType.Success:
                    return new Color(51, 255, 51);

                case EMType.Warning:
                    return new Color(255, 255, 0);

                default:
                case EMType.Neutral:
                    return new Color(128, 128, 128);
            }
        }
    }
}
