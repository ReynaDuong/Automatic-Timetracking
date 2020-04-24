using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTracker.View.EventReport.Consumer
{
	abstract class AbstractReportConsumer
	{
		protected List<Report> reports;
		protected string reportPath;

		protected AbstractReportConsumer(string reportPath)
		{
			reports = new List<Report>();
			this.reportPath = reportPath;
		}

		public abstract void WriteToFile(Report report);
	}
}
