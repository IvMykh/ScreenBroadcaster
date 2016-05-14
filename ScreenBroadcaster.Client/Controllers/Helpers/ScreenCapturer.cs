using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using ScreenBroadcaster.Client.Properties;

namespace ScreenBroadcaster.Client.Controllers.Helpers
{
    public class ScreenCapturer
    {
        public static int ScreenWidth   { get; private set; }
        public static int ScreenHeight  { get; private set; }
        public static int CharsInBlock  { get; private set; }

        static ScreenCapturer()
        {
            ScreenWidth     = (int)SystemParameters.PrimaryScreenWidth;
            ScreenHeight    = (int)SystemParameters.PrimaryScreenHeight;

            var picBlockSizeInKb = int.Parse(Resources.PictureBlockSizeInKb) * 1024;
            CharsInBlock = picBlockSizeInKb / 2;
        }

        public Image    Screenshot                  { get; private set; }
        public string[] ScreenshotAsBase64Strings   { get; private set; }

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

        private ImageCodecInfo getEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        private string[] getScreenshotAsBase64Strings()
        {
            string base64Representation = null;
            byte[] imageBytes = null;

            using (var stream = new MemoryStream())
            {
                ImageCodecInfo jpgEncoder = getEncoder(ImageFormat.Jpeg);
                System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
                EncoderParameters myEncoderParameters = new EncoderParameters(1);

                EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, 15L);
                myEncoderParameters.Param[0] = myEncoderParameter;

                Screenshot.Save(stream, jpgEncoder, myEncoderParameters);
                imageBytes = stream.ToArray();

                base64Representation = Convert.ToBase64String(imageBytes);
            }

            string[] base64Strings = new string[base64Representation.Length / CharsInBlock + 1];

            int i = 0;
            for (; i < base64Strings.Length - 1; i++)
            {
                base64Strings[i] = base64Representation
                    .Substring(i * CharsInBlock, CharsInBlock);
            }
            base64Strings[base64Strings.Length - 1] = base64Representation.Substring(i * CharsInBlock);

            return base64Strings;
        }
    }
}
