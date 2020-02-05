using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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
		private static extern bool GetLastInputInfo(ref Lastinputinfo plii);

		internal struct Lastinputinfo
		{
			public uint cbSize;
			public uint dwTime;
		}

		//get last tick count
		public static uint GetLastTick()
		{
			var lastInput = new Lastinputinfo();

			lastInput.cbSize = (uint) Marshal.SizeOf(lastInput);
			lastInput.dwTime = 0;

			if (GetLastInputInfo(ref lastInput)) //if succeed, return last input tick count
			{
				return lastInput.dwTime;
			}

			return 0;
		}

		//get current active window title
		public static string GetWinTitle(IntPtr handle)
		{
			var titleLength = GetWindowTextLength(handle) + 1;
			var sb = new StringBuilder(titleLength);
			GetWindowText(handle, sb, titleLength);

			return sb.ToString();
		}

		//get process name
		public static string GetPsName(IntPtr handle)
		{
			uint pid = 0;
			GetWindowThreadProcessId(handle, out pid);
			var p = Process.GetProcessById((int) pid);
			return p.ProcessName.ToLower();
		}

		public static void GetAll(out string winTitle, out string psName, out string url)
		{
			try
			{
				//foreground window
				var handle = GetForegroundWindow();

				//foreground window title
				winTitle = GetWinTitle(handle);

				//process name
				psName = GetPsName(handle);

				//URL of foreground window
				url = psName.Equals("chrome") ? GetUrl.FromChromeTitle(winTitle, handle) : "";

				return;
			}
			catch //window closes before PID is able to be obtained, throws exception. Usually happens when the focus is on window A and user clicked close on window B
			{
				winTitle = "ignore";
				psName = "ignore";
				url = "ignore";
				return;
			}
		}
	}
}