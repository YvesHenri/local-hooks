using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

namespace APP
{
    public class HookServer : IDisposable
    {
        public delegate IntPtr WindowCallback(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private readonly TaskFactory taskFactory;
        private readonly CancellationTokenSource cts;
        private readonly HookCallback hookCallback;
        private readonly WindowCallback windowCallback;

        public bool Running { get; private set; }
        public IntPtr Handle { get; private set; }

        public event EventHandler OnStarted;
        public event EventHandler OnShutdown;

        public HookServer(HookCallback hulkCallback)
        {
            Running = false;
            Handle = IntPtr.Zero;

            cts = new CancellationTokenSource();
            taskFactory = new TaskFactory(cts.Token);
            hookCallback = new HookCallback(hulkCallback);
            windowCallback = new WindowCallback(WndProc);
        }

        public bool Start()
        {
            if (!Running)
            {
                var readyEvent = new ManualResetEventSlim();

                taskFactory.StartNew(() =>
                {
                    var processHandle = Process.GetCurrentProcess().Handle;                

                    var windowClass = new WndClassEx
                    {
                        lpszMenuName = null,
                        hInstance = processHandle,
                        cbSize = WndClassEx.Size,
                        lpfnWndProc = windowCallback,
                        lpszClassName = Guid.NewGuid().ToString()
                    };

                    // Register the dummy window class
                    var classAtom = RegisterClassEx(ref windowClass);

                    // Check whether the class was registered successfully
                    if (classAtom != 0u)
                    {
                        // Create the dummy window
                        Handle = CreateWindowEx(0x08000000, classAtom, "", 0, -1, -1, -1, -1, IntPtr.Zero, IntPtr.Zero, processHandle, IntPtr.Zero);
                        Running = Handle != IntPtr.Zero;

                        // Unblock calling thread as everything is ready to go
                        readyEvent.Set();

                        // If window has been created, initialize the message window loop
                        if (Running)
                        {
                            Message message = new Message();

                            OnStarted?.Invoke(this, EventArgs.Empty);

                            while (GetMessage(out message, IntPtr.Zero, 0, 0) != 0)
                            {
                                TranslateMessage(ref message);
                                DispatchMessage(ref message);
                            }

                            OnShutdown?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        // Failed to create the dummy window class. Unblock calling thread
                        readyEvent.Set();
                    }
                });

                // Block calling thread untill we have the window creation result
                readyEvent.Wait();
            }

            return Running;
        }

        public void Stop()
        {
            if (Running)
            {
                const int WM_CLOSE = 16;

                Running = false;

                SendMessage(Handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }

            cts.Cancel();
        }

        public void Dispose()
        {
            Stop();
        }

        private IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
        {
            const int WM_NULL = 0;
            const int WM_DESTROY = 2;
            const int WM_COPYDATA = 0x004A;

            switch (message)
            {
                case WM_COPYDATA:
                    var data = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
                    Debug.WriteLine("{3} Hook message = {0} ({0:X2}), wParam = {1} ({1:X4}), lParam = {2} ({2:X8})", data.dwData, wParam.ToInt32(), data.lpData.ToInt32(), DateTime.Now.ToShortTimeString());
                    return hookCallback.Invoke(data.dwData, wParam, data.lpData);
                case WM_DESTROY:
                    PostQuitMessage(0);
                    return IntPtr.Zero;
                default:
                    Debug.WriteLine("{0} Window message = {1} ({1:X2})", DateTime.Now.ToShortTimeString(), message);
                    break;
            }

            return DefWindowProc(hWnd, message, wParam, lParam);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Message
        {
            IntPtr hwnd;
            uint message;
            UIntPtr wParam;
            IntPtr lParam;
            int time;
            Point pt;
            int lPrivate;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WndClassEx
        {
            public uint cbSize;
            public uint style;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public WindowCallback lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;

            public static uint Size = (uint)Marshal.SizeOf(typeof(WndClassEx));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public int dwData;
            public int cbData;
            public IntPtr lpData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public int x;
            public int y;
            public int data;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref Message lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref Message lpmsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.U2)]
        private static extern ushort RegisterClassEx([In] ref WndClassEx lpwcx);

        [DllImport("user32.dll")]
        private static extern int GetMessage(out Message lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(uint exStyle, ushort classAtom, string title, uint style, int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr lpParam);
    }
}
