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
        public static int PieceStartX   { get; private set; }
        public static int PieceStartY   { get; private set; }
        public static int PieceWidth    { get; private set; }
        public static int PieceHeight   { get; private set; }

        static ScreenCapturer()
        {
            ScreenWidth     = (int)SystemParameters.PrimaryScreenWidth;
            ScreenHeight    = (int)SystemParameters.PrimaryScreenHeight;

            PieceStartX = 0;
            PieceStartY = 0;

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

            PieceStartX = 0;
            PieceStartY = 0;
            PieceWidth = ScreenWidth;
            PieceHeight = ScreenHeight;
        }

        public void CapturePart(Bitmap baseimage)
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

            Bitmap localImg = (Bitmap)Screenshot;

            getBonds(baseimage, localImg);
            Screenshot = CropImage(localImg);

            ScreenshotAsBase64Strings = getScreenshotAsBase64Strings();
        }

        private void getBonds(Bitmap baseimage, Bitmap currimg)
        {
            PieceStartY = currimg.Height;
            int bottom = 0;
            PieceStartX = currimg.Width;
            int right = 0;

            for (int i = 0; i < Screenshot.Height; ++i)
            {
                for (int j = 0; j < Screenshot.Width; ++j)
                {
                    if (baseimage.GetPixel(j, i) != currimg.GetPixel(j, i))
                    {
                        if (PieceStartY > i)
                        {
                            PieceStartY = i;
                        }
                        if (bottom < i)
                        {
                            bottom = i;
                        }
                        if (PieceStartX > j)
                        {
                            PieceStartX = j;
                        }
                        if (right < j)
                        {
                            right = j;
                        }
                    }
                }
            }

            PieceWidth = right - PieceStartX;
            PieceHeight = bottom - PieceStartY;
        }

        private Bitmap CropImage(Bitmap currimg)
        {
            Bitmap res = new Bitmap(PieceWidth, PieceHeight);
            using (Graphics gr = Graphics.FromImage(res))
            {
                gr.DrawImage(currimg, 0, 0,
                    new System.Drawing.Rectangle(PieceStartX, PieceStartY, PieceWidth, PieceHeight),
                    GraphicsUnit.Pixel);
            }

            return res;
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
