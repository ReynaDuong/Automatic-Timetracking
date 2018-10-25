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
                            //Console.WriteLine("Chrome URL found: " + val.Current.Value);

                            if (val != null)
                            {

                                string url = string.Empty;

                                if (val.Current.Value.StartsWith("www"))
                                    url = "http://" + val.Current.Value + "/";
                                else
                                    url = val.Current.Value;
                                

                                string pattern = @"(https:\/\/www\.|http:\/\/www\.|https:\/\/|http:\/\/|www\.)?" +      //matches header such as http, https, ect..
                                                  "(.*?)/";     //matches the rest until / is reached

                                Match match = Regex.Match(url, pattern);

                                if (match.Success)
                                    return match.Value;
                                    

                                //return val.Current.Value;
                            }
                            else
                                return "";
                            
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }


            return "";

        }
        
    }
}
