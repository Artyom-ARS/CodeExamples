using fxcore2;

namespace FxConnect.Facades
{
    public class TableManagerFacade : ITableManagerFacade
    {
        public O2GTableManagerStatus GetStatus(O2GTableManager tableManager)
        {
            return tableManager.getStatus();
        }

        public T GetTable<T>(O2GTableManager tableManager, O2GTableType tableType)
            where T : O2GTable
        {
            return (T)tableManager.getTable(tableType);
        }
    }
}
