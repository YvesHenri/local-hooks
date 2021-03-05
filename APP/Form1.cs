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

        public enum SystemMetric
        {
            SM_CXSCREEN = 0,  // 0x00
            SM_CYSCREEN = 1,  // 0x01
            SM_CXVSCROLL = 2,  // 0x02
            SM_CYHSCROLL = 3,  // 0x03
            SM_CYCAPTION = 4,  // 0x04
            SM_CXBORDER = 5,  // 0x05
            SM_CYBORDER = 6,  // 0x06
            SM_CXDLGFRAME = 7,  // 0x07
            SM_CXFIXEDFRAME = 7,  // 0x07
            SM_CYDLGFRAME = 8,  // 0x08
            SM_CYFIXEDFRAME = 8,  // 0x08
            SM_CYVTHUMB = 9,  // 0x09
            SM_CXHTHUMB = 10, // 0x0A
            SM_CXICON = 11, // 0x0B
            SM_CYICON = 12, // 0x0C
            SM_CXCURSOR = 13, // 0x0D
            SM_CYCURSOR = 14, // 0x0E
            SM_CYMENU = 15, // 0x0F
            SM_CXFULLSCREEN = 16, // 0x10
            SM_CYFULLSCREEN = 17, // 0x11
            SM_CYKANJIWINDOW = 18, // 0x12
            SM_MOUSEPRESENT = 19, // 0x13
            SM_CYVSCROLL = 20, // 0x14
            SM_CXHSCROLL = 21, // 0x15
            SM_DEBUG = 22, // 0x16
            SM_SWAPBUTTON = 23, // 0x17
            SM_CXMIN = 28, // 0x1C
            SM_CYMIN = 29, // 0x1D
            SM_CXSIZE = 30, // 0x1E
            SM_CYSIZE = 31, // 0x1F
            SM_CXSIZEFRAME = 32, // 0x20
            SM_CXFRAME = 32, // 0x20
            SM_CYSIZEFRAME = 33, // 0x21
            SM_CYFRAME = 33, // 0x21
            SM_CXMINTRACK = 34, // 0x22
            SM_CYMINTRACK = 35, // 0x23
            SM_CXDOUBLECLK = 36, // 0x24
            SM_CYDOUBLECLK = 37, // 0x25
            SM_CXICONSPACING = 38, // 0x26
            SM_CYICONSPACING = 39, // 0x27
            SM_MENUDROPALIGNMENT = 40, // 0x28
            SM_PENWINDOWS = 41, // 0x29
            SM_DBCSENABLED = 42, // 0x2A
            SM_CMOUSEBUTTONS = 43, // 0x2B
            SM_SECURE = 44, // 0x2C
            SM_CXEDGE = 45, // 0x2D
            SM_CYEDGE = 46, // 0x2E
            SM_CXMINSPACING = 47, // 0x2F
            SM_CYMINSPACING = 48, // 0x30
            SM_CXSMICON = 49, // 0x31
            SM_CYSMICON = 50, // 0x32
            SM_CYSMCAPTION = 51, // 0x33
            SM_CXSMSIZE = 52, // 0x34
            SM_CYSMSIZE = 53, // 0x35
            SM_CXMENUSIZE = 54, // 0x36
            SM_CYMENUSIZE = 55, // 0x37
            SM_ARRANGE = 56, // 0x38
            SM_CXMINIMIZED = 57, // 0x39
            SM_CYMINIMIZED = 58, // 0x3A
            SM_CXMAXTRACK = 59, // 0x3B
            SM_CYMAXTRACK = 60, // 0x3C
            SM_CXMAXIMIZED = 61, // 0x3D
            SM_CYMAXIMIZED = 62, // 0x3E
            SM_NETWORK = 63, // 0x3F
            SM_CLEANBOOT = 67, // 0x43
            SM_CXDRAG = 68, // 0x44
            SM_CYDRAG = 69, // 0x45
            SM_SHOWSOUNDS = 70, // 0x46
            SM_CXMENUCHECK = 71, // 0x47
            SM_CYMENUCHECK = 72, // 0x48
            SM_SLOWMACHINE = 73, // 0x49
            SM_MIDEASTENABLED = 74, // 0x4A
            SM_MOUSEWHEELPRESENT = 75, // 0x4B
            SM_XVIRTUALSCREEN = 76, // 0x4C
            SM_YVIRTUALSCREEN = 77, // 0x4D
            SM_CXVIRTUALSCREEN = 78, // 0x4E
            SM_CYVIRTUALSCREEN = 79, // 0x4F
            SM_CMONITORS = 80, // 0x50
            SM_SAMEDISPLAYFORMAT = 81, // 0x51
            SM_IMMENABLED = 82, // 0x52
            SM_CXFOCUSBORDER = 83, // 0x53
            SM_CYFOCUSBORDER = 84, // 0x54
            SM_TABLETPC = 86, // 0x56
            SM_MEDIACENTER = 87, // 0x57
            SM_STARTER = 88, // 0x58
            SM_SERVERR2 = 89, // 0x59
            SM_MOUSEHORIZONTALWHEELPRESENT = 91, // 0x5B
            SM_CXPADDEDBORDER = 92, // 0x5C
            SM_DIGITIZER = 94, // 0x5E
            SM_MAXIMUMTOUCHES = 95, // 0x5F

            SM_REMOTESESSION = 0x1000, // 0x1000
            SM_SHUTTINGDOWN = 0x2000, // 0x2000
            SM_REMOTECONTROL = 0x2001, // 0x2001


            SM_CONVERTIBLESLATEMODE = 0x2003,
            SM_SYSTEMDOCKED = 0x2004,
        }

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
            /*
            var mouseHook = HookService.Mouse.Install();
            var keyboardHook = HookService.Keyboard.Install();

            mouseHook.OnMove += OnGlobalMouseMove;
            keyboardHook.OnPress += OnKeyPressed;
            */

            label1.Text = string.Format("Virtual screen size: {0} x {1}", SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
        }

        private void OnKeyPressed(KeyboardHookEventArgs args)
        {
            /*
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
            */
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
    }
}
