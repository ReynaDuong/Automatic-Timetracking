using System;
using System.Diagnostics;
using System.Windows.Automation;
using System.Text.RegularExpressions;
using System.Windows.Forms;


namespace TimeTracker.View
{
    class GetUrl
    {
        static public string Chrome()
        {
            try
            {
                var procsChrome = Process.GetProcessesByName("chrome");
                foreach (var chrome in procsChrome)
                {
                    // the chrome process must have a window
                    if (chrome.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    // find the automation element
                    var elm = AutomationElement.FromHandle(chrome.MainWindowHandle);
                    var elmUrlBar = elm.FindFirst(TreeScope.Descendants, 
                                                                new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));

                    // if it can be found, get the value from the URL bar
                    if (elmUrlBar != null)
                    {
                        var patterns = elmUrlBar.GetSupportedPatterns();
                        if (patterns.Length > 0)
                        {
                            var val = (ValuePattern)elmUrlBar.GetCurrentPattern(patterns[0]);

                            if (val != null)
                            {
                                var url = string.Empty;

                                if (val.Current.Value.StartsWith("www"))
                                {
	                                url = "http://" + val.Current.Value + "/";
                                }
                                else
                                {
	                                url = val.Current.Value + "/";
                                }

                                var pattern = @"(https:\/\/www\.|http:\/\/www\.|https:\/\/|http:\/\/|www\.)?" +      //matches header such as http, https, ect..
                                                  "(.*?)/";     //matches the rest until / is reached
                                
                                var match = Regex.Match(url, pattern);
                                if (match.Success)
                                {
                                    url = Trim(match.Value);
                                    if (FilterUrl(url))
                                    {
	                                    return url;
                                    }
                                    else
                                    {
	                                    return "/";
                                    }
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
        public static string FromChromeTitle(string winTitle, IntPtr handle)
        {
            var url = string.Empty;
            //string pattern = @"\[(.*?)\[utd®\]";
            var pattern = @"\[(.*?)\]";
            Match match;

            for (var i = 0; i < 40; i++)
            {
                if (Global.winTitle2Url.ContainsKey(winTitle))
                {
	                return Global.winTitle2Url[winTitle];
                }

                System.Threading.Thread.Sleep(25);

                if (!FilterTitle(winTitle))
                {
	                return "/";
                }

                match = Regex.Match(winTitle, pattern);
                if (match.Success)
                {
                    //return match.Value;
                    url = Trim2(match.Value);
                    Global.winTitle2Url.Add(winTitle, url);
                    return url;
                    
                }
                else
                {
	                winTitle = ProcessInfo.GetWinTitle(handle);
                }
            }

            
            url = Chrome();
            Global.winTitle2Url.Add(winTitle, url);
            return url;
        }

        private static bool FilterTitle(string title)
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

        private static bool FilterUrl(string url)
        {
            if (url.Equals("chrome-extension:") ||
               (url.Equals(""))
                )
            {
                
                return false;
            }
            return true;
        }

        private static string Trim2(string url)
        {
            var trimmed = string.Empty;
            var count = 0;

            if (url.StartsWith("[www."))
            {
	            count = 5;
            }
            else if (url.StartsWith("["))
            {
	            count = 1;
            }

            trimmed = url.Remove(0, count);
            //trimmed = trimmed.Substring(0, trimmed.Length - 6);
            trimmed = trimmed.Substring(0, trimmed.Length - 1);

            return trimmed;
        }

        //remove http, https, etc..
        private static string Trim(string url)
        {
            var trimmed = string.Empty;
            var count = 0;

            //for testing, remove http or https, and trailing / from url
            if (url.StartsWith("https://www."))
            {
	            count = 12;
            }
            else if (url.StartsWith("https://"))
            {
	            count = 8;
            }

            else if (url.StartsWith("http://www."))
            {
	            count = 11;
            }

            else if (url.StartsWith("http://"))
            {
	            count = 7;
            }
            else if (url.StartsWith("www."))
            {
	            count = 4;
            }


            trimmed = url.Remove(0, count);
            trimmed = trimmed.Substring(0, trimmed.Length - 1);

            return trimmed;
        }
    }
}
