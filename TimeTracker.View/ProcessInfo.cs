using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TimeTracker.View
{
	class ProcessInfo
	{
		private const string Dllname = "user32.dll";

		//foreground window requirements
		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern int GetWindowTextLength(IntPtr hWnd);

		//requirement for retrieving PID from handle
		[DllImport("user32.dll", SetLastError = true)]
		static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

		//requirement for retrieving the last input tick
		[DllImport(Dllname)]
		private static extern bool GetLastInputInfo(ref Lastinputinfo plii);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetWindowRect(out Rect lpRect);

		internal struct Lastinputinfo
		{
			public uint cbSize;
			public uint dwTime;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Rect
		{
			int left, top, right, bottom;

			public Rectangle ToRectangle()
			{
				return new Rectangle(left, top, right - left, bottom - top);
			}
		}

		[DllImport(Dllname, SetLastError = true)]
		public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, String className, String windowTitle);

		[DllImport(Dllname)]
		private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

		public static Rectangle GetClientRect(IntPtr hWnd)
		{
			var nativeRect = new Rect();
			GetClientRect(hWnd, out nativeRect);
			return nativeRect.ToRectangle();
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

		public static void CaptureActiveWindowScreenShot(string filePath, string fileName, ImageFormat format)
		{
			// todo: implement to capture the current active screen
			var groundHandle = GetForegroundWindow();
			var winTitle = GetWinTitle(groundHandle);

			var handle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, String.Empty, winTitle);
			Rectangle bounds = GetClientRect(handle);

			using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
			{
				using (Graphics g = Graphics.FromImage(bitmap))
				{
					g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
				}

				fileName = Path.Combine(filePath, $"{fileName}.jpeg");

				Console.WriteLine($"Save the image to '{fileName}'");

				bitmap.Save(fileName, ImageFormat.Jpeg);
			}
		}
	}
}