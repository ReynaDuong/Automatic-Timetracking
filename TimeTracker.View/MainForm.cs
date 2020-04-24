using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TimeTracker.View.EventReport.Consumer;

namespace TimeTracker.View
{
	public partial class MainForm : Form
	{
		int _idleDebug = 0;

		private const uint MinIdleSeconds = 3; //minimum seconds that trips the idle counter

		bool _idling = false;
		uint _seconds = 0;
		double _idleSeconds = 0;
		double _idleFreeze = 0;
		double _idledAt = 0;
		double _idleContinued = 0;

		string _winTitle = string.Empty; //current winTitle
		string _psName = string.Empty; //current psName
		string _url = string.Empty; //current URL

		string _prevTitle = string.Empty; //previous winTitle
		string _prevPs = string.Empty; //previous psName
		string _prevUrl = string.Empty; //previous URL

		Stopwatch _stopwatch = new Stopwatch();
		TimeSpan _ts = new TimeSpan();
		private string screenshot;
		

		int _i, _j, _k = 0;

		public MainForm()
		{
			try
			{
				InitializeComponent();
				userName.Text = Global.name;
				projectName.Text = "Choose a project to begin session...";

				//format
				FormBorderStyle = FormBorderStyle.FixedSingle;
				MaximizeBox = false;
				MinimizeBox = true;
				CenterToScreen();
				HideLabels();


				//polling thread
				Thread pollingThread;
				pollingThread = new Thread(StartPolling);
				pollingThread.IsBackground = true;
				pollingThread.Start();

				//idle monitor
				Thread idleMonitor;
				idleMonitor = new Thread(StartIdleMonitoring);
				idleMonitor.IsBackground = true;
				idleMonitor.Start();
			}
			catch (Exception e)
			{
				MessageBox.Show(e.StackTrace);
			}
		}

		//thread to poll
		private void StartPolling()
		{
			Console.WriteLine("Polling...");

			//wait for project selection before starting
			try
			{
				while (true)
				{
					Thread.Sleep(50);

					ProcessInfo.GetAll(out _winTitle, out _psName, out _url); //get foreground window info

					if (!_prevTitle.Equals(_winTitle)) //title changed
					{
						_stopwatch.Stop();
						_ts = _stopwatch.Elapsed;

						var e = DictionaryInsert();

						TimeEntriesPost(e); //post or update time entries
						
						_stopwatch.Restart();

						_prevTitle = _winTitle;
						_prevPs = _psName;
						_prevUrl = _url;

						label1.Text = _prevTitle;
						label2.Text = _prevPs;
						label4.Text = _prevUrl;
					}
				} //end while
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}


		private string CaptureCurrentWindow(string applicationName, string windowName)
		{
			var userpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var path = userpath + "/Captures/";
			var today = DateTime.Now;
			var fileName = $"{_psName}_{today:yyyyMMddhhmmss}";

			Directory.CreateDirectory(path);

			return ProcessInfo.CaptureActiveWindowScreenShot(path, fileName, applicationName, windowName);
		}

		public void TimeEntriesPost(Event e)
		{
			if (e == null)
			{
				return;
			}

			var idt = Global.dictionaryEvents[e];

			if (idt.taskId.Equals("")) //undefined events (events with empty task ID) will not be uploaded
			{
				return;
			}

			_i++;
			label7.Text = _i.ToString();

			Global.dictionaryEvents[e].entryId = Guid.NewGuid().ToString();

			Global.dictionaryEvents[e].lastPostedTs = DateTimeOffset.Now;
		}


		//associate event to task ID and names for 'dictionaryEvent
		public List<dynamic> AssociateForDictionaryEvents()
		{
			var e = new Event();
			var idt = new EventValues();
			var associatedSet = new List<dynamic>();

			e.winTitle = _prevTitle;
			e.process = _prevPs;
			e.url = _prevUrl;

			if (!Global.Filter(e))
			{
				return null;
			}

			idt.ts = _ts;
			idt.entryId = "";

			label28.Text = _idleSeconds.ToString(CultureInfo.InvariantCulture);

			_idleFreeze = Math.Floor(_idleSeconds);
			
			if (_idleFreeze > 0)
			{
				label19.Text = _prevPs;
				label20.Text = idt.ts.TotalSeconds.ToString(CultureInfo.InvariantCulture);
				label21.Text = _idleFreeze.ToString(CultureInfo.InvariantCulture);

				if ((idt.ts.TotalSeconds - _idleFreeze) < 0) //error, when idle time is more than duration
				{
					ProcessInfo.GetAll(out _, out var ps, out _);
					label24.Text = ps;

					_k++;
					label29.Text = "Idle error occured# " + _k;

					idt.idle = TimeSpan.FromSeconds(0.0);
				}

				else
				{
					idt.idle = TimeSpan.FromSeconds(_idleFreeze);
				}
			}

			idt.active = idt.ts - idt.idle;
			idt.activeDelta = idt.active; //activeDelta is same as active from the very beginning, since it starts from zero

			//associate task by URL or process name based on if URL is empty
			try
			{
				if (e.url.Equals("")) //empty, it's a non-chrome event
				{
					idt.taskId = Global.associations[_prevPs].id;
					idt.taskName = Global.associations[_prevPs].name;
				}

				else //not empty, it's a chrome event
				{
					idt.taskId = Global.associations[_prevUrl].id;
					idt.taskName = Global.associations[_prevUrl].name;
				}
			}
			catch //non associated events will be marked as undefined
			{
				idt.taskId = "";
				idt.taskName = "*No association*";
			}

			associatedSet.Add(e);
			associatedSet.Add(idt);

			return associatedSet;
		}


		public Event DictionaryInsert()
		{
			//perform association
			var associatedSet = AssociateForDictionaryEvents();

			if (associatedSet == null)
			{
				ResetIdle();
				return null;
			}

			Event e = associatedSet[0]; //key
			EventValues idt = associatedSet[1]; //value

			try
			{
				// if an event is already in the table, update timespan and idle time
				if (Global.dictionaryEvents.ContainsKey(e)) 
				{
					Global.dictionaryEvents[e].ts = Global.dictionaryEvents[e].ts + _ts;

					if (_idleFreeze > 0)
					{
						Global.dictionaryEvents[e].idle =
							Global.dictionaryEvents[e].idle + TimeSpan.FromSeconds(_idleFreeze);
					}

					var oldActive = Global.dictionaryEvents[e].active; //old active time
					Global.dictionaryEvents[e].active =
						Global.dictionaryEvents[e].ts - Global.dictionaryEvents[e].idle; //new active time
					Global.dictionaryEvents[e].activeDelta =
						Global.dictionaryEvents[e].active - oldActive; //get differences of active times

					StoreGlobal(0, e);
					TaskTimeLogUpdate(e);
				}
				else
				{
					Global.dictionaryEvents.Add(e, idt);

					StoreGlobal(1, e);
					TaskTimeLogUpdate(e);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}

			ResetIdle();
			return e;
		}

		
		private void StoreGlobal(int newItem, Event e)
		{
			var report = new Report(e, Global.dictionaryEvents[e], _winTitle, screenshot);
			var today = DateTime.Now.Date.ToString("yyyy_MM_dd");
			// add environment variable to get user path
			var userpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var path = userpath + "/Logs/";
			Directory.CreateDirectory(path);
			string reportName;

			var tasks = new[]
			{
				new Task(() => WriteGlobalEventToScreen(newItem, e)),

				new Task(() =>
				{
					reportName = Path.Combine(path, $"Output{today}.csv");
					WriteGlobalEventToFlatFile(reportName, report);
				}),

				new Task(() =>
				{
					reportName = Path.Combine(path, $"Output{today}.json");
					WriteGlobalEventsJson(reportName, report);
				})
			};

			foreach (var task in tasks)
			{
				task.Start();
			}

			Task.WaitAll(tasks);
		}

		private void WriteGlobalEventToScreen(int newItem, Event e)
		{
			var idt = Global.dictionaryEvents[e];
			var elapsedTime = $"{idt.ts.Hours:00}:{idt.ts.Minutes:00}:{idt.ts.Seconds:00}";
			var idledTime = $"{idt.idle.Hours:00}:{idt.idle.Minutes:00}:{idt.idle.Seconds:00}";
			var activeTime = $"{idt.active.Hours:00}:{idt.active.Minutes:00}:{idt.active.Seconds:00}";

			var lv = new ListViewItem(e.process);
			lv.SubItems.Add(e.url);
			lv.SubItems.Add(elapsedTime);
			lv.SubItems.Add(idledTime);
			lv.SubItems.Add(idt.taskName);
			lv.SubItems.Add(activeTime);

			if (newItem == 1) //add list item
			{
				listView1.Items.Add(lv);
				Global.dictionaryEvents[e].listId = listView1.Items.IndexOf(lv);
			}
			else //update list item
			{
				listView1.Items[Global.dictionaryEvents[e].listId].SubItems[2].Text = elapsedTime;
				listView1.Items[Global.dictionaryEvents[e].listId].SubItems[3].Text = idledTime;
				listView1.Items[Global.dictionaryEvents[e].listId].SubItems[5].Text = activeTime;
			}
		}

		private void WriteGlobalEventToFlatFile(string reportName, Report report)
		{
			var consumer = ReportConsumerFactory.GetFlatFileReportConsumer(reportName);
			consumer.WriteToFile(report);
		}

		private void WriteGlobalEventsJson(string reportName, Report report)
		{
			var consumer = ReportConsumerFactory.GetJsonReportConsumer(reportName);
			consumer.WriteToFile(report);
		}

		private void TaskTimeLogUpdate(Event e)
		{
			var idt = Global.dictionaryEvents[e];
			var taskId = idt.taskId;

			if (taskId.Equals("")) //no taskId means such event is not associated to any tasks
			{
				return;
			}

			var listId = Global.definedTaskIdTimeLogInfo[taskId].listId;

			var newActive = Global.definedTaskIdTimeLogInfo[taskId].active + idt.activeDelta;
			Global.definedTaskIdTimeLogInfo[taskId].active = newActive;

			Global.activeTotal += idt.activeDelta;

			var newActiveFormatted = $"{newActive.Hours:00}:{newActive.Minutes:00}:{newActive.Seconds:00}";
			var activeTotal = $"{Global.activeTotal.Hours:00}:{Global.activeTotal.Minutes:00}:{Global.activeTotal.Seconds:00}";

			listView2.Items[listId].SubItems[1].Text = newActiveFormatted;
			label17.Text = activeTotal;
		}

		private void StartIdleMonitoring()
		{
			Console.WriteLine("Idle monitoring...");

			// TODO: will blow up if running as Debug mode within VS and try to record itself. Find out why
			var captured = false;
			const int idleSecondElapsedToCapture = 3;

			while (true)
			{
				Thread.Sleep(50);

				if (_idleSeconds < 1)
				{
					captured = false;
				}

				var currentTick = (uint)Environment.TickCount;
				var lastTick = ProcessInfo.GetLastTick();

				if (lastTick == 0) //fails to get tick
				{
					continue;
				}

				_seconds = (currentTick - lastTick) / 1000;         //convert to seconds
				if (_seconds >= MinIdleSeconds)
				{
					if (_idling == false)
					{
						_idling = true; //tripped
						_idledAt = _stopwatch.Elapsed.TotalSeconds;
					}
				}
				else if (_idling)
				{
					_idling = false;
					_idleContinued += _stopwatch.Elapsed.TotalSeconds - _idledAt;
				}


				if (_seconds >= MinIdleSeconds && _idling == true)
				{
					_idleSeconds = _idleContinued + (_stopwatch.Elapsed.TotalSeconds - _idledAt);

					if (_idleSeconds > idleSecondElapsedToCapture && !captured)
					{
						screenshot = CaptureCurrentWindow(_psName, _winTitle);

						captured = true;
					}
				}

				if (_idleDebug == 1)
				{
					label14.Text = _idleSeconds.ToString(CultureInfo.InvariantCulture);
					label8.Text = _stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture);
					label22.Text = "idled at    " + _idledAt.ToString(CultureInfo.InvariantCulture);
					label23.Text = "cont. from    " + _idleContinued.ToString(CultureInfo.InvariantCulture);
					label25.Text = _seconds.ToString();
				}
				else
				{
					label14.Text = ((int)_idleSeconds).ToString();
				}

			}

		}

		private void ResetIdle()
		{
			_j++;
			label26.Text = "RESET# " + _j.ToString();
			label27.Text = _prevPs;
			_idling = false;
			_idleSeconds = 0;
			_idleContinued = 0;
			_idledAt = 0;
		}

		private void HideLabels()
		{
			if (_idleDebug == 0)
			{
				label7.Visible = false;
				label8.Visible = false;
				label19.Visible = false;
				label20.Visible = false;
				label21.Visible = false;
				label22.Visible = false;
				label23.Visible = false;
				label24.Visible = false;
				label25.Visible = false;
				label26.Visible = false;
				label27.Visible = false;
				label28.Visible = false;
				label29.Visible = false;
			}
			else
			{
				label7.Visible = true;
				label8.Visible = true;
				label19.Visible = true;
				label20.Visible = true;
				label21.Visible = true;
				label22.Visible = true;
				label23.Visible = true;
				label24.Visible = true;
				label25.Visible = true;
				label26.Visible = true;
				label27.Visible = true;
				label28.Visible = true;
				label29.Visible = true;
			}
		}

		private void button4_Click(object sender, EventArgs e)
		{
			if (_idleDebug == 0)
			{
				_idleDebug = 1;
				HideLabels();
			}
			else
			{
				_idleDebug = 0;
				HideLabels();
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			// load the form
		}
	}
}