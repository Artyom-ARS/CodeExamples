using StructureMap;

namespace FxConnect.DI
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
