using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.Controllers.Helpers;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Client.Controllers
{
    internal class PictureCommandsExecutor
        : AbstrCommandsExecutor<ServerToClientPictureCommand>
    {
        public List<string>     PicFrags        { get; private set; }

        public PictureCommandsExecutor(ClientController clientController)
            :base(clientController)
        {
            setupHandlers();
            PicFrags = new List<string>();
        }

        protected override void setupHandlers()
        {
            Handlers[ServerToClientPictureCommand.ReceiveNewPicture] = receiveNewPicture;
        }

        // Concrete commands handlers.
        private void receiveNewPicture(JObject serverParam)
        {
            var nextPicFrag = (string)serverParam.SelectToken("nextPicFrag");
            var isLast = (bool)serverParam.SelectToken("isLast");
            var fragNumber = (int)serverParam.SelectToken("fragNumber");

            if (fragNumber == PicFrags.Count)
            {
                PicFrags.Add(nextPicFrag);
            }

            if (isLast &&  PicFrags.Count > 0)
            {
                var strBuilder = new StringBuilder(PicFrags.Count * ScreenCapturer.CharsInBlock);
                foreach (var frag in PicFrags)
                {
                    strBuilder.Append(frag);
                }
                
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

                PicFrags.Clear();
            }
        }
    }
}
