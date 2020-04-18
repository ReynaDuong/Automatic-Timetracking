using System;
using System.Globalization;

namespace TimeTracker.View
{
	public class Report
	{
		public string TimeStamp { get; }
		public string Id { get; }
		public string OS { get; }
		public string Process { get; }
		public string Url { get; }
		public string Title { get; }
		public string Duration { get; }
		public string Idle { get; }
		public string Active { get; }
		public string ScreenShot { get; }


		public Report(Event e, EventValues idt, string title, string screenshot)
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
			ScreenShot = screenshot ?? "";
		}
	}
}
