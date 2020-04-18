using System.Collections.Generic;

namespace TimeTracker.View.EventReport.Consumer
{
	class ReportConsumerFactory
	{
		private static Dictionary<string, AbstractReportConsumer> flatFileReportConsumers = new Dictionary<string, AbstractReportConsumer>();
		private static Dictionary<string, AbstractReportConsumer> jsonReportConsumers = new Dictionary<string, AbstractReportConsumer>();
		

		public static AbstractReportConsumer GetFlatFileReportConsumer(string name)
		{
			AbstractReportConsumer consumer = null;

			if (flatFileReportConsumers.ContainsKey(name))
			{
				consumer = flatFileReportConsumers[name];
			}
			else
			{
				consumer = new FlatFileReportConsumer(name);
				flatFileReportConsumers.Add(name, consumer);
			}

			return consumer;
		}


		public static AbstractReportConsumer GetJsonReportConsumer(string name)
		{
			AbstractReportConsumer consumer = null;

			if (flatFileReportConsumers.ContainsKey(name))
			{
				consumer = flatFileReportConsumers[name];
			}
			else
			{
				consumer = new JsonReportConsumer(name);
				flatFileReportConsumers.Add(name, consumer);
			}

			return consumer;
		}

	}
}
