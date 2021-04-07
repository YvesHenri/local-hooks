using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace APP
{
    public sealed class MouseHook : Hook
    {
        private readonly HashSet<uint> downButtons = new HashSet<uint>();

        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;

        private const uint MK_NONE = 0x0000;
        private const uint MK_LBUTTON = 0x0001;
        private const uint MK_MBUTTON = 0x0010;
        private const uint MK_RBUTTON = 0x0002;
        // private const uint MK_XBUTTON1 = 0x0020;
        // private const uint MK_XBUTTON2 = 0x0040;

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

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEHOOKSTRUCT
        {
            public int x;
            public int y;
            public IntPtr hwnd;
            public uint wHitTestCode;
            public IntPtr dwExtraInfo;
        }

        public event MouseHookEvent OnHook;
        public event MouseHookEvent OnMove;
        public event MouseHookEvent OnHold;
        public event MouseHookEvent OnRelease;
        public event MouseHookEvent OnClick;
        public event MouseHookEvent OnWheel;

        protected override int LocalHookId => 7; // WH_MOUSE
        protected override int GlobalHookId => 14; //  WH_MOUSE_LL

        protected override IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (HookMode.Local.Equals(Mode))
                {
                    var ms = Marshal.PtrToStructure<MOUSEHOOKSTRUCT>(lParam);

                    // Console.WriteLine("{0}, {1}", ms.x, ms.y);
                }

                if (HookMode.Global.Equals(Mode))
                {
                    var ms = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                    switch (wParam.ToInt32())
                    {
                        case WM_MOUSEMOVE:
                            OnMove?.Invoke(new MouseHookEventArgs(MK_NONE, ms.flags, ms.time, ms.data, ms.x, ms.y));
                            break;
                        case WM_MOUSEWHEEL:
                            OnWheel?.Invoke(new MouseHookEventArgs(MK_NONE, ms.flags, ms.time, ms.data, ms.x, ms.y));
                            break;
                        case WM_MBUTTONDOWN:
                            HandleKeyDownHookEvent(new MouseHookEventArgs(MK_MBUTTON, ms.flags, ms.time, ms.data, ms.x, ms.y));
                            break;
                        case WM_LBUTTONDOWN:
                            HandleKeyDownHookEvent(new MouseHookEventArgs(MK_LBUTTON, ms.flags, ms.time, ms.data, ms.x, ms.y));
                            break;
                        case WM_RBUTTONDOWN:
                            HandleKeyDownHookEvent(new MouseHookEventArgs(MK_RBUTTON, ms.flags, ms.time, ms.data, ms.x, ms.y));
                            break;
                        case WM_MBUTTONUP:
                            HandleKeyReleasedHookEvent(new MouseHookEventArgs(MK_MBUTTON, ms.flags, ms.time, ms.data, ms.x, ms.y));
                            break;
                        case WM_LBUTTONUP:
                            HandleKeyReleasedHookEvent(new MouseHookEventArgs(MK_LBUTTON, ms.flags, ms.time, ms.data, ms.x, ms.y));
                            break;
                        case WM_RBUTTONUP:
                            HandleKeyReleasedHookEvent(new MouseHookEventArgs(MK_RBUTTON, ms.flags, ms.time, ms.data, ms.x, ms.y));
                            break;
                    }
                }
            }

            return WinAPI.CallNextHookEx(instance, nCode, wParam, lParam);
        }

        private void HandleKeyDownHookEvent(MouseHookEventArgs args)
        {
            OnHook?.Invoke(args);

            lock (downButtons)
            {
                // Save button to recognize successfull key presses (down and up)
                bool added = downButtons.Add(args.Button);

                if (added)
                {
                    OnHold?.Invoke(args);
                }
            }
        }

        private void HandleKeyReleasedHookEvent(MouseHookEventArgs args)
        {
            OnHook?.Invoke(args);

            lock (downButtons)
            {
                bool removed = downButtons.Remove(args.Button);

                OnRelease?.Invoke(args);

                if (removed)
                {
                    OnClick?.Invoke(args);
                }
            }
        }
    }
}
