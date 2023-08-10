namespace Common.Facades
{
    public interface IConsole
    {
        void Write(string input);

        void Write(string input, params object[] args);

        void WriteLine(string input);

        void WriteLine(string input, params object[] args);

        string ReadLine();

        void ReadKey();

        bool KeyAvailable();

        void Clear();
    }
}
