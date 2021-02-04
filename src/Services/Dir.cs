#pragma warning disable 1591

using System.IO;

namespace Sanakan.Services
{
    public static class Dir
    {
        private const string BaseOutput = "../GOut";

        public static void Create()
        {
            Directory.CreateDirectory(CardsMiniatures);
            Directory.CreateDirectory(CardsInProfiles);
            Directory.CreateDirectory(SavedData);
            Directory.CreateDirectory(Profiles);
        }

        public static string Cards = $"{BaseOutput}/Cards";
        public static string CardsMiniatures = $"{Cards}/Small";
        public static string CardsInProfiles = $"{Cards}/Profile";

        public static string SavedData = $"{BaseOutput}/Saved";
        public static string Profiles = $"{BaseOutput}/Profile";
    }
}