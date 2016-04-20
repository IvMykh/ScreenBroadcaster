using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.ScreenCapturing;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Client.Controllers
{
    public class PictureCommandsExecutor
        : CommandsExecutor<ServerToClientPictureCommand>
    {
        public List<string>     PicFrags        { get; private set; }

        public PictureCommandsExecutor(ClientController clientController)
            :base(clientController)
        {
            setupHandlers();

            //ScreenCapturer  = new ScreenCapturer();
            //CurrFragment    = 0;
            PicFrags        = new List<string>();
        }

        protected override void setupHandlers()
        {
            Handlers[ServerToClientPictureCommand.ReceiveNewPicture] = receiveNewPicture;
        }

        // Concrete commands handlers.

        private void receiveNewPicture(JObject serverParam)
        {
            // TODO: implement.

            var nextPicFrag = (string)serverParam.SelectToken("nextPicFrag");
            var isLast = (bool)serverParam.SelectToken("isLast");
            var fragNumber = (int)serverParam.SelectToken("fragNumber");

            if (fragNumber == PicFrags.Count)
            {
                PicFrags.Add(nextPicFrag);
            }

            if (isLast &&  PicFrags.Count > 0) //fragNumber
            {
                var strBuilder = new StringBuilder(PicFrags.Count * ScreenCapturer.CHARS_IN_BLOCK);

                foreach (var frag in PicFrags)
                {
                    strBuilder.Append(frag);
                }

                //lock (new object())
                //{
                
                    ClientController.MainWindow.Dispatcher.Invoke(() =>
                    {
                        byte[] pictureData = Convert.FromBase64String(strBuilder.ToString());
                        using (var memoryStream = new MemoryStream(pictureData))
                        {
                            memoryStream.Position = 0;
                            BitmapImage bitmapImage = new BitmapImage();
                            bitmapImage.BeginInit();
                            bitmapImage.StreamSource = memoryStream;
                            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                            bitmapImage.EndInit();

                            var brush = new ImageBrush(bitmapImage);
                            ClientController.MainWindow.RemoteScreenDisplay.Background = brush;
                        }
                    });
                //}

                PicFrags.Clear();
            }

            //var clientParam = new JObject();
            //clientParam["broadcasterID"] = ClientController.BroadcasterID;
            //clientParam["receiverID"] = ClientController.User.ID;

            //await ClientController.CommandsHubProxy
            //    .Invoke("ExecuteCommand", ClientToServerGeneralCommand.GiveNextPictureFragment, clientParam);
        }
    }
}
