using fxcore2;

namespace FxConnect.Facades
{
    public interface ITableManagerFacade
    {
        O2GTableManagerStatus GetStatus(O2GTableManager tableManager);

        T GetTable<T>(O2GTableManager tableManager, O2GTableType tableType)
            where T : O2GTable;
    }
}
