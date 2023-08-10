using Common.Experts;

namespace HistoryPlatform.Factories
{
    public interface IExpertFactory
    {
        IExpert Switcher(string expertName);
    }
}
