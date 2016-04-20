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

namespace ScreenBroadcaster.Server.Hubs
{
    public class CommandsHub
        : Hub
    {
        // Static members.
        private static HubData _data;

        static CommandsHub()
        {
            _data = HubData.Instance;
        }

        // Instance members.
        private IDictionary<
            ClientToServerGeneralCommand, Action<JObject>> _handlers;

        public CommandsHub()
        {
            _handlers           = setupHandlers();
        }

        private IDictionary<
            ClientToServerGeneralCommand, Action<JObject>> setupHandlers()
        {
            var handlers = new Dictionary<ClientToServerGeneralCommand, Action<JObject>>();

            handlers[ClientToServerGeneralCommand.RegisterNewBroadcaster]   = registerNewBroadcaster;
            handlers[ClientToServerGeneralCommand.RegisterNewReceiver]      = registerNewReceiver;
            handlers[ClientToServerGeneralCommand.StopReceiving]            = stopReceiving;
            handlers[ClientToServerGeneralCommand.StopBroadcasting]         = stopBroadcasting;

            // for pictures.
            //handlers[ClientToServerGeneralCommand.TakeNextPictureFragment]  = takeNextPictureFragment;
            //handlers[ClientToServerGeneralCommand.GiveNextPictureFragment]  = giveNextPictureFragment;

            return handlers;
        }


        private async void registerNewBroadcaster(JObject clientParam)
        {
            var user = new User
            {
                ID = (Guid)clientParam.SelectToken("ID"),
                Name = (string)clientParam.SelectToken("Name"),
                ClientIdOnHub = Context.ConnectionId
            };

            _data.Users.Add(user);
            
            _data.BcastRecDictionary[user.ID] = new List<Guid>();

            var serverParam = new JObject();
            serverParam["message"] = "You have been successfully registered as a Broadcaster.";
            serverParam["caption"] = "Registration succeeded!";
            serverParam["userType"] = "Broadcaster";

            await Clients.Caller.ExecuteCommand(ServerToClientGeneralCommand.ReportSuccessfulRegistration, serverParam);
        }      
        private async void registerNewReceiver(JObject clientParam)
        {
            var newReceiver = new User
            {
                ID = (Guid)clientParam.SelectToken("ID"),
                Name = (string)clientParam.SelectToken("Name"),
                ClientIdOnHub = Context.ConnectionId
            };

            var bcasterId = (Guid)clientParam.SelectToken("BroadcasterID");
            List<Guid> receivers = null;
            bool bcasterExists =_data.BcastRecDictionary.TryGetValue(bcasterId, out receivers);

            var serverParamForCaller = new JObject();

            if (!bcasterExists)
            {
                serverParamForCaller["message"] = "Registration failed: specified Broadcaster does not exist.";
                serverParamForCaller["caption"] = "Registration failed!";

                await Clients.Caller.ExecuteCommand(
                    ServerToClientGeneralCommand.ReportFailedRegistration, serverParamForCaller);
                
                return;
            }

            User bcaster = _data.Users.Find(user => user.ID.CompareTo(bcasterId) == 0);

            _data.Users.Add(newReceiver);
            _data.BcastRecDictionary[bcasterId].Add(newReceiver.ID);

            serverParamForCaller["message"] = "You have been successfully registered as a Receiver.";
            serverParamForCaller["caption"] = "Registration succeeded!";
            serverParamForCaller["userType"] = "Receiver";

            Clients.Caller.ExecuteCommand(
                ServerToClientGeneralCommand.ReportSuccessfulRegistration, serverParamForCaller);
           
            var serverParamForBcaster = new JObject();
            serverParamForBcaster["receiverName"] = newReceiver.Name;
            serverParamForBcaster["receiverID"] = newReceiver.ID;
            serverParamForBcaster["state"] = "joined";

            var bcastState = BroadcastSpecialState.None;
            if (receivers.Count == 1)
            {
                bcastState = BroadcastSpecialState.FirstReceiverJoined;
            }

            serverParamForBcaster["specialState"] = bcastState.ToString();

            Clients.Client(bcaster.ClientIdOnHub).ExecuteCommand(
                ServerToClientGeneralCommand.NotifyReceiverStateChange, serverParamForBcaster);
        }
        private async void stopReceiving(JObject clientParam)
        {
            var callerId = (Guid)clientParam.SelectToken("ID");
            var bcasterId = (Guid)clientParam.SelectToken("BroadcasterID");

            List<Guid> receivers = null;
            bool keyExists = _data.BcastRecDictionary.TryGetValue(bcasterId, out receivers);

            User caller = _data.Users.Find(user => user.ID.Equals(callerId));

            if (keyExists)
            {
                receivers.Remove(callerId);

                User bcaster = _data.Users.Find(user => user.ID.Equals(bcasterId));

                var serverParamForBcaster = new JObject();
                serverParamForBcaster["receiverName"] = caller.Name;
                serverParamForBcaster["receiverID"] = caller.ID;
                serverParamForBcaster["state"] = "left";

                var bcastState = BroadcastSpecialState.None;
                if (receivers.Count == 0)
                {
                    bcastState = BroadcastSpecialState.LastReceiverLeft;
                }

                serverParamForBcaster["specialState"] = bcastState.ToString();

                await Clients.Client(bcaster.ClientIdOnHub).ExecuteCommand(
                        ServerToClientGeneralCommand.NotifyReceiverStateChange, serverParamForBcaster);
            }
            
            _data.Users.Remove(caller);
            
            var serverParamForCaller = new JObject();
            serverParamForCaller["isSuccess"] = true;

            await Clients.Caller.ExecuteCommand(
                ServerToClientGeneralCommand.NotifyStopReceiving, serverParamForCaller);

            
        }
        private async void stopBroadcasting(JObject clientParam)
        {
            var bcasterId = (Guid)clientParam.SelectToken("BroadcasterID");
            var receiverIDs = _data.BcastRecDictionary[bcasterId];

            var receiversIDsOnHub = (from recID in receiverIDs 
                                     join user in _data.Users on recID equals user.ID
                                     select user.ClientIdOnHub)
                                        .ToList<string>();

            await Clients.Clients(receiversIDsOnHub).ExecuteCommand(
                ServerToClientGeneralCommand.ForceStopReceiving, new JObject());

            _data.BcastRecDictionary.Remove(bcasterId);

            var serverParam = new JObject();
            serverParam["isSuccess"] = true;
            
            await Clients.Caller.ExecuteCommand(
                ServerToClientGeneralCommand.NotifyStopBroadcasting, serverParam);
            
        }

        //private async void giveNextPictureFragment(JObject clientParam)
        //{
        //    var bcasterID = (Guid)clientParam.SelectToken("broadcasterID");
        //    var bcaster = _data.Users.Find(user => user.ID.Equals(bcasterID));
        //
        //    await Clients.Client(bcaster.ClientIdOnHub).ExecuteCommand(
        //        ServerToClientGeneralCommand.MakePictureFragment, clientParam);
        //}
        //private async void takeNextPictureFragment(JObject clientParam)
        //{
        //    var serverParam = new JObject();
        //    serverParam["nextPicFrag"] = clientParam.SelectToken("nextPicFrag");
        //    serverParam["isLast"] = clientParam.SelectToken("isLast");
        //
        //    var receiverID = (Guid)clientParam.SelectToken("receiverID");
        //    User receiver = _data.Users.Find(user => user.ID.Equals(receiverID));
        //
        //    await Clients.Client(receiver.ClientIdOnHub).ExecuteCommand(
        //        ServerToClientGeneralCommand.ReceivePictureFragment, serverParam);
        //}

        public void ExecuteCommand(ClientToServerGeneralCommand command, JObject argument)
        {
            _handlers[command](argument);
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
