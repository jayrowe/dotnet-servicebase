using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DotNetServiceBase
{
    public class WindowsServiceRunner : IServiceRunner
    {
        private const int SERVICE_STOPPED = 0x00000001;
        private const int SERVICE_START_PENDING = 0x00000002;
        private const int SERVICE_STOP_PENDING = 0x00000003;
        private const int SERVICE_RUNNING = 0x00000004;
        private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;

        private const int SERVICE_ACCEPT_STOP = 0x00000001;
        private const int SERVICE_ACCEPT_SHUTDOWN = 0x00000004;

        private const int SERVICE_CONTROL_STOP = 0x00000001;
        private const int SERVICE_CONTROL_INTERROGATE = 0x00000004;
        private const int SERVICE_CONTROL_SHUTDOWN = 0x00000005;

        private readonly string _serviceName;
        private readonly ThreadStart _onStart;
        private readonly ThreadStart _onStop;


        private SERVICE_STATUS _status = new SERVICE_STATUS();
        private IntPtr _statusHandle;

        private IntPtr _pServiceName;

        private ServiceControlCallbackEx _commandCallback;
        private ServiceMainCallback _mainCallback;

        public WindowsServiceRunner(string serviceName, ThreadStart onStart, ThreadStart onStop)
        {
            _serviceName = serviceName;
            _onStart = onStart;
            _onStop = onStop;

            _status = new SERVICE_STATUS
            {
                controlsAccepted = SERVICE_ACCEPT_STOP | SERVICE_ACCEPT_SHUTDOWN,
                currentState = SERVICE_START_PENDING,
                win32ExitCode = 0,
                serviceSpecificExitCode = 0,
                checkPoint = 0,
                waitHint = 0,
                serviceType = SERVICE_WIN32_OWN_PROCESS,
            };

            _mainCallback = new ServiceMainCallback(ServiceMain);
            _commandCallback = new ServiceControlCallbackEx(ServiceCommandCallbackEx);

            _serviceName = serviceName;
        }

        public bool TryRun()
        {
            var serviceTableEntry = IntPtr.Zero;

            try
            {
                _pServiceName = Marshal.StringToHGlobalUni(_serviceName);
                serviceTableEntry = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SERVICE_TABLE_ENTRY)));

                Marshal.StructureToPtr(
                    // this is the service table entry for the actual service
                    new SERVICE_TABLE_ENTRY(_pServiceName, _mainCallback),
                    serviceTableEntry,
                    true);

                if (StartServiceCtrlDispatcher(serviceTableEntry))
                {
                    return true;
                }
            }
            catch
            {
            }
            finally
            {
                if (serviceTableEntry != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(serviceTableEntry);
                }

                if (_pServiceName != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_pServiceName);
                }
            }

            return false;
        }

        private void AsyncStop(object ignored) => Stop();

        public void Stop()
        {
            int currentState = _status.currentState;

            UpdateStatus(SERVICE_STOP_PENDING);

            try
            {
                _onStop();

                UpdateStatus(SERVICE_STOPPED);
            }
            catch
            {
                UpdateStatus(currentState);

                throw;
            }
        }

        private int ServiceCommandCallbackEx(int command, int eventType, IntPtr eventData, IntPtr eventContext)
        {
            if (command == SERVICE_CONTROL_INTERROGATE)
            {
                UpdateStatus(_status.currentState);
            }

            if ((_status.currentState == SERVICE_RUNNING) && (command == SERVICE_CONTROL_STOP || command == SERVICE_CONTROL_SHUTDOWN))
            {
                UpdateStatus(SERVICE_STOP_PENDING);

                ThreadPool.QueueUserWorkItem(AsyncStop);
            }
            return 0;
        }

        private void ServiceMain(int argCount, IntPtr argPointer)
        {
            _statusHandle = RegisterServiceCtrlHandlerEx(_pServiceName, _commandCallback, IntPtr.Zero);

            if (_statusHandle == IntPtr.Zero || !UpdateStatus(_status.currentState))
            {
                return;
            }

            try
            {
                _onStart();
                _status.checkPoint = 0;
                _status.waitHint = 0;

                if (!UpdateStatus(SERVICE_RUNNING))
                {
                    UpdateStatus(SERVICE_STOPPED);
                }
            }
            catch
            {
                UpdateStatus(SERVICE_STOPPED);
            }
        }

        private bool UpdateStatus(int status)
        {
            _status.currentState = status;
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(SERVICE_STATUS)));
            Marshal.StructureToPtr(_status, ptr, true);
            return SetServiceStatus(_statusHandle, ptr);
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr serviceStatusHandle, IntPtr status);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool StartServiceCtrlDispatcher(IntPtr entries);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr RegisterServiceCtrlHandlerEx(IntPtr serviceName, Delegate callback, IntPtr userData);

        private struct SERVICE_STATUS
        {
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SERVICE_TABLE_ENTRY
        {
            public SERVICE_TABLE_ENTRY(IntPtr name, Delegate callback)
            {
                this.name = name;
                this.callback = callback;
                endName = IntPtr.Zero;
                endCallback = IntPtr.Zero;
            }

            public IntPtr name;
            public Delegate callback;
            // we're cheating here - this is designed to look like a null SERVICE_TABLE_ENTRY struct
            private IntPtr endName;
            private IntPtr endCallback;
        }

        private delegate int ServiceControlCallbackEx(int control, int eventType, IntPtr eventData, IntPtr eventContext);
        private delegate void ServiceMainCallback(int argCount, IntPtr argPointer);

    }
}
