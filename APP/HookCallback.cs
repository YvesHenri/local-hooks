using System;
using System.Runtime.InteropServices;

namespace APP
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam);
}
