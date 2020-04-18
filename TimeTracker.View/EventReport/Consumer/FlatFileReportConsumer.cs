using System.IO;

namespace TimeTracker.View.EventReport.Consumer
{
	class FlatFileReportConsumer : AbstractReportConsumer
	{
		public FlatFileReportConsumer(string reportPath) : base(reportPath)
		{
		}

		public override void WriteToFile(Report report)
		{
			reports.Add(report);

			if (!File.Exists(reportPath))
			{
				using (var sw = new StreamWriter(reportPath))
				{
					sw.WriteLine("TimeStamp|Id|OS|Process|Duration|Idle|Active|URL|Title|ScreenShot");
				}
			}

			using (var sw = new StreamWriter(reportPath, true))
			{
				sw.Write($"{report.TimeStamp}|");
				sw.Write($"{report.Id}|");
				sw.Write($"{report.Process}|");
				sw.Write($"{report.OS}|");
				sw.Write($"{report.Idle}|");
				sw.Write($"{report.Active}|");
				sw.Write($"{report.Url}|");
				sw.Write($"{report.Title}|");
				sw.WriteLine($"{report.ScreenShot}");
			}
		}
	}
}