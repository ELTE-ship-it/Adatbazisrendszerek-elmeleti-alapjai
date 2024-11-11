
namespace Protocol
{
    public abstract class Processor<T>
    {
        public abstract T Process();
        public abstract int GetMakespan();
    }
}
