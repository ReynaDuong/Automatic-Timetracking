using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WindowsFormsApp2
{
    class ProcessInfo
    {
        //foreground window requirements
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        //requirement for retreiving PID from handle
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        //requirement for retreiving the last input tick
        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        //get last tick count
        public static uint getLastTick()
        {
            LASTINPUTINFO lastInput = new LASTINPUTINFO();

            lastInput.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(lastInput);
            lastInput.dwTime = 0;

            if (GetLastInputInfo(ref lastInput))      //if succeed, return last input tick count
                return lastInput.dwTime;

            return 0;
        }

        //get current active window title
        public static string getForegroundWinTitle()
        {
            string title = string.Empty;
            IntPtr handle = GetForegroundWindow();
            int titleLength = GetWindowTextLength(handle) + 1;
            StringBuilder sb = new StringBuilder(titleLength);
            if (GetWindowText(handle, sb, titleLength) > 0)
            {
                title = sb.ToString();
            }
            return title;
        }
        
        //get process name
        public static  string getForegroundProcName()
        {

            uint pid = 0;
            IntPtr handle = GetForegroundWindow();
            GetWindowThreadProcessId(handle, out pid);
            Process p = Process.GetProcessById( (int)pid );

            return p.ProcessName;
        }

        //get process ID
        public static  int getPid()
        {
            uint pid = 0;
            IntPtr handle = GetForegroundWindow();
            GetWindowThreadProcessId(handle, out pid);

            return (int) pid;
        }
    }
}
