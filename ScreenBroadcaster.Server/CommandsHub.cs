using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

using Microsoft.AspNet.SignalR;

using ScreenBroadcaster.Common;

namespace ScreenBroadcaster.Server
{
    public class CommandsHub
        : Hub
    {
        private IDictionary<User, List<User>>                   _broadcasterReceiversDictionary;
        private IDictionary<
            ClientToServerCommand, Action<User>> _handlers;

        public CommandsHub()
        {
            _broadcasterReceiversDictionary = new Dictionary<User, List<User>>();
            _handlers                       = setupHandlers();

            Debug.WriteLine("CommandsHub initialized.");
        }

        private IDictionary<
            ClientToServerCommand, Action<User>> setupHandlers()
        {
            var handlers = new Dictionary<ClientToServerCommand, Action<User>>();

            handlers[ClientToServerCommand.RegisterNewBroadcaster] =
                new Action<User>((param) =>
                    {
                        _broadcasterReceiversDictionary[param] = new List<User>();
                        Clients.Caller.ExecuteCommand(ServerToClientCommand.ReportSuccessfulBcasterRegistration, null);
                    });

            return handlers;
        }

        public void ExecuteCommand(ClientToServerCommand command, User argument)
        {
            _handlers[command](argument);
        }

        public void Send(string name, string message)
        {
            Clients.All.addMessage(name, message);
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
