using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScreenBroadcaster.Common;

namespace ScreenBroadcaster.Server.Hubs
{
    internal class HubData
    {
        // Singleton.
        public static HubData Instance { get; private set; }

        static HubData()
        {
            Instance = new HubData();
        }


        // Instance members.
        public List<User>       Users { get; set; }
        public Dictionary<
            Guid, List<Guid>>   BcastRecDictionary { get; set; }

        private HubData()
        {
            Users               = new List<User>();
            BcastRecDictionary  = new Dictionary<Guid, List<Guid>>();
        }
    }
}
