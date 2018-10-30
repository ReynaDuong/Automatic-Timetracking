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
        Mutex myMutex = new Mutex();                                //using mutex to prevent threads from modifying dictionaryEvents values simultaneously

        Dictionary<Event, EntryIdTime> dictionaryEvents = new Dictionary<Event, EntryIdTime>();
        Dictionary<string, string> winTitle2url = new Dictionary<string, string>();

        int i = 0;

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

            //posting thread
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
                    try
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

                            //restart timer
                            stopwatch.Restart();

                            //update title and process
                            prevTitle = ProcessInfo.getForegroundWinTitle();
                            prevPs = ProcessInfo.getForegroundProcName();

                            //fetch URL of visited site from dictionary to prevent re-seeks
                            if (winTitle2url.ContainsKey(prevTitle))
                            {
                                label8.Text = "Skipped";
                                prevUrl = winTitle2url[prevTitle];
                                label4.Text = prevUrl;
                            }
                            else
                            {
                                label8.Text = "Ran";
                                prevUrl = GetUrl.chrome();

                                prevTitle = ProcessInfo.getForegroundWinTitle();        //updating winTitle after grabbing URL for higher accuracy...
                                                                                        //...due to it not changing at the same as the URL in chrome
                                winTitle2url.Add(prevTitle, prevUrl);                   //stores into table
                            }

                            label4.Text = prevUrl;
                        }

                        //in case URL is modified by regular applications due to fast focus switchings
                        if (prevUrl.Equals(""))
                        {
                            do
                            {
                                label8.Text = "Oops";
                                prevUrl = GetUrl.chrome();
                                prevTitle = ProcessInfo.getForegroundWinTitle();        //updating winTitle

                                winTitle2url.Add(prevTitle, prevUrl);                   //stores into table
                                label4.Text = prevUrl;
                            }
                            while (prevUrl.Equals(""));

                            
                        }
                    }
                    catch (Exception e)
                    {
                        /*
                        in case of extreme fast window switching causing 'prevTitle'
                        to be corrupted during capture and ultimately leading winTitle2url.ContainsKey(prevTitle)
                        to return FALSE, when it supposed to be TRUE if the dictionary already have such
                        an entry (when captured correctly), an exception will be thrown when trying to insert a non-corrupted version
                        of 'prevTitle' into dictionary, which is obtained right before winTitle2url.Add(prevTitle, prevUrl)
                        in line 147
                        */
                        //MessageBox.Show(e.ToString());

                        winTitle2url.Remove(prevTitle);                                 //remove corrupted winTitle
                        continue;                                                       
                        //MessageBox.Show(e.ToString());                               
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
                    label8.Text = "";
                }//end non-chrome
            }//end while
        }//end polling thread

        //thread to post
        public void startPosting()
        {
            DateTime start = DateTime.Today.AddHours(5.0);      //adds 5 hours for central time
            DateTime end;
            
            string description = string.Empty;
            string entryId = string.Empty;

            while (true)
            {
                System.Threading.Thread.Sleep(5000);

                //looping through dictionary to post or put depending on if the event has been posted
                myMutex.WaitOne();

                label7.Text = i.ToString();
                i++;

                try
                {
                    foreach (var x in dictionaryEvents)
                    {
                        if (x.Value.id.Equals(""))                  //POST, empty ID means this event hasn't been posted
                        {
                            if (x.Key.process.Equals("chrome"))
                                description = x.Key.winTitle + " - " + x.Key.url + " " + "(" + x.Key.process + ")";
                            else
                                description = x.Key.winTitle + " " + "(" + x.Key.process + ")";

                            end = DateTime.Parse(x.Value.ts.ToString()).AddHours(5.0);

                            dynamic res = POSTJSON(start, end, description, "", httpVerb.POST);
                            dictionaryEvents[x.Key].id = res.id;    //update dictionary value to include entry ID returned from clockify
                        }
                        else                                        //PUT
                        {
                            if (x.Key.process.Equals("chrome"))
                                description = x.Key.winTitle + " - " + x.Key.url + " " + "(" + x.Key.process + ")";
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
        }//end posting thread

        public dynamic POSTJSON(DateTime start,DateTime end, string description, string entryId, httpVerb verb)
        {
            Rest Client2 = new Rest();
            Client2.httpMethod = verb;
            Client2.Token = TOK;
            
            if (verb.Equals("POST")) 
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
            
            //TimeEntry.tagIds = new string[1];
            var json = new JavaScriptSerializer().Serialize(TimeEntry);
            Client2.body = json;

            string Response = Client2.MakeRequest();
            
            return JsonConvert.DeserializeObject(Response);                     //returns a deserialized response object
        }

        public void dictionaryInsert(Event e, EntryIdTime idt)
        {
            myMutex.WaitOne();
            if (dictionaryEvents.ContainsKey(e))                                //if an event is already in the table, update timespan
                dictionaryEvents[e].ts = dictionaryEvents[e].ts + ts;
            else
            {
                if (!filter(e))
                {
                    myMutex.ReleaseMutex();
                    return;
                }

                dictionaryEvents.Add(e, idt);
            }
                    
            
            //clear and post all onto listview1
            listView1.Items.Clear();
            string elapsedTime;
            listView1.BeginUpdate();
            foreach (var x in dictionaryEvents)                               
            {
                elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", x.Value.ts.Hours, x.Value.ts.Minutes, x.Value.ts.Seconds);
                ListViewItem lv = new ListViewItem(x.Key.winTitle);

                lv.SubItems.Add(x.Key.url);
                lv.SubItems.Add(x.Key.process);
                lv.SubItems.Add(elapsedTime);
                listView1.Items.Add(lv);
            }
            listView1.EndUpdate();
            listView1.Items[listView1.Items.Count - 1].EnsureVisible();
            myMutex.ReleaseMutex();
        }

        public bool filter(Event e)                                           //returns true if entry is good for insert 
        {
            string pattern = @"^(https:\/\/www\.|http:\/\/www\.|https:\/\/|http:\/\/|www\.)";       //starts with these

            Match match = Regex.Match(e.winTitle, pattern);

            //MessageBox.Show(match.Value);
            

            if (match.Success)                                                  //if winTitle is an url
            {
                MessageBox.Show(match.Value);
                return false;
            }
                
            else if (e.process.Equals("chrome"))
            {
                //MessageBox.Show("Here2");
                if (e.winTitle.Equals("Untitled - Google Chrome") ||
                    e.winTitle.Equals("New Tab - Google Chrome") ||
                    e.winTitle.Equals("Downloads - Google Chrome") ||
                    e.url.Equals("/") ||
                    e.url.Equals("")
                   )
                {
                    return false;
                }
            }
            else if (e.process.Equals("explorer"))
            {
                if (e.winTitle.Equals("Program Manager"))
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
                //MessageBox.Show("Here3");
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
