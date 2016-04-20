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

                // for pictures.
                //Handlers[ServerToClientGeneralCommand.MakePictureFragment]              = makePictureFragment;
                //Handlers[ServerToClientGeneralCommand.ReceivePictureFragment]           = receivePictureFragment;
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

                    //ClientController.CommandsHubProxy
                    //    .Invoke("ExecuteCommand", ClientToServerGeneralCommand.GiveNextPictureFragment, clientParam);
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

            //private async void makePictureFragment(JObject serverParam)
            //{
            //    if (CurrFragment == 0)
            //    {
            //        ScreenCapturer.CaptureScreen();
            //    }

            //    var clientParam = new JObject();
            //    clientParam["broadcaserID"] = serverParam.SelectToken("broadcaserID");
            //    clientParam["receiverID"]   = serverParam.SelectToken("receiverID");
            //    clientParam["nextPicFrag"]  = ScreenCapturer.ScreenshotAsBase64Strings[CurrFragment];

            //    bool isLast = (CurrFragment == ScreenCapturer.ScreenshotAsBase64Strings.Length - 1) ? true : false;

            //    if (isLast)
            //        CurrFragment = 0;
            //    else
            //        ++CurrFragment;

            //    clientParam["isLast"]       = isLast;

            //    await ClientController.CommandsHubProxy
            //        .Invoke("ExecuteCommand", ClientToServerGeneralCommand.TakeNextPictureFragment, clientParam);
            //}
            //private async void receivePictureFragment(JObject serverParam)
            //{
            //    var nextPicFrag = (string)serverParam.SelectToken("nextPicFrag");
            //    var isLast      = (bool)serverParam.SelectToken("isLast");

            //    PicFrags.Add(nextPicFrag);

            //    if (isLast)
            //    {
            //        var strBuilder = new StringBuilder();

            //        foreach (var frag in PicFrags)
            //        {
            //            strBuilder.Append(frag);
            //        }

            //        byte[] pictureData = Convert.FromBase64String(strBuilder.ToString());

            //        using (var memoryStream = new MemoryStream(pictureData))
            //        {
            //             memoryStream.Position = 0;

            //            ClientController.MainWindow.Dispatcher.Invoke(() =>
            //            {
            //                BitmapImage bitmapImage = new BitmapImage();
            //                bitmapImage.BeginInit();
            //                bitmapImage.StreamSource = memoryStream;
            //                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            //                bitmapImage.EndInit();
                            
            //                var brush = new ImageBrush(bitmapImage);
            //                ClientController.MainWindow.RemoteScreenDisplay.Background = brush;
            //            });
            //        }

            //        PicFrags.Clear();
            //    }

            //    var clientParam = new JObject();
            //    clientParam["broadcasterID"] = ClientController.BroadcasterID;
            //    clientParam["receiverID"] = ClientController.User.ID;

            //    await ClientController.CommandsHubProxy
            //        .Invoke("ExecuteCommand", ClientToServerGeneralCommand.GiveNextPictureFragment, clientParam);
            //}
        }
    }
}
