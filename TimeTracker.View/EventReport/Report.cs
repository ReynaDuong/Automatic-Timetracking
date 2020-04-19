using System;
using System.Globalization;

namespace TimeTracker.View
{
	public class Report
	{
		public string TimeStamp { get; set; }
		public string Id { get; set; }
		public string OS { get; set; }
		public string Process { get; set; }
		public string Url { get; set; }
		public string Title { get; set; }
		public string Duration { get; set; }
		public string Idle { get; set; }
		public string Active { get; set; }
		public string ScreenShot { get; set; }


		public Report()
		{

		}

		public Report(Event e, EventValues idt, string title, string screenShot)
		{
			// todo: dynamic OS

			TimeStamp = DateTime.Now.ToString(CultureInfo.InvariantCulture);
			Id = idt.entryId ?? "";
			OS = "Windows";
			Process = e.process ?? "";
			Url = e.url ?? "";
			Title = title ?? "";
			Duration = $"{idt.ts.Hours:00}:{idt.ts.Minutes:00}:{idt.ts.Seconds:00}";
			Idle = $"{idt.idle.Hours:00}:{idt.idle.Minutes:00}:{idt.idle.Seconds:00}";
			Active = $"{idt.active.Hours:00}:{idt.active.Minutes:00}:{idt.active.Seconds:00}";
			ScreenShot = screenShot ?? "";
		}
	}
}
