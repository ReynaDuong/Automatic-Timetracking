namespace TimeTracker.View
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

	        return e.GetHashCode() == obj.GetHashCode();
        }
        
    }
}
