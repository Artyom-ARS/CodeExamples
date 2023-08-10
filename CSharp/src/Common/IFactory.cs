namespace Common
{
    public interface IFactory
    {
        T1 Get<T1, T2>()
            where T2 : T1, new();
    }
}
