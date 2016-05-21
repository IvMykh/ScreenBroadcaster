using System;
using System.Configuration;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.Properties;
using ScreenBroadcaster.Common;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Client.Controllers
{
    public partial class ClientController
    {
        internal partial class GeneralCommandsExecutor
            : AbstrCommandsExecutor<ServerToClientGeneralCommand>
        {
            private PictureSender   _pictureSender;

            public GeneralCommandsExecutor(ClientController clientController)
                : base(clientController)
            {
                setupHandlers();

                int initGenFreq = (int)clientController.MainWindow.GenerationFreqNud.Value;

                /* Old version.
                //int.Parse(ConfigurationManager.AppSettings["PictureGenerationFrequency"]);
                */

                 var picGenFreq = int.Parse(Resources.MilisecondsInSecond) / initGenFreq;
                _pictureSender = new PictureSender(picGenFreq, clientController);
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
                Handlers[ServerToClientGeneralCommand.ChangeGenerationFrequency]        = changeGenerationFrequency;
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
                            string.Format(Resources.ReceiverStateChangedMsgFormat + '\r', recName, state));
                    });

                var specialState = (BroadcastSpecialState)Enum.Parse(
                    typeof(BroadcastSpecialState), (string)serverParam["specialState"]);

                switch (specialState)
                {
                    case BroadcastSpecialState.FirstReceiverJoined:
                        {
                            _pictureSender.Start();
                        } break;
                    case BroadcastSpecialState.LastReceiverLeft:
                        { 
                            _pictureSender.Stop();
                        } break;
                    case BroadcastSpecialState.None:
                    default: 
                        { 
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

            private void changeGenerationFrequency(JObject serverParam)
            {
                int newFreq = (int)serverParam.SelectToken("newFreq");
                _pictureSender.GenerationFrequency = int.Parse(Resources.MilisecondsInSecond) / newFreq;
            }
        }
    }
} 