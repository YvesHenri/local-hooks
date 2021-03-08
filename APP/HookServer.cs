using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace APP
{
    public class HookServer
    {
        public delegate IntPtr WindowCallback(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private readonly HookCallback hookCallback;
        private readonly WindowCallback windowCallback;

        public event EventHandler OnCreated;
        public event EventHandler OnDestroyed;

        public bool Running { get; private set; }
        public IntPtr Handle { get; private set; }

        public HookServer(HookCallback hookCallbackk)
        {
            Running = false;
            Handle = IntPtr.Zero;

            hookCallback = new HookCallback(hookCallbackk);
            windowCallback = new WindowCallback(WndProc);

            OnCreated += HookServer_OnCreated;
            OnDestroyed += HookServer_OnDestroyed;
        }

        private void HookServer_OnDestroyed(object sender, EventArgs e)
        {
            Console.WriteLine("Server destroyed!");
        }

        private void HookServer_OnCreated(object sender, EventArgs e)
        {
            Console.WriteLine("Server created!");
        }

        public bool Start()
        {
            if (!Running)
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

                    if (Running)
                    {
                        // Launch the message loop thread
                        Task.Run(() =>
                        {
                            Message message;

                            while (Running && GetMessage(out message, Handle, 0, 0) != 0)
                            {
                                TranslateMessage(ref message);
                                DispatchMessage(ref message);
                            }

                            OnDestroyed?.Invoke(this, EventArgs.Empty);
                            Console.WriteLine("End of message loop");
                        });

                        OnCreated?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            return Running;
        }

        public void Stop()
        {
            if (Running)
            {
                Running = false;
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam)
        {
            const int WM_COPYDATA = 0x004A;

            if (message == WM_COPYDATA)
            {
                var data = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);

                Console.WriteLine("{3} Hook message = {0} ({0:X2}), wParam = {1} ({1:X4}), lParam = {2} ({2:X8})", data.dwData, wParam.ToInt32(), data.lpData.ToInt32(), DateTime.Now.ToShortTimeString());

                return hookCallback.Invoke(data.dwData, wParam, data.lpData);
            }
            else
            {
                Console.WriteLine("{1} Window message: {0} ({0:X4})", message, DateTime.Now.ToShortTimeString());

                return DefWindowProc(hWnd, message, wParam, lParam);
            }
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

            public static uint Size = (uint) Marshal.SizeOf(typeof(WndClassEx));
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
    }
}
