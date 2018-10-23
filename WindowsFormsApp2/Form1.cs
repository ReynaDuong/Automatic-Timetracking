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
namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
       // System.Threading.Thread workerThread;

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
        Stopwatch time = new Stopwatch();
        TimeSpan ts;
        string TOK=string.Empty;
        string USERIDG = string.Empty;
       
        public Form1(dynamic Token,dynamic USERID)
        {
            InitializeComponent();
            TOK = Token;
            USERIDG = USERID;
          
            //format
            this.TopMost = true;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            listView1.Items[listView1.Items.Count - 1].EnsureVisible();

            //duration
            time.Start();

            //polling thread
            System.Threading.Thread workerThread;
            workerThread = new System.Threading.Thread(startPolling);
            workerThread.IsBackground = true;
            workerThread.Start();
         
            //dlg = new WinEventDelegate(WinEventProc);
           // IntPtr m_hhook = SetWinEventHook(EVENT_SYSTEM_CAPTURESTART, EVENT_SYSTEM_CAPTURESTART, IntPtr.Zero, dlg, 0, 0, WINEVENT_OUTOFCONTEXT);

            //dlg2 = new WinEventDelegate(WinEventProc2);
           // IntPtr m_hhook2 = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dlg2, 0, 0, WINEVENT_OUTOFCONTEXT);

        }
        WinEventDelegate dlg = null;    //prevent program from crashing if initialized here
        WinEventDelegate dlg2 = null;

        //triggers on mouse click
        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {

            if (ProcessInfo.getForegroundProcName().Equals("chrome"))
            {
                if (!(string.IsNullOrEmpty(prevTitle) || string.IsNullOrEmpty(prevPs)))
                {
                    if (!prevTitle.Equals(ProcessInfo.getForegroundWinTitle()))
                    {
                        ts = time.Elapsed;
                        elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

                        ListViewItem lv = new ListViewItem(prevTitle);
                        if (prevPs.Equals("chrome"))
                            lv.SubItems.Add(prevUrl);
                        else
                            lv.SubItems.Add("");

                        lv.SubItems.Add(prevPs);
                        lv.SubItems.Add(elapsedTime);

                        listView1.Items.Add(lv);

                        time.Restart();                 //reset stopwatch
                    }
                }

                //System.Threading.Thread workerThread;
                //workerThread = new System.Threading.Thread(startPolling);
                //workerThread.Start();

                listView1.Items[listView1.Items.Count - 1].EnsureVisible();
            }
        }

        //triggers on active window change
        /*
        public void WinEventProc2(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (ProcessInfo.getForegroundProcName().Equals("chrome"))
                return;
           
                //only add items to list when the first process has been recorded
                if (!(string.IsNullOrEmpty(prevTitle) || string.IsNullOrEmpty(prevPs)))
                {
                    ts = time.Elapsed;
                    elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

                    ListViewItem lv = new ListViewItem(prevTitle);
                    if (prevPs.Equals("chrome"))
                        lv.SubItems.Add(prevUrl);
                    else
                        lv.SubItems.Add("");

                    lv.SubItems.Add(prevPs);
                    lv.SubItems.Add(elapsedTime);
                    listView1.Items.Add(lv);

                    //time.Restart();                 //reset stopwatch
                }

                prevTitle = ProcessInfo.getForegroundWinTitle();
                prevPs = ProcessInfo.getForegroundProcName();
                prevUrl = "";

                label1.Text = prevTitle;
                label2.Text = prevPs;
                label4.Text = prevUrl;

                listView1.Items[listView1.Items.Count - 1].EnsureVisible();

                time.Restart();
           
        }

    */


        //retreive url from chrome
        public void startPolling()
        {
            //string url = string.Empty;
            //System.Threading.Thread.Sleep(1500); ;    //wait until the page url is loaded
            //url = GetUrl.chrome();

            //prevUrl = url;
            //label4.Text = url;

            //prevTitle = ProcessInfo.getForegroundWinTitle();
            //prevPs = ProcessInfo.getForegroundProcName();
            //label1.Text = prevTitle;
            //label2.Text = prevPs;

            //time.Restart();

            
            //polling
            while (true)
            {
                System.Threading.Thread.Sleep(50);
                if (ProcessInfo.getForegroundProcName().Equals("chrome"))
                {
                    DateTime Nowtime = DateTime.Now;
                    Nowtime = Nowtime.AddHours(5);
                    if (!prevTitle.Equals(ProcessInfo.getForegroundWinTitle())) //tab changed or navigated to different url
                    {
                        ts = time.Elapsed;
                        elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

                        ListViewItem lv = new ListViewItem(prevTitle);
                        if (prevPs.Equals("chrome"))
                            lv.SubItems.Add(prevUrl);
                        else
                            lv.SubItems.Add("");

                        lv.SubItems.Add(prevPs);
                        lv.SubItems.Add(elapsedTime);
                        listView1.Items.Add(lv);
                        listView1.Items[listView1.Items.Count - 1].EnsureVisible();
                        DateTime elapsedT = DateTime.Parse(elapsedTime);
                        elapsedT = Nowtime.AddTicks(elapsedT.TimeOfDay.Ticks);
                        var description = prevTitle + " " + prevPs;
                        POSTJSON(Nowtime, elapsedT, description);
                        time.Restart();                 //reset stopwatch


                        prevUrl = GetUrl.chrome();
                        label4.Text = prevUrl;
                    }

                    if (prevUrl.Equals(""))
                    {
                        prevUrl = GetUrl.chrome();
                        label4.Text = prevUrl;
                    }

                    prevTitle = ProcessInfo.getForegroundWinTitle();
                    prevPs = ProcessInfo.getForegroundProcName();
                    label1.Text = prevTitle;
                    label2.Text = prevPs;
                }
                else
                {
                    DateTime Nowtime = DateTime.Now;
                    Nowtime=Nowtime.AddHours(5);
                    if (!(string.IsNullOrEmpty(prevTitle) || string.IsNullOrEmpty(prevPs)))
                    {
                        
                            if (!prevTitle.Equals(ProcessInfo.getForegroundWinTitle()))
                        {
                            ts = time.Elapsed;
                            elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

                            ListViewItem lv = new ListViewItem(prevTitle);
                            if (prevPs.Equals("chrome"))
                                lv.SubItems.Add(prevUrl);
                            else
                                lv.SubItems.Add("");
                            
                                lv.SubItems.Add(prevPs);
                            lv.SubItems.Add(elapsedTime);
                            listView1.Items.Add(lv);
                            listView1.Items[listView1.Items.Count - 1].EnsureVisible();
                            DateTime elapsedT=DateTime.Parse(elapsedTime);
                            elapsedT=Nowtime.AddTicks(elapsedT.TimeOfDay.Ticks);
                            // MessageBox.Show(Nowtime.ToString() + " " + elapsedT.ToString());
                            var description = prevTitle + " " + prevPs;
                            var UserID= USERIDG;
                            label6.Text = "User: "+USERIDG;
                            POSTJSON(Nowtime, elapsedT, description);
                            time.Restart();                 //reset stopwatch
                        }
                    }
                    
                        prevTitle = ProcessInfo.getForegroundWinTitle();
                        prevPs = ProcessInfo.getForegroundProcName();
                        prevUrl = "";

                        label1.Text = prevTitle;
                        label2.Text = prevPs;
                        label4.Text = prevUrl;
                  
                }
               

                //label6.Text = label1.Text + " "+label2.Text;
                //label7.Text = elapsedTime;
            }//end while
            
        }
        public void POSTJSON(DateTime NowTime,DateTime Elapsed,String Description)
        {
            Rest Client2 = new Rest();
            Client2.endpoint = "https://api.clockify.me/api/workspaces/5baa4d06b079875917c7d342/timeEntries/";
            Client2.Token = TOK;
            Client2.httpMethod = httpVerb.POST;
            JSONTIMEENTRY TimeEntry = new JSONTIMEENTRY()
            {
                billable = "true",
                start=NowTime.ToString("yyyy-MM-ddTHH:mm:ss.fff")+"Z",
                description = Description,
                projectId = "5babb125b079875917ca2e66",//architecture
                taskId = "5babb3a9b079875917ca334e",
                end= Elapsed.ToString("yyyy-MM-ddTHH:mm:ss.fff")+"Z"
            };
           
            TimeEntry.tagIds = new string[1];
            var json = new JavaScriptSerializer().Serialize(TimeEntry);
            Client2.postJSON = json;
            String Response = String.Empty;
            Response = Client2.MakeRequest();
            //MessageBox.Show(Response.ToString());
            
            
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
    }
}
