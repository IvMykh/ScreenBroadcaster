using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Server.Hubs
{
    public class PicturesHub
        : Hub
    {
        // Static members.
        private static HubData _data;

        static PicturesHub()
        {
            _data = HubData.Instance;
        }

        // Instance members.
        private IDictionary<
            ClientToServerPictureCommand, Action<JObject>> _handlers;

        public PicturesHub()
        {
            _handlers           = setupHandlers();
        }

        private IDictionary<
            ClientToServerPictureCommand, Action<JObject>> setupHandlers()
        {
            var handlers = new Dictionary<ClientToServerPictureCommand, Action<JObject>>();

            handlers[ClientToServerPictureCommand.SendNewPicture] = sendNewPicture;

            return handlers;
        }


        
        private void sendNewPicture(JObject clientParam)
        {
            var bcasterId = (Guid)clientParam.SelectToken("broadcaserID");
            var nextPicFrag = (string)clientParam.SelectToken("nextPicFrag");
            var isLast = (bool)clientParam.SelectToken("isLast");

            List<Guid> receiverIDs = null;
            bool keyExists = _data.BcastRecDictionary.TryGetValue(bcasterId, out receiverIDs);


            if (keyExists)
            {
                var receiversIDsOnHub = (from recID in receiverIDs
                                         join user in _data.Users on recID equals user.ID
                                         select user.ClientIdOnHub)
                                            .ToList<string>();

                //var serverParam = new JObject();
                //serverParam["nextPicFrag"] = clientParam.SelectToken("broadcaserID");
                //serverParam["isLast"] = clientParam.SelectToken("isLast");

                Clients.Clients(receiversIDsOnHub).ExecuteCommand(
                    ServerToClientPictureCommand.ReceiveNewPicture, clientParam);
            }
        }

        public void ExecuteCommand(ClientToServerPictureCommand command, JObject argument)
        {
            _handlers[command](argument);
        }
    }
}
