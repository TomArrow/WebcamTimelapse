using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using AForge.Video;
using AForge.Video.DirectShow;


namespace WebcamTimelapseNET5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            dostuff();
        }

        ~MainWindow()
        {

            Flush();
        }

        private void Flush()
        {
            byte[] writeBuffer = new byte[frameAddBuffer.Length];
            lock (frameAddBuffer)
            {

                float tmp;
                for (int i = 0; i < frameAddBuffer.Length; i++)
                {
                    tmp = frameAddBuffer[i] / framesAddedCount;
                    tmp = tmp > 0.0031308f ? 1.055f * (float)Math.Pow(tmp, 1 / 2.4) - 0.055f : 12.92f * tmp;
                    writeBuffer[i] = (byte)Math.Max(0.0f, Math.Min(255.0f, tmp * 255.0f));
                    frameAddBuffer[i] = 0;
                }
                framesAddedCount = 0;
            }

            LinearAccessByteImageUnsignedNonVectorized writeImg = new LinearAccessByteImageUnsignedNonVectorized(writeBuffer, imageHusk);

            writeImg.ToBitmap().Save(GetUnusedFilename("test.png"));
        }

        public void dostuff(){
            // enumerate video devices
            var videoDevices = new FilterInfoCollection(
                    FilterCategory.VideoInputDevice);
            // create video source
            VideoCaptureDevice videoSource = new VideoCaptureDevice(
                    videoDevices[1].MonikerString);

            //VideoCapabilities bestIThink = videoSource.VideoCapabilities[videoSource.VideoCapabilities.Length - 1];
            VideoCapabilities bestIThink = videoSource.VideoCapabilities[0];
            
            videoSource.VideoResolution = bestIThink;
            width = bestIThink.FrameSize.Width;
            height = bestIThink.FrameSize.Height;
            frameAddBuffer = new float[3*width * height];
            // set NewFrame event handler
            videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
            // start the video source
            videoSource.Start();
            // ...
            // signal to stop
            //videoSource.SignalToStop();
            // ...

        }

        float[] frameAddBuffer;
        float framesAddedCount;

        int width;
        int height;

        LinearAccessByteImageUnsignedHusk imageHusk = null;
        string imageHuskLock = "abc";

        private void video_NewFrame(object sender,
                NewFrameEventArgs eventArgs)
        {

            // get new frame
            LinearAccessByteImageUnsignedNonVectorized img = LinearAccessByteImageUnsignedNonVectorized.FromBitmap(eventArgs.Frame);

            lock (imageHuskLock)
            {
                if(imageHusk == null)
                {
                    imageHusk = img.toHusk();
                }
            }
            // process the frame
            //bitmap.Save(GetUnusedFilename("frame.png"));
            float n;
            lock (frameAddBuffer)
            {

                for (int i = 0; i < frameAddBuffer.Length; i++)
                {
                    n = (float)img.imageData[i] / 255.0f;
                    frameAddBuffer[i] += (n > 0.04045f ? (float)Math.Pow((n + 0.055) / 1.055, 2.4) : n / 12.92f);
                }
                framesAddedCount++;
            }

            if (framesAddedCount > 1)
            {
                Flush();
            }
        }

        public static string GetUnusedFilename(string baseFilename)
        {
            if (!File.Exists(baseFilename))
            {
                return baseFilename;
            }
            string extension = Path.GetExtension(baseFilename);

            int index = 1;
            while (File.Exists(Path.ChangeExtension(baseFilename, "." + (++index) + extension))) ;

            return Path.ChangeExtension(baseFilename, "." + (index) + extension);
        }
    }
}
