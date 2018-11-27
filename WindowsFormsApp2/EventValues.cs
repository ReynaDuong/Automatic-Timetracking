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
        public int listId { get; set; }

        public TimeSpan ts { get; set; }
        public TimeSpan idle { get; set; }
        public TimeSpan active { get; set; }
        public TimeSpan activeDelta { get; set; }
        public TimeSpan lastPostedTs { get; set; }
    }
}
