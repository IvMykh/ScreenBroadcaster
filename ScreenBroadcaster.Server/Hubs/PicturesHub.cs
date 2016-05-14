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
        : AbstrHub<ClientToServerPictureCommand>
    {
        public PicturesHub()
            : base()
        {
        }

        protected override IDictionary<
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
            bool keyExists = Data.BcastRecDictionary.TryGetValue(bcasterId, out receiverIDs);

            if (keyExists)
            {
                var receiversIDsOnHub = (from recID in receiverIDs
                                         join user in Data.Users on recID equals user.ID
                                         select user.ClientIdOnHub)
                                            .ToList<string>();

                //var serverParam = new JObject();
                //serverParam["nextPicFrag"] = clientParam.SelectToken("broadcaserID");
                //serverParam["isLast"] = clientParam.SelectToken("isLast");

                Clients.Clients(receiversIDsOnHub).ExecuteCommand(
                    ServerToClientPictureCommand.ReceiveNewPicture, clientParam);
            }
        }
    }
}
