namespace Common.Providers
{
    public interface IConfigurationProvider
    {
        T GetConfiguration<T>();

        string GetConfigurationPath();
    }
}
