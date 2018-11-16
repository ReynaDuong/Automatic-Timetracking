using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2
{
    public class EventValues
    {
        public string entryId { get; set; }
        public string taskId { get; set; }
        public string taskName { get; set; }
        public TimeSpan ts { get; set; }

        //public EventValues(string id, TimeSpan ts)
        //{
        //    this.entryId = id;
        //    this.ts = ts;
        //}
    }
}
