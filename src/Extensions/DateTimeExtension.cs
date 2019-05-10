namespace Sanakan.Extensions
{
    public static class DateTimeExtension
    {
        public static string ToShortDateTime(this System.DateTime date)
            => $"{date.ToShortDateString()} {date.ToLocalTime().ToShortTimeString()}";
    }
}
