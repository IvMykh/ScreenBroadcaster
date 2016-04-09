using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
        //public void Send(string message)
        //{
        //    Clients.Caller.addMessage("[Me]", message);
        //}

        public void SendPeriodic(string message)
        {
            // Clients.Others.
        }



        public override Task OnConnected()
        {
            //Use Application.Current.Dispatcher to access UI thread from outside the MainWindow class
            Application.Current.Dispatcher.Invoke(() =>
                ((ServerMainWindow)Application.Current.MainWindow).ServerController.WriteToConsole("Client connected: " + Context.ConnectionId));

            return base.OnConnected();
        }
        public override Task OnDisconnected()
        {
            //Use Application.Current.Dispatcher to access UI thread from outside the MainWindow class
            Application.Current.Dispatcher.Invoke(() =>
                ((ServerMainWindow)Application.Current.MainWindow).ServerController.WriteToConsole("Client disconnected: " + Context.ConnectionId));

            return base.OnDisconnected();
        }
    }
}
