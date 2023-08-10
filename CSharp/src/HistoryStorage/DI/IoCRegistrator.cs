using StructureMap;

namespace HistoryStorage.DI
{
    public class IoCRegistrator
    {
        public static Container RegisterContainer()
        {
            Container container = new Container(x =>
            {
                x.Scan(s =>
                {
                    s.TheCallingAssembly();
                    s.LookForRegistries();
                    s.AssembliesFromApplicationBaseDirectory();
                    s.WithDefaultConventions();
                });
            });
            return container;
        }
    }
}
