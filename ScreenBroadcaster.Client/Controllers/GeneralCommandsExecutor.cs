using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.ScreenCapturing;
using ScreenBroadcaster.Common;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Client.Controllers
{
    public partial class ClientController
    {
        public partial class GeneralCommandsExecutor
            : CommandsExecutor<ServerToClientGeneralCommand>
        {
            private PictureSender   _pictureSender;
            

            public GeneralCommandsExecutor(ClientController clientController)
                : base(clientController)
            {
                setupHandlers();

                _pictureSender = new PictureSender(100, clientController);
            }

            protected override void setupHandlers()
            {
                Handlers[ServerToClientGeneralCommand.ReportSuccessfulRegistration]     = reportSuccessfulRegistration;
                Handlers[ServerToClientGeneralCommand.ReportFailedRegistration]         = reportFailedRegistration;
                Handlers[ServerToClientGeneralCommand.NotifyReceiverStateChange]        = notifyReceiverStateChange;
                Handlers[ServerToClientGeneralCommand.NotifyStopReceiving]              = notifyStopReceiving;
                Handlers[ServerToClientGeneralCommand.NotifyStopBroadcasting]           = notifyStopBroadcasting;
                Handlers[ServerToClientGeneralCommand.ForceStopReceiving]               = forceStopReceiving;
                Handlers[ServerToClientGeneralCommand.ReceiveMessage]                   = sendMessage;
                // тут додати обробника відповідної команди від сервера до клієнта.
            }


            
            private void reportSuccessfulRegistration(JObject serverParam)
            {
                var text = (string)serverParam.SelectToken("message");
                var caption = (string)serverParam.SelectToken("caption");
                var userType = (string)serverParam.SelectToken("userType");

                MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.None);

                ClientController.IsRegistered = true;

                if (String.CompareOrdinal(userType, "Receiver") == 0)
                {
                    // Request for first picture fragment.
                    var clientParam = new JObject();
                    clientParam["broadcasterID"] = ClientController.BroadcasterID;
                    clientParam["receiverID"] = ClientController.User.ID;
                }
            }
            private void reportFailedRegistration(JObject serverParam)
            {
                var text = (string)serverParam.SelectToken("message");
                var caption = (string)serverParam.SelectToken("caption");

                MessageBox.Show(text, caption, MessageBoxButton.OK, MessageBoxImage.Error);

                ClientController.IsRegistered = false;
            }
            private void notifyReceiverStateChange(JObject serverParam)
            {
                var recName = (string)serverParam.SelectToken("receiverName");
                var recId = (Guid)serverParam.SelectToken("receiverID");
                var state = (string)serverParam.SelectToken("state");

                ClientController.MainWindow.Dispatcher.Invoke(() =>
                    {
                        ClientController.MainWindow.LogRichTextBox.AppendText(
                            string.Format("User {0} ({1}) {2} your broadcast.\n", recName, recId, state));
                    });

                var specialState = (BroadcastSpecialState)Enum.Parse(
                    typeof(BroadcastSpecialState), (string)serverParam["specialState"]);

                switch (specialState)
                {
                    case BroadcastSpecialState.FirstReceiverJoined:
                        {
                            // TODO: start passing pictures.
                            _pictureSender.Start();
                            //_pictureSender.sendNextPicture();
                        } break;
                    case BroadcastSpecialState.LastReceiverLeft:
                        { 
                            // TODO: stop passing pictures.
                            _pictureSender.Stop();
                        } break;
                    case BroadcastSpecialState.None:
                    default:
                        {
                            // Do nothing.
                        } break;
                }
            }
            private void notifyStopReceiving(JObject serverParam)
            {
                var isSuccess = (bool)serverParam.SelectToken("isSuccess");
                var text = "";
                var caption = "Stop Receiving";

                if (isSuccess)
                {
                    text = "Receiving has been successfully stopped.";
                }
                else
                {
                    text = "Specified broadcaster does not exist anymore.";
                }

                MessageBox.Show(text, caption);
            }
            private void notifyStopBroadcasting(JObject serverParam)
            {
                var isSuccess = (bool)serverParam.SelectToken("isSuccess");
                var text = "";
                var caption = "Stop Broadcasting";

                if (isSuccess)
                {
                    text = "Broadcasting has been successfully stopped.";
                }
                else
                {
                    text = "Error: broadcasting stop failed.";
                }

                MessageBox.Show(text, caption);
            }
            private void forceStopReceiving(JObject serverParam)
            {
                ClientController.StopReceivingButton_Click(null, null);
            }
            private void sendMessage(JObject serverParam)
            {
                var text = (string)serverParam.SelectToken("Message");
                var name = (string)serverParam.SelectToken("Name");
                ClientController.MainWindow.Dispatcher.Invoke(() =>
                {
                    ClientController.MainWindow.ChatRichTextBox.AppendText(
                        String.Format("{0}: {1}\r",name, text));         
                });
            }
        }
    }
} 