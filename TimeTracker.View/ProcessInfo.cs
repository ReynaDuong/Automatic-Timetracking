using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace TimeTracker.View
{
	class ProcessInfo
	{
		#region Dll
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

		[DllImport(Dllname)]
		private static extern bool GetClientRect(IntPtr hWnd, out Rect lpRect);

		public static Rectangle GetClientRect(IntPtr hWnd)
		{
			var nativeRect = new Rect();
			GetClientRect(hWnd, out nativeRect);
			return nativeRect.ToRectangle();
		}

		[DllImport(Dllname)]
		private static extern bool ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);
		#endregion


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
			return p.ProcessName;
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

		public static string CaptureActiveWindowScreenShot(string filePath, string fileName, string applicationName, string windowsTitle)
		{
			Console.WriteLine($"Application name = {applicationName}");
			Console.WriteLine($"Windows title = {windowsTitle}");

			// get target process
			var process = Process.GetProcesses()
				.FirstOrDefault(p => p.ProcessName.Equals(applicationName) && p.MainWindowTitle.Equals(windowsTitle));

			// build window from process
			var handle = process.MainWindowHandle;
			var window = GetClientRect(handle);

			// calculate offset
			var leftPoint = new Point(0, 0);
			ClientToScreen(handle, ref leftPoint);
			window.X = leftPoint.X;
			window.Y = leftPoint.Y;

			// print screen
			using (var bitmap = new Bitmap(window.Width, window.Height))
			{
				using (var g = Graphics.FromImage(bitmap))
				{
					g.CopyFromScreen(window.X, window.Y, 0, 0, new Size(window.Width, window.Height));
				}

				fileName = Path.Combine(filePath, $"{fileName}.jpeg");

				Console.WriteLine($"Captured to {fileName}");

				bitmap.Save(fileName, ImageFormat.Jpeg);
			}

			return Path.GetFullPath(fileName);
		}

		public static void CaptureEntireWindowScreenShot(string filePath, string fileName, ImageFormat format)
		{
			using (var bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
			{
				using (var g = Graphics.FromImage(bitmap))
				{
					g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
				}

				fileName = Path.Combine(filePath, $"{fileName}.jpeg");
				
				Console.WriteLine($"Captured to {fileName}");

				bitmap.Save(fileName, ImageFormat.Jpeg);
			}
		}
	}
}