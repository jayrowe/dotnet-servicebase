using System;
using System.Threading;

namespace DotNetServiceBase
{
    public class ConsoleServiceRunner : IServiceRunner
    {
        private readonly string _serviceName;
        private readonly ThreadStart _onStart;
        private readonly ThreadStart _onStop;
        private ManualResetEvent _stopEvent;

        public ConsoleServiceRunner(string serviceName, ThreadStart onStart, ThreadStart onStop)
        {
            _serviceName = serviceName;
            _onStart = onStart;
            _onStop = onStop;
            _stopEvent = new ManualResetEvent(false);
        }

        public bool TryRun()
        {
            _onStart();

            Console.CancelKeyPress += (s, e) => _stopEvent.Set();
            _stopEvent.WaitOne();

            _onStop();

            return true;
        }

        public void Stop()
        {
            _stopEvent.Set();
        }
    }
}