using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ScreenBroadcaster.Common;

namespace ScreenBroadcaster.Server.Hubs
{
    // визначає дані про транслятора і його слухачів для хабів.
    internal class HubData
    {
        // Singleton.
        public static HubData Instance { get; private set; }

        static HubData()
        {
            Instance = new HubData();
        }


        // Instance members.

        // всі юзери, які підключені до сервера.
        public List<User>       Users { get; set; }

        // словник, у якому GUID'у транслятора відповідає список GUID'ів його глядачів.
        public Dictionary<
            Guid, List<Guid>>   BcastRecDictionary { get; set; }

        private HubData()
        {
            Users               = new List<User>();
            BcastRecDictionary  = new Dictionary<Guid, List<Guid>>();
        }
    }
}
