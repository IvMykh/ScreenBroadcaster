using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.Controllers.Helpers;
using ScreenBroadcaster.Client.Properties;
using ScreenBroadcaster.Common;
using ScreenBroadcaster.Common.CommandTypes;
using System.Drawing;

namespace ScreenBroadcaster.Client.Controllers
{
    public partial class ClientController
    {
        internal partial class PictureSender
            : IDisposable
        {
            // Instance members.
            private Timer           _timer;

            public ClientController ClientController    { get; private set; }
            public ScreenCapturer   ScreenCapturer      { get; private set; }
            public int              CurrFragment        { get; private set; }
            public int              GenerationFrequency { get; private set; }
            public bool             IsDisposed          { get; private set; }

            public PictureSender(int generationFrequency, ClientController controller)
            {
                _timer              = null;
                
                ClientController    = controller;
                ScreenCapturer      = new ScreenCapturer();
                CurrFragment        = 0;

                GenerationFrequency = generationFrequency;
                IsDisposed          = true;
            }

            public void Start()
            {
                if (_timer == null)
                {
                    _timer = new Timer(new TimerCallback((obj) =>
                        {
                            sendNextPicture();
                        }), null, 0, GenerationFrequency);
                    IsDisposed = false;
                }
            }
            public void Stop()
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                    IsDisposed = true;
                }
            }
            public void Dispose()
            {
                if (!IsDisposed)
                {
                    Stop();
                }
                IsDisposed = true;
            }

            private void sendNextPicture()
            { 
                if (CurrFragment == 0)
                {
                    if (ClientController.GetFullImage == true)
                    {
                        ScreenCapturer.CaptureScreen();
                        //ClientController.FullImage = ScreenCapturer.Screenshot;
                        ClientController.GetFullImage = false;
                    }
                    else
                    {
                        ScreenCapturer.CapturePart(ClientController.FullImage);
                    }
                }

                var clientParam = new JObject();
                clientParam["broadcaserID"] = ClientController.User.ID;
                clientParam["nextPicFrag"] = ScreenCapturer.ScreenshotAsBase64Strings[CurrFragment];
                clientParam["fragNumber"] = CurrFragment;
                clientParam["pieceStartX"] = ScreenCapturer.PieceStartX;
                clientParam["pieceStartY"] = ScreenCapturer.PieceStartY;
                clientParam["pieceWidth"] = ScreenCapturer.PieceWidth;
                clientParam["pieceHeight"] = ScreenCapturer.PieceHeight;

                bool isLast = (CurrFragment == ScreenCapturer.ScreenshotAsBase64Strings.Length - 1) ? true : false;

                if (isLast)
                    CurrFragment = 0;
                else
                    ++CurrFragment;

                clientParam["isLast"] = isLast;

                try
                {
                    ClientController.PicturesHubProxy
                        .Invoke("ExecuteCommand", ClientToServerPictureCommand.SendNewPicture, clientParam);
                }
                catch (InvalidOperationException ioe)
                {
                    MsgReporter.Instance.ReportError(
                        ioe.Message, Resources.CommandExecErrorCaption);
                }
            }
        }
    }
}
