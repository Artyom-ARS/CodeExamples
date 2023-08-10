using StructureMap;

namespace AzureStorage.DI
{
    public class Registries : Registry
    {
        public Registries()
        {
            Scan(s =>
            {
                s.TheCallingAssembly();
                s.WithDefaultConventions();
            });
        }
    }
}
