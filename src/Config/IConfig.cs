#pragma warning disable 1591

using Sanakan.Config.Model;

namespace Sanakan.Config
{
    public interface IConfig
    {
        void Save();
        ConfigModel Get();
    }
}
