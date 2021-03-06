using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APP
{
    public class KeyboardHookEventArgs
    {
        public uint Time { get; }
        public uint KeyCode { get; }
        public uint ScanCode { get; }
        public bool Handled { get; set; }
        public bool Injected { get; }
        public bool InjectedFromLowLevel { get; }
        public bool Extended { get; }
        public bool Alt { get; }

        public KeyboardHookEventArgs(uint keyCode, uint scanCode, uint time, uint flags)
        {
            const uint LLKHF_INJECTED = 0x00000010;
            const uint LLKHF_LOWER_IL_INJECTED = 0x00000002;
            const uint LLKHF_EXTENDED = 0x00000001;
            const uint LLKHF_ALTDOWN = 0x00000020;
            // const uint LLKHF_UP = 0x00000080;

            Time = time;
            KeyCode = keyCode;
            ScanCode = scanCode;
            Handled = false;
            Alt = IsFlagSet(flags, LLKHF_ALTDOWN);
            Extended = IsFlagSet(flags, LLKHF_EXTENDED);
            Injected = IsFlagSet(flags, LLKHF_INJECTED);
            InjectedFromLowLevel = IsFlagSet(flags, LLKHF_LOWER_IL_INJECTED);
        }

        private bool IsFlagSet(uint value, uint flag)
        {
            return (value & flag) == flag;
        }
    }
}
