using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        //get current active window title
        static public string getForegroundWinTitle()
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

        //
        static public string getForegroundProcName()
        {
            uint pid = 0;
            IntPtr handle = GetForegroundWindow();
            GetWindowThreadProcessId(handle, out pid);
            Process p = Process.GetProcessById( (int)pid );

            return p.ProcessName;
        }

        static public int getPid()
        {
            uint pid = 0;
            IntPtr handle = GetForegroundWindow();
            GetWindowThreadProcessId(handle, out pid);

            return (int) pid;
        }
    }
}
