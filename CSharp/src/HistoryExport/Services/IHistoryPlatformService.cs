using System.Threading.Tasks;

namespace HistoryExport.Services
{
    public interface IHistoryExportService
    {
        Task<bool> Start();
    }
}
