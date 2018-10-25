using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp2
{
    public class EntryIdTime
    {
        public string id { get; set; }
        public TimeSpan ts { get; set; }

        public EntryIdTime(string id, TimeSpan ts)
        {
            this.id = id;
            this.ts = ts;
        }
    }
}
