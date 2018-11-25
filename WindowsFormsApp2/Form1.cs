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
        uint idleSeconds = 0;

        string prevTitle = string.Empty;
        string prevPs = string.Empty;
        string prevUrl = string.Empty;
        string elapsedTime = string.Empty;
        Stopwatch stopwatch = new Stopwatch();
        TimeSpan ts;

        Mutex dictionaryEventMutex = new Mutex();   //protects 'dictionaryEvent' being written by posting/polling threads at the same time
        Mutex idleMonitorMutex = new Mutex();       //protects 'idleSeconds' being written by posting/monitoring threads at the same time
        Mutex pollMutex = new Mutex();

        Mutex startPostingMutex = new Mutex();      //halt posting thread until project is selected
        Mutex startPollingMutex = new Mutex();      //same for posting thread
        Mutex startPoliceMutex = new Mutex();       //same for policing thread

        Mutex startIdleMonMutex = new Mutex();      //halt idle monitoring thread until project is selected 

        Dictionary<string, string> winTitle2url = new Dictionary<string, string>();

        int i = 0;
        int j = 0;

        int redo = 0;

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
            startPostingMutex.WaitOne();
            startIdleMonMutex.WaitOne();
            startPoliceMutex.WaitOne();

            //polling thread
            System.Threading.Thread pollingThread;
            pollingThread = new System.Threading.Thread(startPolling);
            pollingThread.IsBackground = true;
            pollingThread.Start();

            //policing thread
            System.Threading.Thread policeThread;
            policeThread = new System.Threading.Thread(startPolicing);
            policeThread.IsBackground = true;
            policeThread.Start();

            //posting thread
            System.Threading.Thread postThread;
            postThread = new System.Threading.Thread(startPosting);
            postThread.IsBackground = true;
            postThread.Start();

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

            while (true)
            {
                pollMutex.WaitOne();
                System.Threading.Thread.Sleep(50);

                if (ProcessInfo.getForegroundProcName().Equals("chrome"))
                {
                    if (!prevTitle.Equals(ProcessInfo.getForegroundWinTitle()) || redo == 1) //tab changed or navigated to different url
                    {
                        ts = stopwatch.Elapsed;
                        dictionaryInsert();
                        stopwatch.Restart();
                        
                        prevTitle = ProcessInfo.getForegroundWinTitle();
                        prevPs = ProcessInfo.getForegroundProcName().ToLower();
                        prevUrl = GetUrl.fromChromeTitle(prevTitle);

                        label1.Text = prevTitle;
                        label2.Text = prevPs;
                        label4.Text = prevUrl;

                        redo = 0;
                    }
                }
                else if (!ProcessInfo.getForegroundProcName().Equals("chrome"))
                {
                    if (!prevTitle.Equals(ProcessInfo.getForegroundWinTitle()) || redo == 2)
                    {
                        ts = stopwatch.Elapsed;
                        dictionaryInsert();
                        stopwatch.Restart();

                        prevTitle = ProcessInfo.getForegroundWinTitle();
                        prevPs = ProcessInfo.getForegroundProcName().ToLower();
                        prevUrl = "";

                        label1.Text = prevTitle;
                        label2.Text = prevPs;
                        label4.Text = prevUrl;
                        
                        redo = 0;
                    }
                }
                
                pollMutex.ReleaseMutex();
            }//end while
        }//end polling thread

        //police thread, in case of URL being incorrectly defined due to fast switching between chrome and non-chrome processes
        private void startPolicing()
        {
            startPoliceMutex.WaitOne(-1, false);

            while (true)
            {
                System.Threading.Thread.Sleep(50);

                if (prevPs.Equals("chrome") && prevUrl.Equals(""))
                {
                    pollMutex.WaitOne();
                    redo = 1;
                    pollMutex.ReleaseMutex();
                }

                else if (!prevPs.Equals("chrome") && !prevUrl.Equals(""))
                {
                    pollMutex.WaitOne();
                    redo = 2;
                    pollMutex.ReleaseMutex();
                }
            }
        }
        
        //thread to post
        public void startPosting()
        {
            //wait for project selection before starting
            startPostingMutex.WaitOne(-1, false);                                

            DateTime start = DateTime.Today.AddHours(6.0);      //adds 6 hours for central time
            DateTime end;

            string description = string.Empty;
            string entryId = string.Empty;
            string value = string.Empty;                        //either process name or URL
            string taskId = string.Empty;

            while (true)
            {
                System.Threading.Thread.Sleep(5000);

                //looping through dictionary to post or put depending on if the event has been posted
                dictionaryEventMutex.WaitOne();

                label7.Text = i.ToString();
                i++;

                try
                {
                    foreach (var x in Global.dictionaryEvents)
                    {
                        //undefined events (events with empty task ID) will not be uploaded
                        if (x.Value.taskId.Equals(""))
                            continue;

                        if (x.Value.entryId.Equals(""))                         //POST, empty ID means this event hasn't been posted
                        {
                            if (x.Key.process.Equals("chrome"))
                            {
                                description = x.Key.url;
                                value = x.Key.url;
                                taskId = x.Value.taskId;
                            }

                            else
                            {
                                description = x.Key.process;
                                value = x.Key.process;
                                taskId = x.Value.taskId;
                            }


                            end = DateTime.Parse(x.Value.active.ToString()).AddHours(6.0);

                            dynamic res = API.AddTimeEntry(start, end, description, Global.workspaceId, Global.projectId, taskId);

                            Global.dictionaryEvents[x.Key].entryId = res.id;    //update dictionary value to include entry ID returned from clockify
                        }
                        else                                                    //PUT   
                        {
                            if (x.Key.process.Equals("chrome"))
                            {
                                description = x.Key.url;
                                value = x.Key.url;
                                taskId = x.Value.taskId;
                            }
                            else
                            {
                                description = x.Key.process;
                                value = x.Key.process;
                                taskId = x.Value.taskId;
                            }

                            entryId = x.Value.entryId;
                            end = DateTime.Parse(x.Value.active.ToString()).AddHours(6.0);

                            API.UpdateTimeEntry(start, end, description, entryId, Global.workspaceId, Global.projectId, taskId);
                        }
                    }//end foreach
                }
                catch (Exception ex)
                {
                    MessageBox.Show("POSTING - " + ex.ToString());
                }
                dictionaryEventMutex.ReleaseMutex();
            }//end while
        }//end posting thread

        //associate event to task ID and names for 'dictionaryEvent'
        public List<dynamic> associateForDictionaryEvents()
        {
            Event e = new Event();
            EventValues idt = new EventValues();
            List<dynamic> associatedSet = new List<dynamic>();

            if (prevPs.Equals("chrome"))        //in case of user switching focus too fast between chrome and other applications
                e.url = prevUrl;
            else
                e.url = "";

            e.winTitle = prevTitle;
            e.process = prevPs;

            idt.entryId = "";
            idt.ts = ts;

            if (idleSeconds > MIN_IDLE_SECONDS)
            {
                if ((idt.ts.TotalSeconds - idleSeconds) < 0)
                    idt.idle = TimeSpan.FromSeconds(0.0);
                else
                    idt.idle = TimeSpan.FromSeconds(idleSeconds);
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
        public void dictionaryInsert()
        {
            dictionaryEventMutex.WaitOne();

            //perform association
            List<dynamic> associatedSet = associateForDictionaryEvents();

            Event e = associatedSet[0];                         //key
            EventValues idt = associatedSet[1];                 //value
            
            try
            {
                if (Global.dictionaryEvents.ContainsKey(e))     //if an event is already in the table, update timespan and idle time
                {
                    Global.dictionaryEvents[e].ts = Global.dictionaryEvents[e].ts + ts;

                    if (idleSeconds >= MIN_IDLE_SECONDS)
                        Global.dictionaryEvents[e].idle = Global.dictionaryEvents[e].idle + TimeSpan.FromSeconds(idleSeconds);

                    TimeSpan oldActive = Global.dictionaryEvents[e].active;                                                 //old active time
                    Global.dictionaryEvents[e].active = Global.dictionaryEvents[e].ts - Global.dictionaryEvents[e].idle;    //new active time
                    Global.dictionaryEvents[e].activeDelta = Global.dictionaryEvents[e].active - oldActive;                 //get differences of active times
                    
                    historyUpdate(0, e);
                    taskTimeLogUpdate(e);
                }
                else
                {
                    if (!filter(e))
                    {
                        dictionaryEventMutex.ReleaseMutex();
                        return;
                    }
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
            dictionaryEventMutex.ReleaseMutex();
        }//end dictionaryInsert

        //start association from scratch, clears out all current dictionaries
        private void associateRaw()
        {
            listView1.Items.Clear();
            listView2.Items.Clear();

            string prevTitle = string.Empty;
            string prevPs = string.Empty;
            string prevUrl = string.Empty;

            Global.dictionaryEvents.Clear();
            Global.associations.Clear();
            Global.definedTaskIdName.Clear();
            Global.definedTaskIdTimeLogInfo.Clear();
            Global.activeTotal = TimeSpan.FromSeconds(0);

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
            Global.allTaskIdName.Clear();

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
                
            dictionaryEventMutex.WaitOne();
            
            associateRaw();
            
            List<Dto.TimeEntryFullDto> entries = new List<Dto.TimeEntryFullDto>();
            while ( (entries = API.FindTimeEntriesByWorkspace(Global.workspaceId)).Count > 0 )
            {
                foreach (Dto.TimeEntryFullDto entry in entries)
                {

                    API.DeleteTimeEntry(Global.workspaceId, entry.id);
                }
            }

            dictionaryEventMutex.ReleaseMutex();
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
                try { startPostingMutex.ReleaseMutex(); } catch { }
                try { startIdleMonMutex.ReleaseMutex(); } catch { }
                try { startPoliceMutex.ReleaseMutex(); } catch { }

                Global.chosen = 0;
            }

            pollMutex.ReleaseMutex();
        }

        //filters url and process names, returns true if entry is good for insert 
        public bool filter(Event e)
        {
            if (string.IsNullOrEmpty(prevTitle) || string.IsNullOrEmpty(prevPs))
                return false;

            string pattern = @"\.(com|net|edu|org)$";

            Match match = Regex.Match(e.winTitle, pattern);

            if (match.Success)                                                  //if winTitle is an url
                return false;

            else if (e.process.Equals("chrome"))
            {
                if (e.url.Equals("/") ||
                    e.url.Equals("") ||
                    e.url.Equals("chrome:")
                   )
                {
                    return false;
                }
            }
            else if (e.process.Equals("explorer"))
            {
                if (e.winTitle.Equals("Program Manager") ||
                    e.winTitle.Equals("File Explorer") ||
                    e.winTitle.Equals("")
                    )
                {
                    return false;
                }

            }
            else if (e.process.Equals("idle") ||
                     e.process.Equals("ShellExperienceHost") ||
                    (e.winTitle.Equals("File Explorer") && (e.winTitle.Equals("explorer"))) ||
                     e.winTitle.Equals("")
                )
            {
                return false;
            }

            return true;
        }//end filter

        private void Form1_Load(object sender, EventArgs e)
        {


        }
    }
}
