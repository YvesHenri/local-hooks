using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace APP
{
    public sealed class KeyboardHook : Hook
    {
        private readonly HashSet<uint> downKeys;
        private readonly KeyboardHookSettings settings;

        private const int WM_KEYUP = 0x0101;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_SYSKEYDOWN = 0x0104;

        [StructLayout(LayoutKind.Sequential)]
        public class KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class KBDHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint keyCount;
            public bool extended;
            public bool alt;
            public bool wasDown;
            public bool transition;
        }

        protected override int LocalHookId => 2; // WH_KEYBOARD
        protected override int GlobalHookId => 13; // WH_KEYBOARD_LL

        public event KeyboardHookEvent OnHook;
        public event KeyboardHookEvent OnHold;
        public event KeyboardHookEvent OnRelease;
        public event KeyboardHookEvent OnPress;

        public KeyboardHook() : this(new KeyboardHookSettings())
        {
        }

        public KeyboardHook(KeyboardHookSettings keyboardHookSettings)
        {
            downKeys = new HashSet<uint>();
            settings = keyboardHookSettings;
        }

        protected override IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var kdb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                if (id == GlobalHookId)
                {
                    var keyboardEventArgs = new KeyboardHookEventArgs(kdb.vkCode, kdb.scanCode, kdb.time, kdb.flags);

                    switch (wParam.ToInt32())
                    {
                        case WM_KEYDOWN:
                        case WM_SYSKEYDOWN:
                            HandleKeyDownHookEvent(keyboardEventArgs);
                            break;
                        case WM_KEYUP:
                        case WM_SYSKEYUP:
                            HandleKeyReleasedHookEvent(keyboardEventArgs);
                            break;
                    }

                    // Trap key chain if it has been handled
                    if (keyboardEventArgs.Handled)
                    {
                        return new IntPtr(1);
                    }
                }
            }

            return WinAPI.CallNextHookEx(instance, nCode, wParam, lParam);
        }

        private void HandleKeyDownHookEvent(KeyboardHookEventArgs args)
        {
            OnHook?.Invoke(args);

            lock (downKeys)
            {
                // Save button to prevent multiple events and also to recognize successfull key presses (down and up)
                bool added = downKeys.Add(args.KeyCode);

                if (added)
                {
                    OnHold?.Invoke(args); // First time
                }
                else
                {
                    if (!settings.HookHoldEventOnce)
                    {
                        OnHold?.Invoke(args);
                    }
                }
            }
        }

        private void HandleKeyReleasedHookEvent(KeyboardHookEventArgs args)
        {
            OnHook?.Invoke(args);

            lock (downKeys)
            {
                bool removed = downKeys.Remove(args.KeyCode);

                OnRelease?.Invoke(args);

                if (removed)
                {
                    OnPress?.Invoke(args);
                }
            }
        }
    }
}
