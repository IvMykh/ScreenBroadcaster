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
        public const int PIC_BLOCK_SIZE = 32 * 1024;
        public const int CHARS_IN_BLOCK = PIC_BLOCK_SIZE / 2;

        public static int ScreenWidth   { get; private set; }
        public static int ScreenHeight  { get; private set; }

        static ScreenCapturer()
        {
            ScreenWidth     = (int)SystemParameters.PrimaryScreenWidth;
            ScreenHeight    = (int)SystemParameters.PrimaryScreenHeight;
        }

        public Bitmap Screenshot { get; private set; }
        public string[] ScreenshotAsBase64Strings { get; private set; }

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

            ScreenshotAsBase64Strings = getScreenshotAsBase64Strings();
        }

        private string[] getScreenshotAsBase64Strings()
        {
            string base64Representation = null;
            byte[] imageBytes = null;

            using (var stream = new MemoryStream())
            {
                Screenshot.Save(stream, ImageFormat.Jpeg);
                imageBytes = stream.ToArray();

                base64Representation = Convert.ToBase64String(imageBytes);
            }

            string[] base64Strings = new string[base64Representation.Length / CHARS_IN_BLOCK + 1];

            int i = 0;
            for (; i < base64Strings.Length - 1; i++)
            {
                base64Strings[i] = base64Representation
                    .Substring(i * CHARS_IN_BLOCK, CHARS_IN_BLOCK);
            }
            base64Strings[base64Strings.Length - 1] = base64Representation.Substring(i * CHARS_IN_BLOCK);

            return base64Strings;
        }
    }
}
