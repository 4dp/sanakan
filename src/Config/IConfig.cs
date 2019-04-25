#pragma warning disable 1591

namespace Sanakan.Config
{
    public interface IConfig
    {
        void Save();
        ConfigModel Get();
    }
}
