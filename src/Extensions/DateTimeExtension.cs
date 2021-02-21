#pragma warning disable 1591

namespace Sanakan.Extensions
{
    public static class DateTimeExtension
    {
        public static string ToShortDateTime(this System.DateTime date)
            => $"{date.ToShortDateString()} {date.ToShortTimeString()}";
    }
}
