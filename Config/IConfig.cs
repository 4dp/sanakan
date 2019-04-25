namespace Sanakan.Config
{
    public interface IConfig
    {
        void Save();
        ConfigModel Get();
    }
}
