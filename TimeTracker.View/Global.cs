﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace TimeTracker.View
{
    class Global
    {
        public static string token = string.Empty;
        public static string name = string.Empty;
        public static string projectId = string.Empty;
        public static string projectName = string.Empty;
        public static string workspaceId = string.Empty;
        public static string workspaceName = string.Empty;
        public static int chosen = 0;


        public static Dictionary<Event, EventValues> dictionaryEvents = new Dictionary<Event, EventValues>();                //user's live event->task association
        public static Dictionary<string, Dto.TaskDto> associations = new Dictionary<string, Dto.TaskDto>();                  //all event->task association
        public static Dictionary<string, string> allTaskIdName = new Dictionary<string, string>();                           //all taskId->taskName of a project
        public static Dictionary<string, string> definedTaskIdName = new Dictionary<string, string>();                       //defined taskId->taskName
        public static Dictionary<string, TimeLogInfo> definedTaskIdTimeLogInfo= new Dictionary<string, TimeLogInfo>();       //defined taskId->timeLog (contains listID and active time)
        public static Dictionary<string, string> winTitle2Url = new Dictionary<string, string>();                            //holds chrome winTItle->URL

        public static TimeSpan activeTotal;

        public static Mutex idleMonitorMutex = new Mutex();
        public static Stopwatch stopwatch = new Stopwatch();
        public static int idleReset = 0;


        public static void ClearGlobals()
        {
            dictionaryEvents.Clear();
            associations.Clear();
            allTaskIdName.Clear();
            definedTaskIdName.Clear();
            definedTaskIdTimeLogInfo.Clear();
            winTitle2Url.Clear();
            activeTotal = TimeSpan.FromSeconds(0);
        }

        //filters url and process names, returns true if entry is good for insert 
        public static bool Filter(Event e)
        {
            if (e.process.Equals("chrome"))
            {
                if (e.url.Equals("/") ||
                    e.url.Equals("")
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
            else if (e.process.Equals("idlezz") ||
                     e.process.Equals("ShellExperienceHost") ||
                    (e.winTitle.Equals("File Explorer") && (e.winTitle.Equals("explorer"))) ||
                     e.winTitle.Equals("")
                )
            {
                return false;
            }
            else if (e.process.Equals("ignore"))
            {
	            return false;
            }

            return true;
        }//end filter

        public override string ToString()
        {
            // TODO: custom output to file here, this is too bad so just make it done quick
            // the stakeholder does not care about code quality anyway so why should we care
	        return base.ToString();
        }
    }
}