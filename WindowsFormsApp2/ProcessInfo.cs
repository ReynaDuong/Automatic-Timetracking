using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;

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
        public static string getWintitle(IntPtr handle)
        {
            int titleLength = GetWindowTextLength(handle) + 1;
            StringBuilder sb = new StringBuilder(titleLength);
            GetWindowText(handle, sb, titleLength);
            
            return sb.ToString();
        }
        
        //get process name
        public static string getPsName(IntPtr handle)
        {
            uint pid = 0;
            GetWindowThreadProcessId(handle, out pid);
            Process p = Process.GetProcessById( (int)pid );
            return p.ProcessName.ToLower();
        }

        public static void getAll(out string winTitle, out string psName, out string URL)
        {
            try
            {
                //foreground window
                IntPtr handle = GetForegroundWindow();

                //foreground window title
                winTitle = getWintitle(handle);

                //process name
                psName = getPsName(handle);

                //URL of foreground window
                if (psName.Equals("chrome"))
                    URL = GetUrl.fromChromeTitle(winTitle, handle);
                else
                    URL = "";

                return;
            }
            catch                   //window closes before PID is able to be obtained, throws exception. Usually happens when the focus is on window A and user clicked close on window B
            {
                winTitle = "ignore";
                psName = "ignore";
                URL = "ignore";
                return;
            }
        }

    }
}
