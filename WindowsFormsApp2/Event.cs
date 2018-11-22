using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public class Event
    {
        public string winTitle { get; set; }
        public string process { get; set; }
        public string url { get; set; }


        public override int GetHashCode()           //overrides implicit 'GetHashCode' function so Event objects with same process/winTitle/url will be count as equal
        {
            //return process.GetHashCode() + url.GetHashCode() + winTitle.GetHashCode();
            return process.GetHashCode() + url.GetHashCode();
        }

        public override bool Equals(object obj)     //same, but only runs when there are collisions
        {
            Event e = (Event)obj;

            if (e.GetHashCode() == obj.GetHashCode())
                return true;
            else
                return false;
        }
        
    }
}
