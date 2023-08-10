using HistoryPlatform.Providers;
using PlatformCommon.Providers;
using StructureMap;

namespace HistoryPlatform.DI
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

                x.For<IHistoryPricesProvider>().Use<HistoryPricesProvider>();
            });
            return container;
        }
    }
}
