using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

using Microsoft.AspNet.SignalR;
using Newtonsoft.Json.Linq;

using ScreenBroadcaster.Common;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Server
{
    public class CommandsHub
        : Hub
    {
        private class CommandsHubData
        {
            public List<User>       Users { get; set; }
            public Dictionary<
                Guid, List<Guid>>   BcastRecDictionary { get; set; }

            public CommandsHubData()
            {
                Users               = new List<User>();
                BcastRecDictionary  = new Dictionary<Guid, List<Guid>>();
            }
        }

        private static CommandsHubData _data;

        static CommandsHub()
        {
            _data = new CommandsHubData();
        }

        private IDictionary<
            ClientToServerCommand, Action<JObject>> _handlers;

        public CommandsHub()
        {
            _handlers           = setupHandlers();
        }

        private IDictionary<
            ClientToServerCommand, Action<JObject>> setupHandlers()
        {
            var handlers = new Dictionary<ClientToServerCommand, Action<JObject>>();

            handlers[ClientToServerCommand.RegisterNewBroadcaster] =
                new Action<JObject>(param =>
                    {
                        var user = new User
                            {
                                ID = (Guid)param.SelectToken("ID"),
                                Name = (string)param.SelectToken("Name")
                            };

                        _data.Users.Add(user);
                        _data.BcastRecDictionary[user.ID] = new List<Guid>();

                        var serverParam = new JObject();
                        serverParam["message"] = "You have been successfully registered as a Broadcaster.";
                        serverParam["caption"] = "Registration succeeded!";

                        Clients.Caller.ExecuteCommand(ServerToClientCommand.ReportSuccessfulRegistration, serverParam);
                    });

            handlers[ClientToServerCommand.RegisterNewReceiver] =
                new Action<JObject>(clientParam =>
                    {
                        var user = new User
                            {
                                ID = (Guid)clientParam.SelectToken("ID"),
                                Name = (string)clientParam.SelectToken("Name")
                            };

                        var bcasterId = (Guid)clientParam.SelectToken("BroadcasterID");

                        var serverParam = new JObject();
                        if (!_data.BcastRecDictionary.Keys.Any(key => key.CompareTo(bcasterId) == 0))
                        {
                            serverParam["message"] = "Registration failed: specified Broadcaster does not exist.";
                            serverParam["caption"] = "Registration failed!";

                            Clients.Caller.ExecuteCommand(ServerToClientCommand.ReportFailedRegistration, serverParam);
                            return;
                        }

                        _data.Users.Add(user);
                        _data.BcastRecDictionary[bcasterId].Add(user.ID);

                        serverParam["message"] = "You have been successfully registered as a Receiver.";
                        serverParam["caption"] = "Registration succeeded!";

                        Clients.Caller.ExecuteCommand(ServerToClientCommand.ReportSuccessfulRegistration, serverParam);
                    });

            return handlers;
        }

        public void ExecuteCommand(ClientToServerCommand command, JObject argument)
        {
            _handlers[command](argument);
        }

        // Unrequired;
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
