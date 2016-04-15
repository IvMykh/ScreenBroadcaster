using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Common;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Client.Controllers
{
    public partial class ClientController
    {
        public partial class ServerToClientCommandsExecutor
        {
            private IDictionary<
                ServerToClientCommand,
                Action<JObject>> _handlers;

            private ClientController _clientController;

            public ServerToClientCommandsExecutor(ClientController clientController)
            {
                _handlers = setupHandlers();
                _clientController = clientController;
            }

            private IDictionary<
                ServerToClientCommand, Action<JObject>> setupHandlers()
            {
                var handlers = new Dictionary<ServerToClientCommand, Action<JObject>>();

                handlers[ServerToClientCommand.ReportSuccessfulRegistration]    = reportSuccessfulRegistration;
                handlers[ServerToClientCommand.ReportFailedRegistration]        = reportFailedRegistration;

                return handlers;
            }

            private void reportSuccessfulRegistration(JObject serverParam)
            {
                var text = (string)serverParam.SelectToken("message");
                var caption = (string)serverParam.SelectToken("caption");

                MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.None);

                _clientController.IsRegistered = true;
            }

            private void reportFailedRegistration(JObject serverParam)
            {
                var text = (string)serverParam.SelectToken("message");
                var caption = (string)serverParam.SelectToken("caption");

                MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Error);

                _clientController.IsRegistered = false;
            }

            public void ExecuteCommand(ServerToClientCommand command, JObject serverParam)
            {
                _handlers[command](serverParam);
            }
        }
    }
}
