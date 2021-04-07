using System;
using System.Linq;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace APP
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("Kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool IsWow64Process([In] IntPtr process, [Out, MarshalAs(UnmanagedType.Bool)] out bool isWow64Process);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get { return Left; }
                set { Right -= (Left - value); Left = value; }
            }

            public int Y
            {
                get { return Top; }
                set { Bottom -= (Top - value); Top = value; }
            }

            public int Height
            {
                get { return Bottom - Top; }
                set { Bottom = value + Top; }
            }

            public int Width
            {
                get { return Right - Left; }
                set { Right = value + Left; }
            }

            public Point Location
            {
                get { return new Point(Left, Top); }
                set { X = value.X; Y = value.Y; }
            }

            public Size Size
            {
                get { return new Size(Width, Height); }
                set { Width = value.Width; Height = value.Height; }
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void OnFormShown(object sender, EventArgs e)
        {
            // var mouseHook = HookService.Mouse.Install();
            // var keyboardHook = HookService.Keyboard.Install();

            // mouseHook.OnMove += OnGlobalMouseMove;
            // keyboardHook.OnPress += OnKeyPressed;

            label1.Text = string.Format("Virtual screen size: {0} x {1}", SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
        }

        private void OnKeyPressed(KeyboardHookEventArgs args)
        {
            // 0 - Mana: first right panel, first bp
            if (args.KeyCode == 48)
            {
                Sender.Use(3358, 132, 2624, 454);
            }

            // R - SD: first right panel, second bp
            if (args.KeyCode == 82)
            {
                Sender.Click(true, 3358, 206);
            }

            // T - MW: first right panel, third bp
            if (args.KeyCode == 84)
            {
                Sender.Click(true, 3358, 280);
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            HookService.Destroy();
        }

        private void OnGlobalMouseMove(MouseHookEventArgs args)
        {
            label2.Text = string.Format("Current mouse coords: {0}, {1}", args.X, args.Y);
            label3.Text = string.Format("Absolute mouse coords: {0}, {1}", Sender.AbsoluteX(args.X), Sender.AbsoluteY(args.Y));
        }

        private void OnLocalMouseMove(MouseHookEventArgs args)
        {
            label4.Text = string.Format("Window mouse coords: {0}, {1}", args.X, args.Y);
        }

        private void OnComboboxDropDown(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();

            foreach (var process in Process.GetProcesses())
            {
                if (process.MainWindowTitle.Length > 0)
                {
                    comboBox1.Items.Add(process);
                }
            }
        }

        private void OnLeftButtonClick(object sender, EventArgs e)
        {
            var x = int.Parse(textBox1.Text);
            var y = int.Parse(textBox2.Text);

            Sender.Click(false, x, y);
        }

        private void OnRightButtonClick(object sender, EventArgs e)
        {
            var x = int.Parse(textBox1.Text);
            var y = int.Parse(textBox2.Text);

            Sender.Click(true, x, y);
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            var process = comboBox1.SelectedItem as Process;

            if (process != null)
            {
                GetClientRect(process.MainWindowHandle, out RECT client);
                GetWindowRect(process.MainWindowHandle, out RECT window);

                label5.Text = string.Format("Window position: {0}, {1}", window.X, window.Y);
                label6.Text = string.Format("Window size: {0}, {1}", window.Width, window.Height);
                label7.Text = string.Format("Window client size: {0}, {1}", client.Width, client.Height);
                label8.Text = string.Format("Process 64 bits: {0}", IsProcess64Bits(process));

                listBox1.Items.Clear();

                foreach (ProcessModule module in process.Modules)
                {
                    listBox1.Items.Add(module.FileName);
                }
            }
        }

        private bool IsProcess64Bits(Process process)
        {
            IsWow64Process(process.Handle, out bool isProcess32Bits);

            return Environment.Is64BitOperatingSystem && !isProcess32Bits;
        }

        private void OnInjectButtonClick(object sender, EventArgs e)
        {
            var process = comboBox1.SelectedItem as Process;

            if (process != null)
            {
                var localInstance = HookService.Mouse.Install(process);

                localInstance.OnMove += OnLocalMouseMove;

                if (localInstance.Installed)
                {
                    Process p = Process.GetProcessById(process.Id);

                    listBox1.Items.Clear();

                    foreach (ProcessModule module in p.Modules)
                    {
                        listBox1.Items.Add(module.FileName);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to inject mouse hook!");
                }
            }
        }

        private void OnEjectButtonClick(object sender, EventArgs e)
        {
            var process = comboBox1.SelectedItem as Process;

            if (process != null)
            {
                HookService.Mouse.Uninstall(process);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var imperianic = Process.GetProcessesByName("Imperianic DirectX9").FirstOrDefault();

            if (imperianic != null)
            {
                System.Threading.Thread.Sleep(2000);

                //WinAPI.SendMessage(imperianic.MainWindowHandle, 0x104, )

                // Sender.PressKey(42);

                Sender2.Send(Sender2.ScanCodeShort.F8);

                /*
                Sender.SendKeyboardInput(new Sender.KeyboardInput[]
                {
                    new Sender.KeyboardInput
                    {
                        wVk = 0x70,
                        dwFlags = (uint) (Sender.KeyEventF.KeyDown.GetHashCode())
                    },
                    new Sender.KeyboardInput
                    {
                        wVk = 0x70,
                        dwFlags = (uint) (Sender.KeyEventF.KeyUp).GetHashCode()
                    }
                });
                */

                Console.WriteLine("Sent");
            }
        }
    }
}
