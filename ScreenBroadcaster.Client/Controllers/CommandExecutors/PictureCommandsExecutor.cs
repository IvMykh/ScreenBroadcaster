using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.Controllers.Helpers;
using ScreenBroadcaster.Common.CommandTypes;
using System.Drawing;

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
            var pieceStartX = (int)serverParam.SelectToken("pieceStartX");
            var pieceStartY = (int)serverParam.SelectToken("pieceStartY");
            var pieceWidth = (int)serverParam.SelectToken("pieceWidth");
            var pieceHeight = (int)serverParam.SelectToken("pieceHeight");

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
                        
                        //bitmapImage.BeginInit();
                        //bitmapImage.StreamSource = memoryStream;
                        //bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        //bitmapImage.EndInit();

                        if (ClientController.GetFullImage == true)
                        {
                            ClientController.FullImage = new Bitmap(memoryStream);
                        }
                        else
                        {
                            using (Graphics g = Graphics.FromImage(ClientController.FullImage))
                            {
                                Bitmap piece = new Bitmap(memoryStream);
                                g.DrawImage(piece, pieceStartX, pieceStartY);
                            }
                        }

                        System.Windows.Media.Imaging.BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                ClientController.FullImage.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty,
                                BitmapSizeOptions.FromWidthAndHeight(ClientController.FullImage.Width, ClientController.FullImage.Height));

                        var brush = new ImageBrush(bs);
                        ClientController.MainWindow.RemoteScreenDisplay.Background = brush;
                    }
                });

                PicFrags.Clear();
            }
        }
    }
}
