using System;
using System.Windows;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.Properties;
using ScreenBroadcaster.Common;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Client.Controllers
{
    public partial class ClientController
    {
        public partial class GeneralCommandsExecutor
            : AbstrCommandsExecutor<ServerToClientGeneralCommand>
        {
            private PictureSender   _pictureSender;

            public GeneralCommandsExecutor(ClientController clientController)
                : base(clientController)
            {
                setupHandlers();

                _pictureSender = new PictureSender(160, clientController);
            }

            public void StopBroadcast()
            {
                _pictureSender.Stop();
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
                var msg = (string)serverParam.SelectToken("message");
                var caption = (string)serverParam.SelectToken("caption");
                var userType = (string)serverParam.SelectToken("userType");

                MsgReporter.Instance.ReportInfo(msg, caption);

                ClientController.IsRegistered = true;
            }
            private void reportFailedRegistration(JObject serverParam)
            {
                var msg = (string)serverParam.SelectToken("message");
                var caption = (string)serverParam.SelectToken("caption");

                MsgReporter.Instance.ReportError(msg, caption);

                ClientController.IsRegistered = false;
            }
            
            private void notifyReceiverStateChange(JObject serverParam)
            {
                var recName = (string)serverParam.SelectToken("receiverName");
                var recId   = (Guid)serverParam.SelectToken("receiverID");
                var state   = (string)serverParam.SelectToken("state");

                ClientController.MainWindow.Dispatcher.Invoke(() =>
                    {
                        ClientController.MainWindow.LogRichTextBox.AppendText(
                            string.Format(Resources.ReceiverStateChangedMsgFormat + '\r', recName, recId, state));
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

                if (isSuccess)
                {
                    MsgReporter.Instance.ReportInfo(
                        Resources.ReceivingStopOkMsg, Resources.StopReceivingCaption);
                }
                else
                {
                    MsgReporter.Instance.ReportWarning(
                        Resources.BcasterDoesNotExistMsg, Resources.StopReceivingCaption);
                }
            }
            private void notifyStopBroadcasting(JObject serverParam)
            {
                var isSuccess = (bool)serverParam.SelectToken("isSuccess");

                if (isSuccess)
                {
                    MsgReporter.Instance.ReportInfo(
                        Resources.BcastingStopOkMsg, Resources.StopBcastingCaption);
                }
                else
                {
                    MsgReporter.Instance.ReportError(
                        Resources.BcastingStopFailedMsg, Resources.StopBcastingCaption);
                }
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
                            String.Format(Resources.ChatMsgFormat, name, text + '\r'));
                    }
                );
            }
        }
    }
} 