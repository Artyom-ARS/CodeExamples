using Common.Facades;

namespace Common.Decorators
{
    public class ConsoleDecorator : IConsoleDecorator
    {
        private const char Block = '■';
        private const string Back = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b";
        private const string Twirl = "-\\|/";

        private readonly IConsole _console;

        public ConsoleDecorator(IConsole console)
        {
            _console = console;
        }

        public void WriteProgressBar(int percent, bool update = false)
        {
            if (update)
            {
                _console.Write(Back);
            }

            _console.Write("[");
            var p = (int)((percent / 10f) + .5f);
            for (var i = 0; i < 10; ++i)
            {
                _console.Write(i >= p ? ' '.ToString() : Block.ToString());
            }

            _console.Write("] {0,3:##0}%", percent);
        }

        public void WriteProgress(int progress, bool update = false)
        {
            if (update)
            {
                _console.Write("\b");
            }

            _console.Write(Twirl[progress % Twirl.Length].ToString());
        }
    }
}
