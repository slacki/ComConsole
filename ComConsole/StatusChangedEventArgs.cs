using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComConsole
{
    class StatusChangedEventArgs : EventArgs
    {
        public string status 
        { 
            get; 
            private set; 
        }

        StatusChangedEventArgs(string status)
        {
            this.status = status;
        }
    }
}
