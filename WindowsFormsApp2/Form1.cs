using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.Diagnostics;
using System.Windows.Automation;
using Newtonsoft.Json;
using System.Web.Helpers;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        private const uint MIN_IDLE_SECONDS = 3;    //minimum seconds that trips the idle counter
        uint idleSeconds = 0;                       //seconds idled
        uint idleFreeze = 0;                        //freeze frame of idled seconds

        string winTitle = string.Empty;             //current winTitle
        string psName = string.Empty;               //current psName
        string URL = string.Empty;                  //current URL

        string prevTitle = string.Empty;            //previous winTitle
        string prevPs = string.Empty;               //previous psName
        string prevUrl = string.Empty;              //previous URL
        
        string elapsedTime = string.Empty;
        Stopwatch stopwatch = new Stopwatch();
        TimeSpan ts;

        Mutex pollMutex = new Mutex();              //prevent from polling when choosing project/associations/deleting time entries, etc..
        Mutex idleMonitorMutex = new Mutex();       //protects 'idleSeconds' being written by posting/monitoring threads at the same time

        Mutex startPollingMutex = new Mutex();      //same for posting thread
        Mutex startIdleMonMutex = new Mutex();      //halt idle monitoring thread until project is selected 

        int i = 0;

        public Form1()
        //public Form1()
        {
            InitializeComponent();
            label6.Text = Global.name;
            label9.Text = "Choose a project to begin session...";

            //format
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.CenterToScreen();

            //wait until a project is selected
            startPollingMutex.WaitOne();
            startIdleMonMutex.WaitOne();

            //polling thread
            System.Threading.Thread pollingThread;
            pollingThread = new System.Threading.Thread(startPolling);
            pollingThread.IsBackground = true;
            pollingThread.Start();

            //idle monitor
            System.Threading.Thread idleMonitor;
            idleMonitor = new System.Threading.Thread(startIdleMonitoring);
            idleMonitor.IsBackground = true;
            idleMonitor.Start();
        }

        //thread to poll
        private void startPolling()
        {
            //wait for project selection before starting
            startPollingMutex.WaitOne(-1, false);
            try
            {
                while (true)
                {
                    pollMutex.WaitOne();
                    System.Threading.Thread.Sleep(50);

                    ProcessInfo.getAll(out winTitle, out psName, out URL);
                    if (psName.Equals("chrome"))
                    {
                        if (!prevTitle.Equals(winTitle))                                  //tab title changed
                        {
                            ts = stopwatch.Elapsed;
                            Event e = dictionaryInsert();
                            timeEntriesPost(e);                                           //post or update time entries
                            stopwatch.Restart();

                            prevTitle = winTitle;
                            prevPs = psName;
                            prevUrl = URL;

                            label1.Text = prevTitle;
                            label2.Text = prevPs;
                            label4.Text = prevUrl;
                        }
                    }
                    else
                    {
                        if (!prevTitle.Equals(winTitle))
                        {
                            ts = stopwatch.Elapsed;
                            Event e = dictionaryInsert();
                            timeEntriesPost(e);
                            stopwatch.Restart();

                            prevTitle = winTitle;
                            prevPs = psName;
                            prevUrl = URL;

                            label1.Text = prevTitle;
                            label2.Text = prevPs;
                            label4.Text = prevUrl;
                        }
                    }

                    pollMutex.ReleaseMutex();
                }//end while
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }//end polling thread



        //post or update time entries
        public void timeEntriesPost(Event e)
        {
            if (e == null)                                          
                return;

            EventValues idt = Global.dictionaryEvents[e];
            DateTime start = DateTime.Today.AddHours(6.0);           //adds 6 hours for central time
            DateTime end;

            string description = string.Empty;
            string entryId = string.Empty;
            string value = string.Empty;                             //either process name or URL
            string taskId = string.Empty;


            if (idt.taskId.Equals(""))                               //undefined events (events with empty task ID) will not be uploaded
                return;
            else if (!shouldPost(idt, e))                            //if less than 5 seconds difference from last post
                return;

            i++;
            label7.Text = i.ToString();

            if (idt.entryId.Equals(""))                              //POST, empty ID means this event hasn't been posted
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

                dynamic res = API.AddTimeEntry(start, end, description, Global.workspaceId, Global.projectId, taskId);
                Global.dictionaryEvents[e].entryId = res.id;         //update dictionary value to include entry ID returned from clockify
            }
            else                                                     //PUT   
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
                API.UpdateTimeEntry(start, end, description, entryId, Global.workspaceId, Global.projectId, taskId);
            }

            Global.dictionaryEvents[e].lastPostedTs = idt.ts;
        }//end time entries post/update


        //allow post or update if ts is more than 5 seconds of lastPostedTs
        public bool shouldPost(EventValues idt, Event e)
        {
            uint ts = (uint) idt.ts.TotalSeconds;
            uint lastPostedTs = (uint)idt.lastPostedTs.TotalSeconds;

            if ((ts - lastPostedTs) > 5)
                return true;
            else
                return false;
        }

        //associate event to task ID and names for 'dictionaryEvent'
        public List<dynamic> associateForDictionaryEvents()
        {
            Event e = new Event();
            EventValues idt = new EventValues();
            List<dynamic> associatedSet = new List<dynamic>();
            
            e.winTitle = prevTitle;
            e.process = prevPs;
            e.url = prevUrl;
            idt.ts = ts;
            idt.entryId = "";
            
            idleFreeze = idleSeconds;

            if (idleFreeze > MIN_IDLE_SECONDS)
            {
                label25.Text = "IDLED DETECTED";

                if ((idt.ts.TotalSeconds - idleFreeze) < 0)
                {
                    label8.Text = prevPs;
                    label19.Text = idt.ts.ToString();
                    label20.Text = idleFreeze.ToString();
                    
                    MessageBox.Show("idle problem");

                    idt.idle = TimeSpan.FromSeconds(0.0);
                }
                    
                else
                    idt.idle = TimeSpan.FromSeconds(idleFreeze);
            }
                
            idt.active = idt.ts - idt.idle;
            idt.activeDelta = idt.active;                                      //activeDelta is same as active from the very beginning, since it starts from zero
            
            //associate task by URL or process name based on if URL is empty
            try
            {
                if (e.url.Equals(""))                                          //empty, it's a non-chrome event
                {
                    idt.taskId = Global.associations[prevPs].id;
                    idt.taskName = Global.associations[prevPs].name;
                }

                else                                                           //not empty, it's a chrome event
                {
                    idt.taskId = Global.associations[prevUrl].id;
                    idt.taskName = Global.associations[prevUrl].name;
                }
            }
            catch                                                              //non associated events will be marked as undefined
            {
                idt.taskId = "";
                idt.taskName = "No association";
            }

            associatedSet.Add(e);
            associatedSet.Add(idt);

            return associatedSet;
        }//end associateDictionary

        //insert events into dictionary
        public Event dictionaryInsert()
        {
            //perform association
            List<dynamic> associatedSet = associateForDictionaryEvents();

            Event e = associatedSet[0];                         //key
            EventValues idt = associatedSet[1];                 //value

            try
            {
                if (Global.dictionaryEvents.ContainsKey(e))     //if an event is already in the table, update timespan and idle time
                {
                    Global.dictionaryEvents[e].ts = Global.dictionaryEvents[e].ts + ts;

                    if (idleFreeze >= MIN_IDLE_SECONDS)
                        Global.dictionaryEvents[e].idle = Global.dictionaryEvents[e].idle + TimeSpan.FromSeconds(idleFreeze);

                    TimeSpan oldActive = Global.dictionaryEvents[e].active;                                                 //old active time
                    Global.dictionaryEvents[e].active = Global.dictionaryEvents[e].ts - Global.dictionaryEvents[e].idle;    //new active time
                    Global.dictionaryEvents[e].activeDelta = Global.dictionaryEvents[e].active - oldActive;                 //get differences of active times
                    
                    historyUpdate(0, e);
                    taskTimeLogUpdate(e);
                }
                else
                {
                    if (!Global.filter(e))
                        return null;
                    
                    Global.dictionaryEvents.Add(e, idt);

                    historyUpdate(1, e);
                    taskTimeLogUpdate(e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            //idleSeonds has been consumed, restting it to 0
            if (idleSeconds >= MIN_IDLE_SECONDS)
            {
                idleMonitorMutex.WaitOne();
                idleSeconds = 0;
                label14.Text = idleSeconds.ToString();
                idleMonitorMutex.ReleaseMutex();
            }

            return e;

        }//end dictionaryInsert

        //start association from scratch, clears out all current dictionaries
        private void associateRaw()
        {
            listView1.Items.Clear();
            listView2.Items.Clear();

            string prevTitle = string.Empty;
            string prevPs = string.Empty;
            string prevUrl = string.Empty;

            Global.clearGlobals();

            //binds task ID and name together, must be done before calling 'loadAssociation' since Association object needed to lookup task name by task ID
            bindAllTaskIdName();

            //load and associate value->taskId using SQL
            List<Association> processes = SQL.loadAssociations(1, Global.projectId);
            List<Association> URLs = SQL.loadAssociations(2, Global.projectId);

            //adds event->task association
            foreach (Association ps in processes)
            {
                Dto.TaskDto t = new Dto.TaskDto() { id = ps.taskId, name = ps.taskName };

                Global.associations.Add(ps.value, t);       
            }

            //adds event->task association
            foreach (Association url in URLs)
            {
                Dto.TaskDto t = new Dto.TaskDto() { id = url.taskId, name = url.taskName };

                Global.associations.Add(url.value, t);
            }
            
            bindDefinedTaskIdName();
            bindDefinedTaskIdListId();

            stopwatch.Restart();
            i = 0;                          //postingThread counts
        }

        //binds task ID and name together
        private void bindAllTaskIdName()
        {
            List<Dto.TaskDto> tasks = API.getTasksByProjectId(Global.workspaceId, Global.projectId);
            foreach (Dto.TaskDto t in tasks)
            {
                Global.allTaskIdName.Add(t.id, t.name);
            }
        }

        //binds tasksID to taskName that have an association to events
        private void bindDefinedTaskIdName()
        {
            string taskId = string.Empty;
            string taskName = string.Empty;
            
          
            foreach (KeyValuePair<string, Dto.TaskDto> t in Global.associations)
            {
                taskId = t.Value.id;
                taskName = t.Value.name;

                if (!Global.definedTaskIdName.ContainsKey(taskId))
                    Global.definedTaskIdName.Add(taskId, taskName);
            }
        }

        //binds taskId with listId, and initializes columns for time log (tasks are listed in names)
        private void bindDefinedTaskIdListId()
        {
            string taskId = string.Empty;
            string taskName = string.Empty;
            string startTime = string.Format("{0:00}:{1:00}:{2:00}", TimeSpan.FromSeconds(0).Hours, TimeSpan.FromSeconds(0).Minutes, TimeSpan.FromSeconds(0).Seconds);

            foreach (KeyValuePair<string, string> t in Global.definedTaskIdName)
            {
                taskId = t.Key;
                taskName = t.Value;

                ListViewItem lv = new ListViewItem(taskName);
                lv.SubItems.Add(startTime);
                listView2.Items.Add(lv);

                TimeLogInfo tl = new TimeLogInfo();
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
                
            pollMutex.WaitOne();
            associateRaw();
            
            List<Dto.TimeEntryFullDto> entries = new List<Dto.TimeEntryFullDto>();
            while ( (entries = API.FindTimeEntriesByWorkspace(Global.workspaceId)).Count > 0 )
            {
                foreach (Dto.TimeEntryFullDto entry in entries)
                {
                    API.DeleteTimeEntry(Global.workspaceId, entry.id);
                }
            }
            pollMutex.ReleaseMutex();
        }

        //thread to monitor idle
        private void startIdleMonitoring()
        {
            startIdleMonMutex.WaitOne(-1, false);
            uint currentTick = 0;
            uint lastTick = 0;
            uint seconds = 0;
            int idleStatus = 0;

            while (true)
            {
                System.Threading.Thread.Sleep(1000);

                currentTick = (uint)Environment.TickCount;             //current tick count
                lastTick = ProcessInfo.getLastTick();                  //last input tick count

                seconds = (currentTick - lastTick) / 1000;             //convert to seconds

                if (seconds >= MIN_IDLE_SECONDS)
                {
                    idleMonitorMutex.WaitOne();

                    if (idleSeconds == 0 && idleStatus == 0)
                    {
                        idleSeconds = seconds;
                        idleStatus = 1;
                    }
                    else if (idleStatus == 0)
                    {
                        idleSeconds += seconds;
                        idleStatus = 1;
                    }
                    else if (idleStatus == 1)
                        idleSeconds++;

                    label14.Text = idleSeconds.ToString();

                    idleMonitorMutex.ReleaseMutex();
                }
                else
                    idleStatus = 0;
            }
        }

        //update listView
        private void historyUpdate(int newItem, Event e)
        {
            //listView1.BeginUpdate();

            EventValues idt = Global.dictionaryEvents[e];

            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}", idt.ts.Hours, idt.ts.Minutes, idt.ts.Seconds);
            string idledTime = string.Format("{0:00}:{1:00}:{2:00}", idt.idle.Hours, idt.idle.Minutes, idt.idle.Seconds);
            string activeTime = string.Format("{0:00}:{1:00}:{2:00}", idt.active.Hours, idt.active.Minutes, idt.active.Seconds);

            ListViewItem lv = new ListViewItem(e.process);
            lv.SubItems.Add(e.url);
            lv.SubItems.Add(elapsedTime);
            lv.SubItems.Add(idledTime);
            lv.SubItems.Add(idt.taskName);
            lv.SubItems.Add(activeTime);

            if (newItem == 1)                                   //add list item
            {
                listView1.Items.Add(lv);
                Global.dictionaryEvents[e].listId = listView1.Items.IndexOf(lv);
            }
            else                                                //update list item
            {
                listView1.Items[Global.dictionaryEvents[e].listId].SubItems[2].Text = elapsedTime;
                listView1.Items[Global.dictionaryEvents[e].listId].SubItems[3].Text = idledTime;
                listView1.Items[Global.dictionaryEvents[e].listId].SubItems[5].Text = activeTime;
            }

            //listView1.EndUpdate();
        }

        //update times in time log
        private void taskTimeLogUpdate(Event e)
        {
           // listView2.BeginUpdate();

            EventValues idt = Global.dictionaryEvents[e];
            string taskId = idt.taskId;

            if (taskId.Equals(""))                              //no taskId means such event is not associated to any tasks
                return;
            
            int listId = Global.definedTaskIdTimeLogInfo[taskId].listId;

            TimeSpan newActive = Global.definedTaskIdTimeLogInfo[taskId].active + idt.activeDelta;
            Global.definedTaskIdTimeLogInfo[taskId].active = newActive;

            Global.activeTotal += idt.activeDelta;

            string newActiveFormated = string.Format("{0:00}:{1:00}:{2:00}", newActive.Hours, newActive.Minutes, newActive.Seconds);
            string activeTotal = string.Format("{0:00}:{1:00}:{2:00}", Global.activeTotal.Hours, Global.activeTotal.Minutes, Global.activeTotal.Seconds);

            listView2.Items[listId].SubItems[1].Text = newActiveFormated;
            label17.Text = activeTotal;

            //listView2.EndUpdate();
        }

        //associations (Form 4)
        private void button1_Click(object sender, EventArgs e)
        {
            pollMutex.WaitOne();                      //prevent inserting into dictionary while making association changes

            Form4 f = new Form4();
            f.StartPosition = FormStartPosition.CenterParent;
            f.ShowDialog(this);

            if (Global.chosen == 1)
            {
                associateRaw();
                Global.chosen = 0;
            }

            pollMutex.ReleaseMutex();
        }

        //projects (Form 3
        private void button2_Click(object sender, EventArgs e)
        {
            pollMutex.WaitOne();                      //prevent inserting into dictionary while making association changes

            Form3 f = new Form3();
            f.StartPosition = FormStartPosition.CenterParent;
            f.ShowDialog(this);

            if (Global.chosen == 1)
            {
                label9.Text = Global.projectName;
                label13.Text = Global.workspaceName;

                associateRaw();

                try { startPollingMutex.ReleaseMutex(); } catch { }
                try { startIdleMonMutex.ReleaseMutex(); } catch { }

                Global.chosen = 0;
            }

            pollMutex.ReleaseMutex();
        }

        private void Form1_Load(object sender, EventArgs e)
        {


        }
    }
}
