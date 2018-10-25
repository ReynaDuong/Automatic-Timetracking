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
        //windows event hook initializations---------------------------------------------------
        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        //call back pointer
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;             //win title change event
        private const uint EVENT_OBJECT_NAMECHANGE = 0X800C;        //win title change event - can detect tab change
        private const uint  EVENT_SYSTEM_CAPTURESTART = 0x0008;

        string prevTitle = string.Empty;
        string prevPs = string.Empty;
        string prevUrl = string.Empty;
        List<String> NameList = new List<String>();
        string elapsedTime = string.Empty;
        Stopwatch stopwatch = new Stopwatch();
        TimeSpan ts;
        string TOK=string.Empty;
        string USERIDG = string.Empty;
        Mutex myMutex = new Mutex();


        Dictionary<Event, EntryIdTime> dictionaryEvents = new Dictionary<Event, EntryIdTime>();
        Dictionary<string, string> winTitle2url = new Dictionary<string, string>();



        public Form1(dynamic Token,dynamic USERID)
        //public Form1()
        {
            InitializeComponent();
            TOK = Token;

            label6.Text = USERID;

            //format
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            listView1.Items[listView1.Items.Count - 1].EnsureVisible();

            //duration
            stopwatch.Start();

            //polling thread
            System.Threading.Thread pollingThread;
            pollingThread = new System.Threading.Thread(startPolling);
            pollingThread.IsBackground = true;
            pollingThread.Start();
            
            System.Threading.Thread postThread;
            postThread = new System.Threading.Thread(startPosting);
            postThread.IsBackground = true;
            postThread.Start();



            //dlg = new WinEventDelegate(WinEventProc);
            // IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_CAPTURESTART, EVENT_SYSTEM_CAPTURESTART, IntPtr.Zero, dlg, 0, 0, WINEVENT_OUTOFCONTEXT);
        }
        WinEventDelegate dlg = null;    //prevent program from crashing if initialized here

        //triggers on mouse click - reserved
        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {

        }

        //thread to poll
        public void startPolling()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(50);
                if (ProcessInfo.getForegroundProcName().Equals("chrome"))
                {
                    if (!prevTitle.Equals(ProcessInfo.getForegroundWinTitle())) //tab changed or navigated to different url
                    {
                        //stop timer
                        ts = stopwatch.Elapsed;

                        //new objects of Event and EntryIdTime for dictionary
                        Event e = new Event();
                        EntryIdTime idt = new EntryIdTime("", ts);

                        //set title and process
                        e.winTitle = prevTitle;
                        if (prevPs.Equals("chrome"))        //in case of user switching focus too fast between chrome and other applications
                            e.url = prevUrl;
                        else
                            e.url = "";
                        e.process = prevPs;

                        //insert events into dictionary, similar events will be updated with new TimeSpan ts
                        dictionaryInsert(e, idt);

                        //startPosting();

                        //restart timer
                        stopwatch.Restart();                 

                        //update title and process
                        prevTitle = ProcessInfo.getForegroundWinTitle();
                        prevPs = ProcessInfo.getForegroundProcName();

                        //fetch URL from visited site from dictionary to prevent re-seeks
                        if (winTitle2url.ContainsKey(prevTitle))
                        {
                            prevUrl = winTitle2url[prevTitle];
                            label4.Text = prevUrl;
                            //label6.Text = "Skipped";
                        }
                        else
                        {
                            //label6.Text = "Ran";
                            prevUrl = GetUrl.chrome();
                            winTitle2url.Add(prevTitle, prevUrl);
                        }
                        
                        label4.Text = prevUrl;
                    }

                    //in case URL is modified by regular applications due to fast focus switchings
                    if (prevUrl.Equals(""))
                    {
                       
                        if (winTitle2url.ContainsKey(prevTitle))
                            prevUrl = winTitle2url[prevTitle];
                        else
                        {
                            //label6.Text = "Oops";
                            prevUrl = GetUrl.chrome();
                            winTitle2url.Add(prevTitle, prevUrl);
                           
                        }
                        
                        prevUrl = GetUrl.chrome();
                        label4.Text = prevUrl;
                    }

                    label1.Text = prevTitle;
                    label2.Text = prevPs;
                }//end chrome
                else
                {
                    if (!(string.IsNullOrEmpty(prevTitle) || string.IsNullOrEmpty(prevPs)))     //prevent empty entry being inserted into listview
                    {
                        
                        if (!prevTitle.Equals(ProcessInfo.getForegroundWinTitle()))
                        {
                            //stop timer
                            ts = stopwatch.Elapsed;

                            //new objects of Event and EntryIdTime for dictionary
                            Event e = new Event();
                            EntryIdTime idt = new EntryIdTime("", ts);

                            //set title and process
                            e.winTitle = prevTitle;
                            if (prevPs.Equals("chrome"))        //in case of user switching focus too fast between chrome and other applications
                                e.url = prevUrl;
                            else
                                e.url = "";
                            e.process = prevPs;

                            //insert events into dictionary, similar events will be updated with new TimeSpan ts
                            dictionaryInsert(e, idt);
                            
                            //startPosting();

                            //restart timer
                            stopwatch.Restart();                 
                        }
                    }
                    prevTitle = ProcessInfo.getForegroundWinTitle();
                    prevPs = ProcessInfo.getForegroundProcName();
                    prevUrl = "";

                    label1.Text = prevTitle;
                    label2.Text = prevPs;
                    label4.Text = prevUrl;
                }//end non-chrome
            }//end while
        }

        //thread to post
        public void startPosting()
        {
            DateTime start = DateTime.Today.AddHours(5.0);      //adds 5 hours for central time
            DateTime end;
            

            //MessageBox.Show(start.ToString());

            string description = string.Empty;
            string entryId = string.Empty;

            while (true)
            {
                System.Threading.Thread.Sleep(5000);

                //looping through dictionary to post or put depending on if the event has been posted

                myMutex.WaitOne();
                try
                {
                    MessageBox.Show("posting");
                    foreach (var x in dictionaryEvents)
                    {
                        if (x.Value.id.Equals(""))                  //POST, empty ID means this event hasn't been posted
                        {
                            if (x.Key.process.Equals("chrome"))
                                description = x.Key.url + " " + "(" + x.Key.process + ")";
                            else
                                description = x.Key.winTitle + " " + "(" + x.Key.process + ")";

                            end = DateTime.Parse(x.Value.ts.ToString()).AddHours(5.0);

                            dynamic res = POSTJSON(start, end, description, "", httpVerb.POST);
                            dictionaryEvents[x.Key].id = res.id;    //update dictionary value to include entry ID returned from clockify
                        }
                        else                                        //PUT
                        {
                            if (x.Key.process.Equals("chrome"))
                                description = x.Key.url + " " + "(" + x.Key.process + ")";
                            else
                                description = x.Key.winTitle + " " + "(" + x.Key.process + ")";

                            entryId = x.Value.id;
                            end = DateTime.Parse(x.Value.ts.ToString()).AddHours(5.0);

                            POSTJSON(start, end, description, entryId, httpVerb.PUT);
                        }
                    }//end foreach
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                myMutex.ReleaseMutex();

            }//end while
        }

        public dynamic POSTJSON(DateTime start,DateTime end, string description, string entryId, httpVerb verb)
        {
            Rest Client2 = new Rest();
            Client2.httpMethod = verb;
            Client2.Token = TOK;
            
            if (entryId.Equals("")) 
                Client2.endpoint = "https://api.clockify.me/api/workspaces/5badbd30b079875917cd57ca/timeEntries/";
            else
                Client2.endpoint = "https://api.clockify.me/api/workspaces/5badbd30b079875917cd57ca/timeEntries/" + entryId;

            JSONTIMEENTRY TimeEntry = new JSONTIMEENTRY()
                {
                    billable = "true",
                    start = start.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z",
                    description = description,
                    projectId = "5bbe5e43b079870146fc4137",//architecture
                    taskId = "5bbe5ffbb079870146fc44d3",
                    end = end.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z"
                };

            //MessageBox.Show(TimeEntry.start);
            //MessageBox.Show(TimeEntry.end);

            TimeEntry.tagIds = new string[1];
            var json = new JavaScriptSerializer().Serialize(TimeEntry);
            Client2.body = json;

            string Response = Client2.MakeRequest();
            
            return JsonConvert.DeserializeObject(Response);                     //returns a deserialized response object
        }

        public void dictionaryInsert(Event e, EntryIdTime idt)
        {
            myMutex.WaitOne();
            MessageBox.Show("insert");
            if (dictionaryEvents.ContainsKey(e))                                //if an event is already in the table, update timespan
                dictionaryEvents[e].ts = dictionaryEvents[e].ts + ts;
            else
            {
                if (!filter(e))
                    return;

                dictionaryEvents.Add(e, idt);
            }
                    
            
            //clear and post all onto listview1
            listView1.Items.Clear();
            string elapsedTime;
            foreach (var x in dictionaryEvents)                               
            {
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", x.Value.ts.Hours, x.Value.ts.Minutes, x.Value.ts.Seconds);
                ListViewItem lv = new ListViewItem(x.Key.winTitle);

                lv.SubItems.Add(x.Key.url);
                lv.SubItems.Add(x.Key.process);
                lv.SubItems.Add(elapsedTime);
                listView1.Items.Add(lv);
                listView1.Items[listView1.Items.Count - 1].EnsureVisible();
            }
            myMutex.ReleaseMutex();
        }

        public bool filter(Event e)                                           //returns true if entry is good for insert 
        {
            string pattern = @"(https:\/\/www\.|http:\/\/www\.|https:\/\/|http:\/\/|www\.)?" +      //matches header such as http, https, ect..
                   "(.*?)/";     //matches the rest until / is reached

            Match match = Regex.Match(e.winTitle, pattern);

            if (match.Success || 
                e.process.Equals("idle") ||
                e.process.Equals("explorer") ||
                e.winTitle.Equals("")
                )
            {
                return false;
            }

            return true;
        }



        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {


        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
