#pragma warning disable 1591

using System.Collections.Generic;
using Shinden.Models;

namespace Sanakan.Extensions
{
    public static class IPictureExtension
    {
        public static string GetStr(this IPicture p)
        {
            if (p == null || p.Is18Plus)
                return null;

            return Shinden.API.Url.GetPersonPictureURL(p.PictureId);
        }

        public static List<string> GetPicList(this List<IPicture> ps)
        {
            var urls = new List<string>();
            if (ps == null) return urls;

            foreach(var p in ps)
            {
                var pic = p.GetStr();
                if (!string.IsNullOrEmpty(pic))
                    urls.Add(pic);
            }

            return urls;
        }
    }
}
