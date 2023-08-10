namespace Common
{
    public class Factory : IFactory
    {
        public T1 Get<T1, T2>()
            where T2 : T1, new()
        {
            return new T2();
        }
    }
}
