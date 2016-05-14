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

        // GUID, який він отримав при запуску клієнта.
        public Guid     ID              { get; set; }
        
        // id для спілкування через SignalR.
        public string   ClientIdOnHub   { get; set; }
    }
}
