using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace renda
{
    public partial class Form1 : Form
    {
        private IntPtr MouseHookHandle { get; set; }

        private IntPtr KeyboardHookHandle { get; set; }

        private NativeMethods.HookCallbackEvent MouseHookProc { get; set; }

        private NativeMethods.HookCallbackEvent KeyboardHookProc { get; set; }

        private Timer AutoFireTimer { get; set; }

        public Form1()
        {
            InitializeComponent();

            Opacity = 0.6;    // for ExStyle

            var h = Marshal.GetHINSTANCE(typeof(Form1).Assembly.GetModules()[0]);
            MouseHookProc = new NativeMethods.HookCallbackEvent(MouseHookCallback);
            MouseHookHandle = NativeMethods.SetWindowsHookEx(14, MouseHookProc, h, 0);   // WH_MOUSE_LL = 14

            KeyboardHookProc = new NativeMethods.HookCallbackEvent(KeyboardHookCallback);
            KeyboardHookHandle = NativeMethods.SetWindowsHookEx(13, KeyboardHookProc, h, 0);    // WH_KEYBOARD_LL = 13

            AutoFireTimer = new Timer();
            AutoFireTimer.Elapsed += (sender, e) => {
                NativeMethods.mouse_event(0x2, 0, 0, 0, (IntPtr)0); // MOUSEEVENTF_LEFTDOWN = 0x2
                NativeMethods.mouse_event(0x4, 0, 0, 0, (IntPtr)0); // MOUSEEVENTF_LEFTUP = 0x4
            };

            if (MouseHookHandle == IntPtr.Zero || KeyboardHookHandle == IntPtr.Zero)
            {
                throw new Exception("failed to capture mouse");
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80000;  // WS_EX_LAYERED
                cp.ExStyle |= 0x20;     // WS_EX_TRANSPARENT

                return cp;
            }
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            var s = (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));

            if (nCode >= 0 && wParam == (IntPtr)0x0100) // WM_KEYDOWN = 0x0100
            {
                if (0x31 <= s.vkCode && s.vkCode <= 0x35) // 1 - 5key
                {
                    var interval = (int)Math.Pow((s.vkCode - 0x31 + 1), 5);
                    AutoFireTimer.Enabled = (AutoFireTimer.Interval == interval) ? !AutoFireTimer.Enabled : true;
                    AutoFireTimer.Interval = interval;

                    label1.ForeColor = AutoFireTimer.Enabled ? Color.Red : Color.Black;
                }

                if (s.vkCode == 0x51)   // Q key
                {
                    Close();
                }
            }

            return NativeMethods.CallNextHookEx(MouseHookHandle, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            var s = (NativeMethods.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.MSLLHOOKSTRUCT));

            if (nCode >= 0)
            {
                Location = new Point(s.pt.x, s.pt.y);
            }

            return NativeMethods.CallNextHookEx(MouseHookHandle, nCode, wParam, lParam);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (MouseHookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(MouseHookHandle);
            }

            if (KeyboardHookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(KeyboardHookHandle);
            }
        }

        class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int x;
                public int y;
            }
            
            [StructLayout(LayoutKind.Sequential)]
            public struct KBDLLHOOKSTRUCT
            {
                public uint vkCode;
                public uint scanCode;
                public uint flags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct MSLLHOOKSTRUCT
            {
                public POINT pt;
                public uint mouseData;
                public uint flags;
                public uint time;
                public IntPtr dwExtraInfo;
            }

            public delegate IntPtr HookCallbackEvent(int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern IntPtr SetWindowsHookEx(int idHook, HookCallbackEvent lpfn, IntPtr hmod, uint dwThreadId);

            [DllImport("user32.dll")]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll")]
            public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);
        }
    }
}
