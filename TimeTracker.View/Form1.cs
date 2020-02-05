using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace TimeTracker.View
{
	public partial class Form1 : Form
	{
		int _idleDebug = 0;

		private const uint MinIdleSeconds = 3; //minimum seconds that trips the idle counter
		private const int MinTimeToPost = 0; //minimum second of differences in duration before posting

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

		string _elapsedTime = string.Empty;
		Stopwatch _stopwatch = new Stopwatch();
		TimeSpan _ts = new TimeSpan();

		Mutex
			_pollMutex =
				new Mutex(); //prevent from polling when choosing project/associations/deleting time entries, etc..

		Mutex
			_idleMonitorMutex =
				new Mutex(); //protects 'idleSeconds' being written by posting/monitoring threads at the same time

		Mutex _startPollingMutex = new Mutex(); //same for posting thread
		Mutex _startIdleMonMutex = new Mutex(); //halt idle monitoring thread until project is selected 

		int _i, _j, _k = 0;

		public Form1()
			//public Form1()
		{
			InitializeComponent();
			userName.Text = Global.name;
			projectName.Text = "Choose a project to begin session...";

			//format
			TopMost = true;
			FormBorderStyle = FormBorderStyle.FixedSingle;
			MaximizeBox = false;
			MinimizeBox = true;
			CenterToScreen();
			HideLabels();

			//wait until a project is selected
			_startPollingMutex.WaitOne();
			_startIdleMonMutex.WaitOne();

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

		//thread to poll
		private void StartPolling()
		{
			//wait for project selection before starting
			_startPollingMutex.WaitOne(-1, false);
			try
			{
				while (true)
				{
					_pollMutex.WaitOne();
					Thread.Sleep(50);

					ProcessInfo.GetAll(out _winTitle, out _psName, out _url); //get foreground window info

					if (!_prevTitle.Equals(_winTitle)) //title changed
					{
						_stopwatch.Stop();
						_ts = _stopwatch.Elapsed;

						_idleMonitorMutex.WaitOne();
						var e = DictionaryInsert();
						_idleMonitorMutex.ReleaseMutex();
						TimeEntriesPost(e); //post or update time entries
						_stopwatch.Restart();

						_prevTitle = _winTitle;
						_prevPs = _psName;
						_prevUrl = _url;

						label1.Text = _prevTitle;
						label2.Text = _prevPs;
						label4.Text = _prevUrl;
					}

					_pollMutex.ReleaseMutex();
				} //end while
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		} //end polling thread

		//post or update time entries
		public void TimeEntriesPost(Event e)
		{
			if (e == null)
			{
				return;
			}

			var idt = Global.dictionaryEvents[e];
			var start = DateTime.Today.AddHours(6.0); //adds 6 hours for central time
			DateTime end;

			var description = string.Empty;
			var entryId = string.Empty;
			var value = string.Empty; //either process name or URL
			var taskId = string.Empty;

			if (idt.taskId.Equals("")) //undefined events (events with empty task ID) will not be uploaded
			{
				return;
			}
			else if (!ShouldPost(idt, e)) //post only if more than a certain amount of differences in duration
			{
				return;
			}

			_i++; // 
			label7.Text = _i.ToString();

			if (idt.entryId.Equals("")) //POST, empty ID means this event hasn't been posted
			{
				if (e.process.Equals("chrome"))
				{
					description = e.url;
					value = e.url;
					taskId = idt.taskId;
				}

				else
				{
					description = e.process;
					value = e.process;
					taskId = idt.taskId;
				}

				end = DateTime.Parse(idt.active.ToString()).AddHours(6.0);

				var res = Api.AddTimeEntry(start, end, description, Global.workspaceId, Global.projectId, taskId);
				Global.dictionaryEvents[e].entryId =
					res.id; //update dictionary value to include entry ID returned from clockify
			}
			else //PUT   
			{
				if (e.process.Equals("chrome"))
				{
					description = e.url;
					value = e.url;
					taskId = idt.taskId;
				}
				else
				{
					description = e.process;
					value = e.process;
					taskId = idt.taskId;
				}

				entryId = idt.entryId;
				end = DateTime.Parse(idt.active.ToString()).AddHours(6.0);
				Api.UpdateTimeEntry(start, end, description, entryId, Global.workspaceId, Global.projectId, taskId);
			}

			Global.dictionaryEvents[e].lastPostedTs = idt.ts;
		} //end time entries post/update


		//allow post or update if ts is more than a specified seconds of lastPostedTs
		public bool ShouldPost(EventValues idt, Event e)
		{
			var ts = (uint) idt.ts.TotalSeconds;
			var lastPostedTs = (uint) idt.lastPostedTs.TotalSeconds;

			if ((ts - lastPostedTs) >= MinTimeToPost)
			{
				return true;
			}
			else
			{
				return false;
			}
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

			label28.Text = _idleSeconds.ToString();

			_idleFreeze = Math.Floor(_idleSeconds);
			//if (idleFreeze >= MIN_IDLE_SECONDS)
			if (_idleFreeze > 0)
			{
				label19.Text = _prevPs;
				label20.Text = idt.ts.TotalSeconds.ToString();
				label21.Text = _idleFreeze.ToString();

				if ((idt.ts.TotalSeconds - _idleFreeze) < 0) //error, when idle time is more than duration
				{
					_idleMonitorMutex.WaitOne();

					string title, ps, url = string.Empty;
					ProcessInfo.GetAll(out title, out ps, out url);
					label24.Text = ps;


					//MessageBox.Show("idle problem");
					_k++;
					label29.Text = "Idle error occured# " + _k.ToString();

					_idleMonitorMutex.ReleaseMutex();

					idt.idle = TimeSpan.FromSeconds(0.0);
				}

				else
				{
					idt.idle = TimeSpan.FromSeconds(_idleFreeze);
				}
			}

			idt.active = idt.ts - idt.idle;
			idt.activeDelta =
				idt.active; //activeDelta is same as active from the very beginning, since it starts from zero

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
		} //end associateDictionary

		//insert events into dictionary
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
				if (Global.dictionaryEvents.ContainsKey(e)
				) //if an event is already in the table, update timespan and idle time
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

					HistoryUpdate(0, e);
					TaskTimeLogUpdate(e);
				}
				else
				{
					Global.dictionaryEvents.Add(e, idt);

					HistoryUpdate(1, e);
					TaskTimeLogUpdate(e);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}

			ResetIdle();
			return e;
		} //end dictionaryInsert

		//start association from scratch, clears out all current dictionaries
		private void AssociateRaw()
		{
			_i = _j = _k = 0;

			listView1.Items.Clear();
			listView2.Items.Clear();
			label7.Text = _i.ToString();

			var prevTitle = string.Empty;
			var prevPs = string.Empty;
			var prevUrl = string.Empty;

			Global.ClearGlobals();

			//binds task ID and name together, must be done before calling 'loadAssociation' since Association object needed to lookup task name by task ID
			BindAllTaskIdName();

			//load and associate value->taskId using SQL
			var processes = Sql.LoadAssociations(1, Global.projectId);
			var urLs = Sql.LoadAssociations(2, Global.projectId);

			//adds event->task association
			foreach (var ps in processes)
			{
				var t = new Dto.TaskDto() {id = ps.taskId, name = ps.taskName};

				Global.associations.Add(ps.value, t);
			}

			//adds event->task association
			foreach (var url in urLs)
			{
				var t = new Dto.TaskDto() {id = url.taskId, name = url.taskName};

				Global.associations.Add(url.value, t);
			}

			BindDefinedTaskIdName();
			BindDefinedTaskIdListId();

			_stopwatch.Restart();
			ResetIdle();
		}

		//binds task ID and name together
		private void BindAllTaskIdName()
		{
			List<Dto.TaskDto> tasks = Api.GetTasksByProjectId(Global.workspaceId, Global.projectId);
			foreach (var t in tasks)
			{
				Global.allTaskIdName.Add(t.id, t.name);
			}
		}

		//binds tasksID to taskName that have an association to events
		private void BindDefinedTaskIdName()
		{
			var taskId = string.Empty;
			var taskName = string.Empty;

			foreach (var t in Global.associations)
			{
				taskId = t.Value.id;
				taskName = t.Value.name;

				if (!Global.definedTaskIdName.ContainsKey(taskId))
				{
					Global.definedTaskIdName.Add(taskId, taskName);
				}
			}

			//perform sorting
			var sorted = Global.definedTaskIdName.ToList(); //convert dictionary to a list

			sorted.Sort((pair1, pair2) =>
				pair1.Value.CompareTo(pair2.Value)); //sort the list, by comparing the value of each pair

			Global.definedTaskIdName.Clear(); //clears dictionary

			foreach (var t in sorted) //insert sorted pair values back into dictionary
			{
				taskId = t.Key;
				taskName = t.Value;

				if (!Global.definedTaskIdName.ContainsKey(taskId))
				{
					Global.definedTaskIdName.Add(taskId, taskName);
				}
			}
		}

		//binds taskId with listId, and initializes columns for time log (tasks are listed in names)
		private void BindDefinedTaskIdListId()
		{
			var taskId = string.Empty;
			var taskName = string.Empty;
			var startTime = string.Format("{0:00}:{1:00}:{2:00}", TimeSpan.FromSeconds(0).Hours,
				TimeSpan.FromSeconds(0).Minutes, TimeSpan.FromSeconds(0).Seconds);

			foreach (var t in Global.definedTaskIdName)
			{
				taskId = t.Key;
				taskName = t.Value;

				var lv = new ListViewItem(taskName);
				lv.SubItems.Add(startTime);
				listView2.Items.Add(lv);

				var tl = new TimeLogInfo();
				tl.listId = listView2.Items.IndexOf(lv);

				Global.definedTaskIdTimeLogInfo.Add(taskId, tl);
			}

			label17.Text = startTime;
		}

		//delete time entries of a workspace
		private void button3_Click(object sender, EventArgs e)
		{
			if (Global.workspaceId.Equals(string.Empty))
			{
				MessageBox.Show("Session is not running, choose a workspace/project first.");
				return;
			}

			var delete = new Thread(DeleteEntries);
			delete.Start();
		}

		//thread to delete time entries
		private void DeleteEntries()
		{
			button1.Enabled = false;
			button2.Enabled = false;
			button3.Enabled = false;
			button4.Enabled = false;

			_pollMutex.WaitOne();
			_idleMonitorMutex.WaitOne();

			AssociateRaw();
			label1.Text = "Deleting time entries..";

			var entries = new List<Dto.TimeEntryFullDto>();
			while ((entries = Api.FindTimeEntriesByWorkspace(Global.workspaceId)).Count > 0)
			{
				foreach (var entry in entries)
				{
					label2.Text = entry.description;
					label4.Text = entry.id;
					Api.DeleteTimeEntry(Global.workspaceId, entry.id);
				}
			}

			label1.Text = "";
			label2.Text = "";
			label4.Text = "";

			_idleMonitorMutex.ReleaseMutex();
			_pollMutex.ReleaseMutex();

			button1.Enabled = true;
			button2.Enabled = true;
			button3.Enabled = true;
			button4.Enabled = true;
		}

		//update listView
		private void HistoryUpdate(int newItem, Event e)
		{
			var tasks = new List<Task>();

			var writeScreenTask = new Task(() => WriteGlobalEventToScreen(newItem, e));
			var writeFileTask = new Task(() => WriteGlobalEventToFile(e));
			var writeDbTask = new Task(() => WriteGlobalEventToDatabase(e));

			tasks.AddRange(new[]{writeScreenTask, writeFileTask, writeDbTask});

			writeScreenTask.Start();
			writeFileTask.Start();
			writeDbTask.Start();

			Task.WaitAll(tasks.ToArray());
		}


		private void WriteGlobalEventToScreen(int newItem, Event e)
		{
			var idt = Global.dictionaryEvents[e];
			var elapsedTime = string.Format("{0:00}:{1:00}:{2:00}", idt.ts.Hours, idt.ts.Minutes, idt.ts.Seconds);
			var idledTime = string.Format("{0:00}:{1:00}:{2:00}", idt.idle.Hours, idt.idle.Minutes, idt.idle.Seconds);
			var activeTime = string.Format("{0:00}:{1:00}:{2:00}", idt.active.Hours, idt.active.Minutes,
				idt.active.Seconds);

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

		private void WriteGlobalEventToDatabase(Event e)
		{
			// todo: implement
		}


		private void WriteGlobalEventToFile(Event e)
		{
			var today = DateTime.Now.Date.ToString("yyyyMMdd");
			var fileName = $"Output{today}.csv";
			var fileExist = File.Exists(fileName);
			var idt = Global.dictionaryEvents[e];

			using (var sw = new StreamWriter(fileName, true))
			{
				if (!fileExist)
				{
					sw.WriteLine("TaskId|TaskName|LastPostedTime|Active|TimeStamp");
				}

				sw.WriteLine($"{idt.taskId}|" +
				             $"{idt.taskName}|" +
				             $"{idt.lastPostedTs}|" +
				             $"{idt.active}|" +
				             $"{idt.ts}");
			}
		}

		//update times in time log
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

			var newActiveFormated = string.Format("{0:00}:{1:00}:{2:00}", newActive.Hours, newActive.Minutes,
				newActive.Seconds);
			var activeTotal = string.Format("{0:00}:{1:00}:{2:00}", Global.activeTotal.Hours,
				Global.activeTotal.Minutes, Global.activeTotal.Seconds);

			listView2.Items[listId].SubItems[1].Text = newActiveFormated;
			label17.Text = activeTotal;
		}

		//associations (Form 4)
		private void button1_Click(object sender, EventArgs e)
		{
			_pollMutex.WaitOne(); //prevent inserting into dictionary while making association changes
			_idleMonitorMutex.WaitOne();

			var f = new Form4();
			f.StartPosition = FormStartPosition.CenterParent;
			f.ShowDialog(this);

			if (Global.chosen == 1)
			{
				AssociateRaw();
				Global.chosen = 0;
			}

			_idleMonitorMutex.ReleaseMutex();
			_pollMutex.ReleaseMutex();
		}

		//projects (Form 3)
		private void button2_Click(object sender, EventArgs e)
		{
			_pollMutex.WaitOne(); //prevent inserting into dictionary while making association changes
			_idleMonitorMutex.WaitOne();

			var f = new Form3();
			f.StartPosition = FormStartPosition.CenterParent;
			f.ShowDialog(this);

			if (Global.chosen == 1)
			{
				projectName.Text = Global.projectName;
				label13.Text = Global.workspaceName;

				AssociateRaw();

				try
				{
					_startPollingMutex.ReleaseMutex();
				}
				catch
				{
				}

				try
				{
					_startIdleMonMutex.ReleaseMutex();
				}
				catch
				{
				}

				Global.chosen = 0;
			}

			_idleMonitorMutex.ReleaseMutex();
			_pollMutex.ReleaseMutex();
		}

		//thread to monitor idle
		private void StartIdleMonitoring()
		{
			_startIdleMonMutex.WaitOne(-1, false);
			uint currentTick = 0;
			uint lastTick = 0;

			while (true)
			{
				_idleMonitorMutex.WaitOne();
				Thread.Sleep(50);

				currentTick = (uint) Environment.TickCount; //current tick count
				lastTick = ProcessInfo.GetLastTick(); //last input tick count

				if (lastTick == 0) //fails to get tick
				{
					continue;
				}

				_seconds = (currentTick - lastTick) / 1000; //convert to seconds
				if (_seconds >= MinIdleSeconds)
				{
					if (_idling == false)
					{
						_idling = true; //tripped
						_idledAt = _stopwatch.Elapsed.TotalSeconds;
					}
				}
				else if (_idling == true)
				{
					_idling = false;
					_idleContinued += _stopwatch.Elapsed.TotalSeconds - _idledAt;
				}


				if (_seconds >= MinIdleSeconds && _idling == true)
				{
					_idleSeconds = _idleContinued + (_stopwatch.Elapsed.TotalSeconds - _idledAt);
				}


				if (_idleDebug == 1)
				{
					label14.Text = _idleSeconds.ToString();
					label8.Text = _stopwatch.Elapsed.TotalSeconds.ToString();
					label22.Text = "idled at    " + _idledAt.ToString();
					label23.Text = "cont. from    " + _idleContinued.ToString();
					label25.Text = _seconds.ToString();
				}
				else
				{
					label14.Text = ((int) _idleSeconds).ToString();
				}


				_idleMonitorMutex.ReleaseMutex();
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