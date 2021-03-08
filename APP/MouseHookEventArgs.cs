using System;
using System.Runtime.InteropServices;

namespace APP
{
    public class MouseHookEventArgs
    {
        public int X { get; }
        public int Y { get; }
        public uint Time { get; }
        public uint Button { get; }
        public bool Injected { get; }
        public bool InjectedFromLowLevel { get; }
        public int WheelDelta { get; }

        public MouseHookEventArgs(uint button, uint flags, uint time, int data, int x, int y)
        {
            const uint LLMHF_INJECTED = 0x00000001;
            const uint LLMHF_LOWER_IL_INJECTED = 0x00000002;

            X = x;
            Y = y;
            Time = time;
            Button = button;
            Injected = IsFlagSet(flags, LLMHF_INJECTED);
            InjectedFromLowLevel = IsFlagSet(flags, LLMHF_LOWER_IL_INJECTED);
            WheelDelta = data != 0 ? data >> 16 : 0;
        }

        private bool IsFlagSet(uint value, uint flag)
        {
            return (value & flag) == flag;
        }
    }
}
