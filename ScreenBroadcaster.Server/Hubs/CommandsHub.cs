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
using System.Threading;
using ScreenBroadcaster.Server.Properties;

namespace ScreenBroadcaster.Server.Hubs
{
    public class CommandsHub
        : AbstrHub<ClientToServerGeneralCommand>
    {
        public CommandsHub()
            : base()
        {
        }

        protected override IDictionary<
            ClientToServerGeneralCommand, Action<JObject>> setupHandlers()
        {
            var handlers = new Dictionary<ClientToServerGeneralCommand, Action<JObject>>();

            handlers[ClientToServerGeneralCommand.RegisterNewBroadcaster]   = registerNewBroadcaster;
            handlers[ClientToServerGeneralCommand.RegisterNewReceiver]      = registerNewReceiver;
            handlers[ClientToServerGeneralCommand.StopReceiving]            = stopReceiving;
            handlers[ClientToServerGeneralCommand.StopBroadcasting]         = stopBroadcasting;
            handlers[ClientToServerGeneralCommand.SendMessage]              = sendMessage;

            // тут додати обробника для відповідної команди від клієнта.
            
            // ВАЖЛИВО! Щоб відправити, треба за GUID'ом знайти у списку користувача 
            // і взяти його властивість ClientIdOnHub (див. як я робив в інших методах)
            
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

            Data.Users.Add(user);
            
            Data.BcastRecDictionary[user.ID] = new List<Guid>();

            var serverParam = new JObject();
            serverParam["message"] = Resources.BcasterRegistrationOkMsg;
            serverParam["caption"] = Resources.RegistrationOkCaption;
            serverParam["userType"] = "Broadcaster";

//#if DEBUG
            var clipboardOpThread = new Thread(() => 
                {
                    Clipboard.Clear();
                    Clipboard.SetText(user.ID.ToString());
                }
            );

            clipboardOpThread.SetApartmentState(ApartmentState.STA);
            clipboardOpThread.Start();
//#endif

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
            bool bcasterExists = Data.BcastRecDictionary.TryGetValue(bcasterId, out receivers);

            var serverParamForCaller = new JObject();

            if (!bcasterExists)
            {
                serverParamForCaller["message"] = Resources.BcasterDoesNotExistMsg;
                serverParamForCaller["caption"] = Resources.RegistrationFailedCaption;

                await Clients.Caller.ExecuteCommand(
                    ServerToClientGeneralCommand.ReportFailedRegistration, serverParamForCaller);
                
                return;
            }

            User bcaster = Data.Users.Find(user => user.ID.CompareTo(bcasterId) == 0);

            Data.Users.Add(newReceiver);
            Data.BcastRecDictionary[bcasterId].Add(newReceiver.ID);

            serverParamForCaller["message"] = Resources.ReceiverRegistrationOkMsg;
            serverParamForCaller["caption"] = Resources.RegistrationOkCaption;
            serverParamForCaller["userType"] = "Receiver";

            Clients.Caller.ExecuteCommand(
                ServerToClientGeneralCommand.ReportSuccessfulRegistration, serverParamForCaller);
           
            var serverParamForBcaster = new JObject();
            serverParamForBcaster["receiverName"] = newReceiver.Name;
            serverParamForBcaster["receiverID"] = newReceiver.ID;
            serverParamForBcaster["state"] = "joined";

            var bcastState = BroadcastSpecialState.None;
            if (Data.BcastRecDictionary[bcasterId].Count == 1) // receivers.Count == 1
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
            bool keyExists = Data.BcastRecDictionary.TryGetValue(bcasterId, out receivers);

            User caller = Data.Users.Find(user => user.ID.Equals(callerId));

            if (keyExists)
            {
                receivers.Remove(callerId);

                User bcaster = Data.Users.Find(user => user.ID.Equals(bcasterId));

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
            
            Data.Users.Remove(caller);
            
            var serverParamForCaller = new JObject();
            serverParamForCaller["isSuccess"] = true;

            await Clients.Caller.ExecuteCommand(
                ServerToClientGeneralCommand.NotifyStopReceiving, serverParamForCaller);

            
        }
        private async void stopBroadcasting(JObject clientParam)
        {
            var bcasterId = (Guid)clientParam.SelectToken("BroadcasterID");
            var receiverIDs = Data.BcastRecDictionary[bcasterId];

            var receiversIDsOnHub = (from recID in receiverIDs 
                                     join user in Data.Users on recID equals user.ID
                                     select user.ClientIdOnHub)
                                        .ToList<string>();

            await Clients.Clients(receiversIDsOnHub).ExecuteCommand(
                ServerToClientGeneralCommand.ForceStopReceiving, new JObject());

            Data.BcastRecDictionary.Remove(bcasterId);

            var serverParam = new JObject();
            serverParam["isSuccess"] = true;
            
            await Clients.Caller.ExecuteCommand(
                ServerToClientGeneralCommand.NotifyStopBroadcasting, serverParam);
            
        }
        private async void sendMessage(JObject clientParam)
        { 
            var bcasterId = (Guid)clientParam.SelectToken("BroadcasterID");
            var id = (Guid)clientParam.SelectToken("ID");
            

            if (bcasterId == Guid.Empty)
            {
                bcasterId = id;
            }
                 
            var receiverIDs = Data.BcastRecDictionary[bcasterId];
            var receiversIDsOnHub = (from recID in receiverIDs
                                     join user in Data.Users on recID equals user.ID
                                     select user.ClientIdOnHub)
                                     .ToList<string>();
            
            receiversIDsOnHub.Add(Data.Users.Find(u => u.ID.Equals(bcasterId)).ClientIdOnHub);
            
            await Clients.Clients(receiversIDsOnHub).ExecuteCommand(
                ServerToClientGeneralCommand.ReceiveMessage, clientParam);      
        }
    }
}
