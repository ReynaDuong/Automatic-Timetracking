using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApp2
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

        public static Dictionary<Event, EventValues> dictionaryEvents = new Dictionary<Event, EventValues>();
        public static Dictionary<string, Dto.TaskDto> associations = new Dictionary<string, Dto.TaskDto>();

        public static Dictionary<string, string> taskIdName = new Dictionary<string, string>();
    }
}
