namespace DotNetServiceBase
{
    public interface IServiceRunner
    {
        bool TryRun();
        void Stop();
    }
}
