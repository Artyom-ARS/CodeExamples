namespace Common.Decorators
{
    public interface IConsoleDecorator
    {
        void WriteProgressBar(int percent, bool update = false);

        void WriteProgress(int progress, bool update = false);
    }
}
