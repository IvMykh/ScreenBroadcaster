using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScreenBroadcaster.Common
{
    public class User
    {
        public string   Name            { get; set; }
        public Guid     ID              { get; set; }
        public string   ClientIdOnHub   { get; set; }
    }

    public enum UserType
    {
        Broadcaster,
        Receiver
    }
}
