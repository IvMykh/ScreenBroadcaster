using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.ScreenCapturing;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Client.Controllers
{
    public partial class ClientController
    {
        public partial class PictureSender
            : IDisposable
        {
            // Instance members.
            private int             _counter;
            private Timer           _timer;

            public ClientController ClientController    { get; private set; }
            public ScreenCapturer   ScreenCapturer      { get; private set; }
            public int              CurrFragment        { get; private set; }
            
            public int              GenerationFrequency { get; private set; }
            public bool             IsDisposed          { get; private set; }

            public PictureSender(int generationFrequency, ClientController controller)
            {
                _timer              = null;
                _counter            = 0;

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

            public bool sendNextPicture()
            {
                if (CurrFragment == 0)
                {
                    ScreenCapturer.CaptureScreen();
                }

                var clientParam = new JObject();
                clientParam["broadcaserID"] = ClientController.User.ID;
                clientParam["nextPicFrag"] = ScreenCapturer.ScreenshotAsBase64Strings[CurrFragment];
                clientParam["fragNumber"] = CurrFragment;

                bool isLast = (CurrFragment == ScreenCapturer.ScreenshotAsBase64Strings.Length - 1) ? true : false;

                if (isLast)
                    CurrFragment = 0;
                else
                    ++CurrFragment;

                clientParam["isLast"] = isLast;

                ClientController.PicturesHubProxy
                    .Invoke("ExecuteCommand", ClientToServerPictureCommand.SendNewPicture, clientParam);

                return true;
            }

            public void Dispose()
            {
                if (!IsDisposed)
                {
                    Stop();
                }
            }
        }
    }
}
