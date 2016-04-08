using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace ScreenBroadcaster.Server
{
    public class MyHub
        : Hub
    {
        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
        }
        public void Send(string message)
        {
            Clients.Caller.addMessage("[Me]", message);
        }

        public void SendPeriodic(string message)
        {
            // Clients.Others.
        }





        public override Task OnConnected()
        {
            Program.MainServerWindow.WriteToConsole(
                string.Format("Client connected: {0}", Context.ConnectionId));
            
            return base.OnConnected();
        }
        public override Task OnDisconnected()
        {
            Program.MainServerWindow.WriteToConsole(
                string.Format("Client disconnected: {0}", Context.ConnectionId));

            return base.OnDisconnected();
        }
    }
}
