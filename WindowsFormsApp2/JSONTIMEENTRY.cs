
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsFormsApp2
{

    public class JSONTIMEENTRY
    {
        public string start { get; set; }
        public string billable { get; set; }
        public string description { get; set; }
        public string projectId { get; set; }
        public string taskId { get; set; }
        public string end { get; set; }
        public string[] tagIds { get; set; }
    }

}