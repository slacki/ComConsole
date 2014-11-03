using System;

namespace ComConsole
{
    public class StatusChangedEventArgs : EventArgs
    {
        public string status 
        { 
            get; 
            private set; 
        }

        public StatusChangedEventArgs(string status)
        {
            this.status = status;
        }
    }
}
