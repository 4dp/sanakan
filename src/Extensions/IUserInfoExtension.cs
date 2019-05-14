#pragma warning disable 1591

using Shinden.Models;

namespace Sanakan.Extensions
{
    public static class IUserInfoExtension
    {
        public static MoreSeriesStatus GetMoreSeriesStats(this IUserInfo info, bool manga = false)
        {
            return new MoreSeriesStatus()
            {
                Time = manga ? info?.ListStats?.MangaTime : info?.ListStats?.AnimeTime,
                Score = manga ? info?.ListStats?.MangaMeanScore : info?.ListStats?.AnimeMeanScore,
                Count = manga ? info?.ListStats?.ChaptersCount : info?.ListStats?.EpisodesCount
            };
        }
    }
}
