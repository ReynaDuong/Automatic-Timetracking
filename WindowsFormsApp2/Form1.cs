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
        int idleDebug = 0;

        private const uint MIN_IDLE_SECONDS = 3;    //minimum seconds that trips the idle counter
        private const int MIN_TIME_TO_POST = 0;     //minimum second of differences in duration before posting

        bool idling = false;
        uint seconds = 0;
        double idleSeconds = 0;
        double idleFreeze = 0;
        double idledAt = 0;
        double idleContinued = 0;

        string winTitle = string.Empty;             //current winTitle
        string psName = string.Empty;               //current psName
        string URL = string.Empty;                  //current URL

        string prevTitle = string.Empty;            //previous winTitle
        string prevPs = string.Empty;               //previous psName
        string prevUrl = string.Empty;              //previous URL
        
        string elapsedTime = string.Empty;
        Stopwatch stopwatch = new Stopwatch();
        TimeSpan ts = new TimeSpan();

        Mutex pollMutex = new Mutex();              //prevent from polling when choosing project/associations/deleting time entries, etc..
        Mutex idleMonitorMutex = new Mutex();       //protects 'idleSeconds' being written by posting/monitoring threads at the same time
        Mutex startPollingMutex = new Mutex();      //same for posting thread
        Mutex startIdleMonMutex = new Mutex();      //halt idle monitoring thread until project is selected 

        int i, j, k = 0;

        public Form1()
        //public Form1()
        {
            InitializeComponent();
            label6.Text = Global.name;
            label9.Text = "Choose a project to begin session...";
            
            //format
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.CenterToScreen();
            hideLabels();

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

                    ProcessInfo.getAll(out winTitle, out psName, out URL);            //get foreground window info

                    if (!prevTitle.Equals(winTitle))                                  //title changed
                    {
                        stopwatch.Stop();
                        ts = stopwatch.Elapsed;

                        idleMonitorMutex.WaitOne();
                        Event e = dictionaryInsert();
                        idleMonitorMutex.ReleaseMutex();
                        timeEntriesPost(e);                                           //post or update time entries
                        stopwatch.Restart();
                            
                        prevTitle = winTitle;
                        prevPs = psName;
                        prevUrl = URL;

                        label1.Text = prevTitle;
                        label2.Text = prevPs;
                        label4.Text = prevUrl;
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
            else if (!shouldPost(idt, e))                            //post only if more than a certain amount of differences in duration
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


        //allow post or update if ts is more than a specified seconds of lastPostedTs
        public bool shouldPost(EventValues idt, Event e)
        {
            uint ts = (uint) idt.ts.TotalSeconds;
            uint lastPostedTs = (uint)idt.lastPostedTs.TotalSeconds;

            if ((ts - lastPostedTs) >= MIN_TIME_TO_POST)
                return true;
            else
                return false;
        }

        //associate event to task ID and names for 'dictionaryEvent
        public List<dynamic> associateForDictionaryEvents()
        {
            Event e = new Event();
            EventValues idt = new EventValues();
            List<dynamic> associatedSet = new List<dynamic>();
            
            e.winTitle = prevTitle;
            e.process = prevPs;
            e.url = prevUrl;

            if (!Global.filter(e))
                return null;

            idt.ts = ts;
            idt.entryId = "";

            label28.Text = idleSeconds.ToString();

            idleFreeze = Math.Floor(idleSeconds);
            //if (idleFreeze >= MIN_IDLE_SECONDS)
            if (idleFreeze > 0)
            {

                label19.Text = prevPs;
                label20.Text = idt.ts.TotalSeconds.ToString();
                label21.Text = idleFreeze.ToString();

                if ((idt.ts.TotalSeconds - idleFreeze) < 0)                    //error, when idle time is more than duration
                {
                    idleMonitorMutex.WaitOne();

                    string title, ps, url = string.Empty;
                    ProcessInfo.getAll(out title, out ps, out url);
                    label24.Text = ps;


                    //MessageBox.Show("idle problem");
                    k++;
                    label29.Text = "Idle error occured# " + k.ToString();

                    idleMonitorMutex.ReleaseMutex();

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
                idt.taskName = "*No association*";
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

            if (associatedSet == null)
            {
                resetIdle();
                return null;
            }

            Event e = associatedSet[0];                         //key
            EventValues idt = associatedSet[1];                 //value

            try
            {
                if (Global.dictionaryEvents.ContainsKey(e))     //if an event is already in the table, update timespan and idle time
                {
                    Global.dictionaryEvents[e].ts = Global.dictionaryEvents[e].ts + ts;

                    if (idleFreeze > 0)
                        Global.dictionaryEvents[e].idle = Global.dictionaryEvents[e].idle + TimeSpan.FromSeconds(idleFreeze);

                    TimeSpan oldActive = Global.dictionaryEvents[e].active;                                                 //old active time
                    Global.dictionaryEvents[e].active = Global.dictionaryEvents[e].ts - Global.dictionaryEvents[e].idle;    //new active time
                    Global.dictionaryEvents[e].activeDelta = Global.dictionaryEvents[e].active - oldActive;                 //get differences of active times
                    
                    historyUpdate(0, e);
                    taskTimeLogUpdate(e);
                }
                else
                {
                    Global.dictionaryEvents.Add(e, idt);

                    historyUpdate(1, e);
                    taskTimeLogUpdate(e);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            
            resetIdle();
            return e;

        }//end dictionaryInsert

        //start association from scratch, clears out all current dictionaries
        private void associateRaw()
        {
            i = j = k = 0;

            listView1.Items.Clear();
            listView2.Items.Clear();
            label7.Text = i.ToString();

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
            resetIdle();
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

            //perform sorting
            var sorted = Global.definedTaskIdName.ToList();                         //convert dictionary to a list

            sorted.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));      //sort the list, by comparing the value of each pair

            Global.definedTaskIdName.Clear();                                       //clears dictionary

            foreach (KeyValuePair<string, string> t in sorted)                      //insert sorted pair values back into dictionary
            {
                taskId = t.Key;
                taskName = t.Value;

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

            System.Threading.Thread delete = new Thread(deleteEntries);
            delete.Start();
        }

        //thread to delete time entries
        private void deleteEntries()
        {
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;

            pollMutex.WaitOne();
            idleMonitorMutex.WaitOne();

            associateRaw();
            label1.Text = "Deleting time entries..";

            List<Dto.TimeEntryFullDto> entries = new List<Dto.TimeEntryFullDto>();
            while ((entries = API.FindTimeEntriesByWorkspace(Global.workspaceId)).Count > 0)
            {
                foreach (Dto.TimeEntryFullDto entry in entries)
                {
                    label2.Text = entry.description;
                    label4.Text = entry.id;
                    API.DeleteTimeEntry(Global.workspaceId, entry.id);
                }
            }

            label1.Text = "";
            label2.Text = "";
            label4.Text = "";

            idleMonitorMutex.ReleaseMutex();
            pollMutex.ReleaseMutex();

            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
        }

        //update listView
        private void historyUpdate(int newItem, Event e)
        {
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
        }

        //update times in time log
        private void taskTimeLogUpdate(Event e)
        {
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
        }

        //associations (Form 4)
        private void button1_Click(object sender, EventArgs e)
        {
            pollMutex.WaitOne();                      //prevent inserting into dictionary while making association changes
            idleMonitorMutex.WaitOne();

            Form4 f = new Form4();
            f.StartPosition = FormStartPosition.CenterParent;
            f.ShowDialog(this);

            if (Global.chosen == 1)
            {
                associateRaw();
                Global.chosen = 0;
            }

            idleMonitorMutex.ReleaseMutex();
            pollMutex.ReleaseMutex();
        }

        //projects (Form 3)
        private void button2_Click(object sender, EventArgs e)
        {
            pollMutex.WaitOne();                      //prevent inserting into dictionary while making association changes
            idleMonitorMutex.WaitOne();

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

            idleMonitorMutex.ReleaseMutex();
            pollMutex.ReleaseMutex();
        }

        //thread to monitor idle
        private void startIdleMonitoring()
        {
            startIdleMonMutex.WaitOne(-1, false);
            uint currentTick = 0;
            uint lastTick = 0;

            while (true)
            {
                idleMonitorMutex.WaitOne();
                System.Threading.Thread.Sleep(50);

                currentTick = (uint)Environment.TickCount;             //current tick count
                lastTick = ProcessInfo.getLastTick();                  //last input tick count

                if (lastTick == 0)                                     //fails to get tick
                    continue;

                seconds = (currentTick - lastTick) / 1000;             //convert to seconds
                if (seconds >= MIN_IDLE_SECONDS)
                {

                    if (idling == false)
                    {
                        idling = true;                                  //tripped
                        idledAt = stopwatch.Elapsed.TotalSeconds;
                    }
                }
                else if (idling == true)
                {
                    idling = false;
                    idleContinued += stopwatch.Elapsed.TotalSeconds - idledAt;
                }


                if (seconds >= MIN_IDLE_SECONDS && idling == true)
                    idleSeconds = idleContinued + (stopwatch.Elapsed.TotalSeconds - idledAt);

                
                if (idleDebug == 1)
                {
                    label14.Text = idleSeconds.ToString();
                    label8.Text = stopwatch.Elapsed.TotalSeconds.ToString();
                    label22.Text = "idled at    " + idledAt.ToString();
                    label23.Text = "cont. from    " + idleContinued.ToString();
                    label25.Text = seconds.ToString();
                }
                else
                    label14.Text = ((int) idleSeconds).ToString();

                

                idleMonitorMutex.ReleaseMutex();
            }
        }

        private void resetIdle()
        {
            j++;
            label26.Text = "RESET# " + j.ToString();
            label27.Text = prevPs;
            idling = false;
            idleSeconds = 0;
            idleContinued = 0;
            idledAt = 0;
        }

        private void hideLabels()
        {
            if (idleDebug == 0)
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
            if (idleDebug == 0)
            {
                idleDebug = 1;
                hideLabels();
            }
            else
            {
                idleDebug = 0;
                hideLabels();
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {


        }
    }
}
