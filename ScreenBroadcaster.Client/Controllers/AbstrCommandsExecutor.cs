using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Client.Controllers
{
    public abstract class AbstrCommandsExecutor<CommandType>
        where CommandType : struct, IConvertible
    {
        protected IDictionary<
                CommandType, Action<JObject>> Handlers { get; private set; }

        protected ClientController ClientController { get; private set; }

        public AbstrCommandsExecutor(ClientController clientController)
        {
            ClientController = clientController;
            Handlers = new Dictionary<CommandType, Action<JObject>>();
        }

        protected abstract void setupHandlers();

        public void ExecuteCommand(CommandType command, JObject serverParam)
        {
            Handlers[command](serverParam);
        }
    }
}
