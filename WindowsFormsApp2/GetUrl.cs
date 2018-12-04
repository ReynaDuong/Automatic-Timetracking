using System;
using System.Windows;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Automation;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Windows.Forms;


namespace WindowsFormsApp2
{
    class GetUrl
    {
        static public string chrome()
        {
            try
            {
                Process[] procsChrome = Process.GetProcessesByName("chrome");
                foreach (Process chrome in procsChrome)
                {
                    // the chrome process must have a window
                    if (chrome.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    // find the automation element
                    AutomationElement elm = AutomationElement.FromHandle(chrome.MainWindowHandle);
                    AutomationElement elmUrlBar = elm.FindFirst(TreeScope.Descendants, 
                                                                new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));

                    // if it can be found, get the value from the URL bar
                    if (elmUrlBar != null)
                    {
                        AutomationPattern[] patterns = elmUrlBar.GetSupportedPatterns();
                        if (patterns.Length > 0)
                        {
                            ValuePattern val = (ValuePattern)elmUrlBar.GetCurrentPattern(patterns[0]);

                            if (val != null)
                            {
                                string URL = string.Empty;

                                if (val.Current.Value.StartsWith("www"))
                                    URL = "http://" + val.Current.Value + "/";
                                else
                                    URL = val.Current.Value + "/";

                                string pattern = @"(https:\/\/www\.|http:\/\/www\.|https:\/\/|http:\/\/|www\.)?" +      //matches header such as http, https, ect..
                                                  "(.*?)/";     //matches the rest until / is reached
                                
                                Match match = Regex.Match(URL, pattern);
                                if (match.Success)
                                {
                                    URL = trim(match.Value);
                                    if (filterUrl(URL))
                                        return URL;
                                    else
                                        return "/";
                                }
                                    
                            }
                        }
                    }
                }//end for each loop
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return "/";
        }

        //get URL from title, chrome extension needed
        public static string fromChromeTitle(string winTitle, IntPtr handle)
        {
            string URL = string.Empty;
            //string pattern = @"\[(.*?)\[utd®\]";
            string pattern = @"\[(.*?)\]";
            Match match;

            for (int i = 0; i < 40; i++)
            {
                if (Global.winTitle2url.ContainsKey(winTitle))
                    return Global.winTitle2url[winTitle];

                System.Threading.Thread.Sleep(25);

                if (!filterTitle(winTitle))
                    return "/";

                match = Regex.Match(winTitle, pattern);
                if (match.Success)
                {
                    //return match.Value;
                    URL = trim2(match.Value);
                    Global.winTitle2url.Add(winTitle, URL);
                    return URL;
                    
                }
                else
                    winTitle = ProcessInfo.getWintitle(handle);
            }

            
            URL = chrome();
            Global.winTitle2url.Add(winTitle, URL);
            return URL;
        }

        private static bool filterTitle(string title)
        {
            if (title.Equals("") ||
                title.Equals("Untitled - Google Chrome") ||
                title.Equals("New Tab - Google Chrome") ||
                title.Equals("Downloads - Google Chrome") ||
                title.Equals("Extensions - Google Chrome") ||
                title.Equals("Settings - Google Chrome") ||
                title.Equals("Bookmarks - Google Chrome")  ||
                title.Equals("Disable developer mode extensions") || 
                title.Contains(".pdf")
             )
            {
                return false;
            }

            return true;
        }

        private static bool filterUrl(string URL)
        {
            if (URL.Equals("chrome-extension:") ||
               (URL.Equals(""))
                )
            {
                
                return false;
            }
            return true;
        }

        private static string trim2(string url)
        {
            string trimmed = string.Empty;
            int count = 0;

            if (url.StartsWith("[www."))
                count = 5;
            else if (url.StartsWith("["))
                count = 1;

            trimmed = url.Remove(0, count);
            //trimmed = trimmed.Substring(0, trimmed.Length - 6);
            trimmed = trimmed.Substring(0, trimmed.Length - 1);

            return trimmed;
        }

        //remove http, https, etc..
        private static string trim(string url)
        {
            string trimmed = string.Empty;
            int count = 0;

            //for testing, remove http or https, and trailing / from url
            if (url.StartsWith("https://www."))
                count = 12;
            else if (url.StartsWith("https://"))
                count = 8;

            else if (url.StartsWith("http://www."))
                count = 11;

            else if (url.StartsWith("http://"))
                count = 7;
            else if (url.StartsWith("www."))
                count = 4;
            

            trimmed = url.Remove(0, count);
            trimmed = trimmed.Substring(0, trimmed.Length - 1);

            return trimmed;
        }
    }
}
