using System;
using System.Runtime.InteropServices;

namespace APP
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HookRequest
    {
        public int Code;
        public IntPtr WParam;
        public IntPtr LParam;
    }
}
