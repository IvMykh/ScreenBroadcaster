using System;
using System.Threading;
using Newtonsoft.Json.Linq;
using ScreenBroadcaster.Client.Controllers.Helpers;
using ScreenBroadcaster.Client.Properties;
using ScreenBroadcaster.Common;
using ScreenBroadcaster.Common.CommandTypes;

namespace ScreenBroadcaster.Client.Controllers
{
    public partial class ClientController
    {
        internal partial class PictureSender
            : IDisposable
        {
            // Instance members.
            private object          _thisLock;
            private Timer           _timer;
            private int             _generationFreq;

            public ClientController ClientController    { get; private set; }
            public ScreenCapturer   ScreenCapturer      { get; private set; }
            public int              CurrFragment        { get; private set; }
            public bool             IsDisposed          { get; private set; }

            public int              GenerationFrequency 
            {
                get { return _generationFreq; }
                set 
                {
                    _generationFreq = value;
                    if (!IsDisposed)
                    {
                        Stop();
                        Start();
                    }
                }
            }

            public PictureSender(int generationFrequency, ClientController controller)
            {
                _thisLock           = new object();
                _timer              = null;
                
                ClientController    = controller;
                ScreenCapturer      = new ScreenCapturer();
                CurrFragment        = 0;

                IsDisposed          = true;
                GenerationFrequency = generationFrequency;
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
                    lock (_thisLock)
                    {
                        ScreenCapturer.CaptureScreen();
                    }
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
