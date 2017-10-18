using System;
using System.Collections.Generic;

namespace DotNetServiceBase
{
    public class DotNetServiceBase : IDisposable
    {
        private const int MAX_SERVICE_NAME_LENGTH = 80;
        private readonly string _serviceName;
        private IServiceRunner _runner;

        public DotNetServiceBase(string serviceName)
        {
            if (!ValidServiceName(serviceName))
            {
                throw new ArgumentException("Invalid service name");
            }

            _serviceName = serviceName;
        }

        protected virtual void OnStart() { }

        protected virtual void OnStop() { }

        public void Run()
        {
            foreach (var runner in GetRunners())
            {
                _runner = runner;

                if (runner.TryRun())
                {
                    return;
                }
            }
        }

        private IEnumerable<IServiceRunner> GetRunners()
        {
            yield return new WindowsServiceRunner(_serviceName, OnStart, OnStop);
            yield return new ConsoleServiceRunner(_serviceName, OnStart, OnStop);
        }

        private static bool ValidServiceName(string serviceName)
        {
            return serviceName != null &&
                serviceName.Length <= MAX_SERVICE_NAME_LENGTH &&
                serviceName.Length > 0 &&
                !serviceName.Contains("\\") &&
                !serviceName.Contains("/");
        }

        #region IDisposable Support
        private bool _disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _runner?.Stop();
                    }
                    catch
                    {
                        // tried our best and failed
                    }
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}