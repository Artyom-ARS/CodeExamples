namespace Common.Facades
{
    public class Console : IConsole
    {
        public void Write(string input)
        {
            System.Console.Write(input);
        }

        public void Write(string input, params object[] args)
        {
            System.Console.Write(input, args);
        }

        public void WriteLine(string input)
        {
            System.Console.WriteLine(input);
        }

        public void WriteLine(string input, params object[] args)
        {
            System.Console.WriteLine(input, args);
        }

        public string ReadLine()
        {
            return System.Console.ReadLine();
        }

        public void ReadKey()
        {
            System.Console.ReadKey();
        }

        public bool KeyAvailable()
        {
            return System.Console.KeyAvailable;
        }

        public void Clear()
        {
            System.Console.Clear();
        }
    }
}
