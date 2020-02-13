using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

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

		[DllImport("User32.Dll")]
		public static extern int FindWindow(string lpClassName, string lpWindowName);


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

		public static void CaptureActiveWindowScreenShot(string filePath, string fileName, string windowsTitle)
		{
			IntPtr hwnd = IntPtr.Zero;
			IntPtr hwndChild = IntPtr.Zero;
			
			Console.WriteLine($"windowsTitle = {windowsTitle}");
			
			//Get a handle for the Calculator Application main window
			hwnd = (IntPtr) FindWindow(null, windowsTitle);

			var appProcess = FindWindow(windowsTitle, windowsTitle);

			Console.WriteLine($"appProcess = {appProcess}");
			
			var handle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, windowsTitle, windowTitle: windowsTitle);

			Console.WriteLine($"Win title of handle = {GetWinTitle(handle)}");
			//			var handle = GetForegroundWindow();


			Console.WriteLine($"Handle = {handle}");

			Rectangle bounds = GetClientRect(handle);

			Console.WriteLine($"bounds = {bounds}");

			using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
			{
				using (Graphics g = Graphics.FromImage(bitmap))
				{
					g.CopyFromScreen(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom, bitmap.Size);

					//					g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
				}

				Directory.CreateDirectory(filePath);

				fileName = Path.Combine(filePath, $"{fileName}.jpeg");

				Console.WriteLine($"Save the image to '{fileName}'");

				bitmap.Save(fileName, ImageFormat.Jpeg);
			}
		}

		public static void CaptureEntireWindowScreenShot(string filePath, string fileName, ImageFormat format)
		{
			using (var bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
			{
				using (var g = Graphics.FromImage(bitmap))
				{
					g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
				}

				Directory.CreateDirectory(filePath);

				fileName = Path.Combine(filePath, $"{fileName}.jpeg");

				Console.WriteLine($"Save the image to '{fileName}'");

				bitmap.Save(fileName, ImageFormat.Jpeg);
			}
		}
	}
}