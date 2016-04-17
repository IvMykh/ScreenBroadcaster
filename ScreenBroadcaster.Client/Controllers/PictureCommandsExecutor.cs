using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Client.Controllers
{
    public class PictureCommandsExecutor
        : CommandsExecutor<ServerToClientPictureCommand>
    {
        public PictureCommandsExecutor(ClientController clientController)
            :base(clientController)
        {
            setupHandlers();
        }

        protected override void setupHandlers()
        {
            //Handlers[] = ;
        }

        // Concrete commands handlers.
    }
}
