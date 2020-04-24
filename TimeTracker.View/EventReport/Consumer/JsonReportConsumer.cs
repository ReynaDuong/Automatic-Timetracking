using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TimeTracker.View.EventReport.Consumer
{
	/*
	 * If has to serialize everything to comply with JSON array syntax, thus has to store every report in memory
	 * So if let running long time probably will run out of memory
	 */ 
	class JsonReportConsumer : AbstractReportConsumer
	{
		public JsonReportConsumer(string reportPath) : base(reportPath)
		{
			if (File.Exists(reportPath))
			{
				var jsonString = File.ReadAllText(reportPath);
				reports = JsonConvert.DeserializeObject<List<Report>>(jsonString);
			}
		}

		public override void WriteToFile(Report report)
		{
			reports.Add(report);
			
			var jsonData = JsonConvert.SerializeObject(reports, Formatting.Indented);
			File.WriteAllText(reportPath, jsonData);
		}
	}
}
