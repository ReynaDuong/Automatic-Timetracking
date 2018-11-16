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

                    //MessageBox.Show("here");

                    // if it can be found, get the value from the URL bar
                    if (elmUrlBar != null)
                    {
                        AutomationPattern[] patterns = elmUrlBar.GetSupportedPatterns();
                        if (patterns.Length > 0)
                        {
                            ValuePattern val = (ValuePattern)elmUrlBar.GetCurrentPattern(patterns[0]);

                            if (val != null)
                            {
                                string url = string.Empty;

                                if (val.Current.Value.StartsWith("www"))
                                    url = "http://" + val.Current.Value + "/";
                                else
                                    url = val.Current.Value + "/";

                                string pattern = @"(https:\/\/www\.|http:\/\/www\.|https:\/\/|http:\/\/|www\.)?" +      //matches header such as http, https, ect..
                                                  "(.*?)/";     //matches the rest until / is reached

                                Match match = Regex.Match(url, pattern);

                                if (match.Success)
                                    return trim(match.Value);
                            }
                        }//end if length > 0
                    }//end elmUrlBar != null
                }//end for each loop
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return "";
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
