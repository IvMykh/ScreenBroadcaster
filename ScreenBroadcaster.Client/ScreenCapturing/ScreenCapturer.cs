using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.IO;

namespace ScreenBroadcaster.Client.ScreenCapturing
{
    public class ScreenCapturer
    {
        public static int ScreenWidth   { get; private set; }
        public static int ScreenHeight  { get; private set; }

        static ScreenCapturer()
        {
            ScreenWidth     = (int)SystemParameters.PrimaryScreenWidth;
            ScreenHeight    = (int)SystemParameters.PrimaryScreenHeight;
        }

        public Bitmap Screenshot { get; private set; }

        ~ScreenCapturer()
        {
            if (Screenshot != null)
            {
                Screenshot.Dispose();
            }
        }

        public void CaptureScreen()
        {
            if (Screenshot != null)
            {
                Screenshot.Dispose();
                Screenshot = null;
            }

            Screenshot = new Bitmap(ScreenWidth, ScreenHeight);

            using (var graphics = Graphics.FromImage(Screenshot))
            {
                var sourceUpLeftPoint = new System.Drawing.Point(0, 0);
                var destUpLeftPoint = new System.Drawing.Point(0, 0);
                graphics.CopyFromScreen(sourceUpLeftPoint, destUpLeftPoint, Screenshot.Size, CopyPixelOperation.SourceCopy);
            }
        }

        public string Base64String
        {
            get
            {
                string base64Representation = null;
                byte[] imageBytes = null;

                using (var stream = new MemoryStream())
                {
                    Screenshot.Save(stream, ImageFormat.Jpeg);
                    imageBytes = stream.ToArray();

                    base64Representation = Convert.ToBase64String(imageBytes);
                }

                return base64Representation;
            }
        }
    }
}
