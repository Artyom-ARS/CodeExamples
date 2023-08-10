using StructureMap;

namespace Common.DI
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
