using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using AForge.Video.DirectShow;
using AForge.Video;
using System.Drawing;
using System.Numerics;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Threading;
using System.Collections.Concurrent;

namespace WebcamTimelapseNET5
{
    class CapturerAforge
    {

        int floatVectorCount = Vector<float>.Count;

        VideoCaptureDevice videoSource;

        ~CapturerAforge()
        {
            videoSource.SignalToStop();
            Flush();
        }

        public struct CaptureResult
        {
            public float lastDiff;
            public float calculatedFramesPerFrame;
            public BitmapImage image;
        }

        private Action<CaptureResult> _resultAnalysis;

        private Matrix4x4 inverseRec709Fix;
        private Matrix4x4 inverseRec709Fix_transposed;

        private Vector3 vec255 = new Vector3(255f, 255f, 255f);
        private Vector3 vec0 = Vector3.Zero;

        public CapturerAforge(Action<CaptureResult> resultAnalysis)
        {
            _resultAnalysis = resultAnalysis;

            if(Matrix4x4.Invert(Color.FixRGBConvertedAsPC709fromLimitedSource,out inverseRec709Fix))
            {
                inverseRec709Fix_transposed = Matrix4x4.Transpose(inverseRec709Fix);
            } else
            {
                throw new Exception("Rec709 fix matrix cannot be inverted.");
            }
        }

        private float _framesPerFrame = 120;

        bool hasAlreadyFinishedOneImage = false;

        static string statsLog = "capture.stats.csv";
        static string flushLock = "blah";

        
        private void Flush()
        {
            if (Monitor.TryEnter(flushLock)) // Don't wait. Just ignore and do it another time.
            {
                try
                {
                    bool isRelDiff = _settings.diffType == TimelapseSettings.DiffType.RELATIVE;
                    double noiseThreshold = _settings.minDiffNoiseThreshold;

                    // use object
                
                    //lock (flushLock) {  // Don't want two flush processes interfering with each otehr


                    float diff = 0;
                    byte[] writeBuffer = new byte[frameAddBuffer.Length];
                    lock (frameAddBuffer)
                    {

                        float tmp;
                        float bigger, smaller,delta;
                        float tmpdiff,tmpAbsDiff;
                        /*for (int i = 0; i < frameAddBuffer.Length; i++)
                        {
                            tmp = frameAddBuffer[i] / framesAddedCount;
                            //diff += Math.Abs(tmp - lastFrameBuffer[i]);
                            tmpAbsDiff = Math.Abs(tmp - lastFrameBuffer[i]);
                            if (isRelDiff)
                            {
                                bigger = Math.Max(lastFrameBuffer[i], tmp);
                                smaller = Math.Min(lastFrameBuffer[i], tmp);
                                delta = bigger - smaller;
                                tmpdiff = bigger==0?0: delta / bigger;
                            } else
                            {

                                tmpdiff = tmpAbsDiff;
                            }
                            if (tmpAbsDiff < noiseThreshold) { tmpdiff = 0.0f; }
                            diff += tmpdiff;

                            lastFrameBuffer[i] = tmp;
                            tmp = tmp > 0.0031308f ? 1.055f * (float)Math.Pow(tmp, 1 / 2.4) - 0.055f : 12.92f * tmp;
                            writeBuffer[i] = (byte)Math.Max(0.0f, Math.Min(255.0f, tmp * 255.0f));
                            frameAddBuffer[i] = 0;
                        }*/
                        Vector3 tmpVec,lastFrameVec,tmpAbsDiffVec;
                        Vector3 tmpdiffVec, biggerVec, smallerVec, deltaVec;
                        Vector3 tmpVecAbs, tmpVecSign;
                        for (int i = 0; i < frameAddBuffer.Length-2; i+=3) //Vectorize try
                        {
                            tmpVec.X = frameAddBuffer[i];
                            tmpVec.Y = frameAddBuffer[i+1];
                            tmpVec.Z = frameAddBuffer[i+2];
                            lastFrameVec.X = lastFrameBuffer[i];
                            lastFrameVec.Y = lastFrameBuffer[i+1];
                            lastFrameVec.Z = lastFrameBuffer[i+2];
                            tmpVec /= framesAddedCount;
                            //tmp = frameAddBuffer[i] / framesAddedCount;
                            tmpAbsDiffVec = Vector3.Abs(tmpVec - lastFrameVec);// Math.Abs(tmp - lastFrameBuffer[i]);
                            tmpAbsDiff = tmpAbsDiffVec.X + tmpAbsDiffVec.Y + tmpAbsDiffVec.Z;
                            if (isRelDiff)
                            {
                                biggerVec = Vector3.Max(lastFrameVec, tmpVec);
                                smallerVec = Vector3.Min(lastFrameVec, tmpVec);
                                deltaVec = biggerVec - smallerVec;
                                tmpdiffVec = deltaVec / biggerVec;
                                //tmpdiff = bigger == 0 ? 0 : delta / bigger;
                                //tmpdiff = bigger == 0 ? 0 : delta / bigger;
                                tmpdiff = (biggerVec.X == 0 ? 0 : tmpdiffVec.X) +
                                    (biggerVec.Y == 0 ? 0 : tmpdiffVec.Y) +
                                    (biggerVec.Z == 0 ? 0 : tmpdiffVec.Z); // Todo check if this creates issues with negative values
                            }
                            else
                            {

                                tmpdiff = tmpAbsDiff;
                            }
                            if (tmpAbsDiff/3 < noiseThreshold) { tmpdiff = 0.0f; }
                            diff += tmpdiff;

                            lastFrameBuffer[i] = tmpVec.X;
                            lastFrameBuffer[i+1] = tmpVec.Y;
                            lastFrameBuffer[i+2] = tmpVec.Z;
                            if(_settings.outputType == TimelapseSettings.OutputType.Rec709_FULL)
                            {
                                tmpVecAbs = Vector3.Abs(tmpVec);
                                tmpVecSign = tmpVec/ tmpVecAbs;
                                tmpVec.X = (float)Math.Pow(tmpVecAbs.X, 1 / 2.4)*tmpVecSign.X;
                                tmpVec.Y = (float)Math.Pow(tmpVecAbs.Y, 1 / 2.4) * tmpVecSign.Y;
                                tmpVec.Z = (float)Math.Pow(tmpVecAbs.Z, 1 / 2.4) * tmpVecSign.Z;
                                tmpVec = Vector3.Max(vec0, Vector3.Min(vec255, tmpVec * 255.0f));
                                writeBuffer[i] = (byte)tmpVec.X;
                                writeBuffer[i + 1] = (byte)tmpVec.Y;
                                writeBuffer[i + 2] = (byte)tmpVec.Z;
                            } else if(_settings.outputType == TimelapseSettings.OutputType.Rec709_LIMITED)
                            {
                                tmpVecAbs = Vector3.Abs(tmpVec);
                                tmpVecSign = tmpVec / tmpVecAbs;
                                tmpVec.X = (float)Math.Pow(tmpVecAbs.X, 1 / 2.4) * tmpVecSign.X;
                                tmpVec.Y = (float)Math.Pow(tmpVecAbs.Y, 1 / 2.4) * tmpVecSign.Y;
                                tmpVec.Z = (float)Math.Pow(tmpVecAbs.Z, 1 / 2.4) * tmpVecSign.Z;
                                /*tmpVec.X = (float)Math.Pow(tmpVec.X, 1 / 2.4);
                                tmpVec.Y = (float)Math.Pow(tmpVec.Y, 1 / 2.4);
                                tmpVec.Z = (float)Math.Pow(tmpVec.Z, 1 / 2.4);*/
                                tmpVec = Vector3.Transform(tmpVec, inverseRec709Fix_transposed);
                                tmpVec = Vector3.Max(vec0, Vector3.Min(vec255, tmpVec));
                                writeBuffer[i] = (byte)tmpVec.X;
                                writeBuffer[i + 1] = (byte)tmpVec.Y;
                                writeBuffer[i + 2] = (byte)tmpVec.Z;
                            } else //if(_settings.outputType == TimelapseSettings.OutputType.SRGB)
                            {
                                tmpVec.X = tmpVec.X > 0.0031308f ? 1.055f * (float)Math.Pow(tmpVec.X, 1 / 2.4) - 0.055f : 12.92f * tmpVec.X;
                                tmpVec.Y = tmpVec.Y > 0.0031308f ? 1.055f * (float)Math.Pow(tmpVec.Y, 1 / 2.4) - 0.055f : 12.92f * tmpVec.Y;
                                tmpVec.Z = tmpVec.Z > 0.0031308f ? 1.055f * (float)Math.Pow(tmpVec.Z, 1 / 2.4) - 0.055f : 12.92f * tmpVec.Z;
                                tmpVec = Vector3.Max(vec0, Vector3.Min(vec255, tmpVec * 255.0f));
                                //writeBuffer[i] = (byte)Math.Max(0.0f, Math.Min(255.0f, tmp * 255.0f));
                                writeBuffer[i] = (byte)tmpVec.X;
                                writeBuffer[i+1] = (byte)tmpVec.Y;
                                writeBuffer[i+2] = (byte)tmpVec.Z;
                            }
                            frameAddBuffer[i] = 0;
                            frameAddBuffer[i+1] = 0;
                            frameAddBuffer[i+2] = 0;
                        }
                        framesAddedCount = 0;
                    }

                    diff = 100.0f * diff / (float)frameAddBuffer.Length;
            

                    float thresholdedDiff = Math.Max((float)_settings.diffLowestThreshold,Math.Min((float)_settings.diffHighestThreshold,diff));
                    float diffRange = (float)_settings.diffHighestThreshold - (float)_settings.diffLowestThreshold;
                    float normalizedDiff = (thresholdedDiff - (float)_settings.diffLowestThreshold) / diffRange;
                    float framesPerFrameRange = (float)_settings.maxFramesPerFrame - (float)_settings.minFramesPerFrame;
                    float newFramesPerFrame = (float)_settings.minFramesPerFrame + (1.0f-normalizedDiff) * framesPerFrameRange;

                    if (!hasAlreadyFinishedOneImage)
                    {
                        diff = float.NaN;
                    }

                    float tmpFpFDiff = newFramesPerFrame - _framesPerFrame;

                    // Make sure we respect limits on how quickly the frames per frame speed can change
                    // Tyically you want the up direction to be more restrained.
                    // For example if you suddenly start moving around, you want quick reaction of the 
                    // timelapse to a higher temporal resolution
                    // But if you stop moving or slow down, you don't want to immediately drop down
                    // Because maybe it was just a short interruption of activity.
                    if(tmpFpFDiff > 0)
                    {
                        if (Math.Abs(tmpFpFDiff) > _settings.framesPerFrameMaxStepUp)
                        {

                            newFramesPerFrame = _framesPerFrame + (float)_settings.framesPerFrameMaxStepUp;
                        }
                    } else if(tmpFpFDiff < 0)
                    {
                        if (Math.Abs(tmpFpFDiff) > _settings.framesPerFrameMaxStepDown)
                        {

                            newFramesPerFrame = _framesPerFrame - (float)_settings.framesPerFrameMaxStepDown;
                        }
                    }

                    float framesPerFrameOld = _framesPerFrame;
                    _framesPerFrame = newFramesPerFrame;

                    LinearAccessByteImageUnsignedNonVectorized writeImg = new LinearAccessByteImageUnsignedNonVectorized(writeBuffer, imageHusk);

                    //long longstamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                    long longstamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
                    long? timeSinceLast = lastStamp.HasValue?  longstamp - lastStamp : null;
                    lastStamp = longstamp;
                    string timestamp = longstamp.ToString();
                    //writeImg.ToBitmap().Save(GetUnusedFilename("test.png"));
                    Directory.CreateDirectory("captureNum");
                    using (Bitmap myBitmap = writeImg.ToBitmap())
                    {
                        lock (statsLog)
                        {

                            if (logFirstStamp == null)
                            {
                                logFirstStamp = getLogFirstStamp();
                                if (logFirstStamp == null)
                                {
                                    logFirstStamp = longstamp;
                                }
                            }

                            if (fileNumber ==null)
                            {
                                fileNumber = getLastFileNumber();
                                if (fileNumber == null)
                                {
                                    fileNumber = 0;
                                }
                                else
                                {
                                    fileNumber++;
                                }
                            }

                            //myBitmap.Save(GetUnusedFilename("captureNum/" + timestamp + ".png"));
                            string filename = GetUnusedFilename("captureNum/" + fileNumber + ".png");
                            myBitmap.Save(filename);


                            if (!File.Exists(statsLog))
                            {
                                File.AppendAllLines(statsLog,new string[] { "timestamp;timestamp_relative;fpf;diff;timeSinceLast;ffmpegline;timestampRelative_00" });
                            }

                            long timestampRelative = longstamp - logFirstStamp.Value;

                            File.AppendAllLines(statsLog, new string[] { timestamp.ToString()+";"+ timestampRelative.ToString()+ ";"+framesPerFrameOld+";"+diff.ToString()+";"+timeSinceLast.ToString()+";"+"\"file '"+ filename + "'\""+";\""+timestampRelative.ToString()+ ".00\"" });
                            fileNumber++;
                        }
                        BitmapImage tmp = BitmapToImageSource(myBitmap);
                        tmp.Freeze();
                        _resultAnalysis(new CaptureResult() { image = tmp, lastDiff = diff, calculatedFramesPerFrame = newFramesPerFrame }); ;

                    }
                    hasAlreadyFinishedOneImage = true;
                    //}
                }
                finally
                {
                    Monitor.Exit(flushLock);
                }
            }
        }

        private static long? getLastFileNumber()
        {
            string[] filesInDir = Directory.GetFiles("captureNum","*.png");
            long? biggestNumber = null;
            long tmp;
            foreach(string file in filesInDir)
            {
                string splitFile = Path.GetFileNameWithoutExtension(file);
                if(long.TryParse(splitFile,out tmp))
                {
                    if(tmp > biggestNumber || biggestNumber == null)
                    {
                        biggestNumber = tmp;
                    }
                }
            }
            return biggestNumber;
        }

        private static long? getLogFirstStamp()
        {
            long? retVal = null;
            lock (statsLog)
            {
                if (!File.Exists(statsLog))
                {
                    return null;
                }

                using (System.IO.StreamReader file = new System.IO.StreamReader(statsLog))
                {

                    string line;
                    int index = 0;
                    while ((line = file.ReadLine()) != null)
                    {
                        if (index == 1) // skip first line because it contains only the column names
                        {
                            string[] lineparts = line.Split(";");
                            long tmp;
                            if(long.TryParse(lineparts[0],out tmp))
                            {
                                retVal = tmp;
                            }
                            break;
                        }
                        index++;
                    }

                }
            }
            return retVal;
        }

        long? lastStamp=null;
        long? logFirstStamp=null;
        long? fileNumber = null;
        long statsLogStartFps = 0;

        public static FilterInfoCollection getVideoSources()
        {
            return new FilterInfoCollection(
                    FilterCategory.VideoInputDevice);
        }

        private TimelapseSettings _settings = null;

        public void dostuff(FilterInfo filterInfo,TimelapseSettings settings)
        {
            _settings = settings;
            _framesPerFrame = (float)settings.minFramesPerFrame;
            // enumerate video devices
            FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            // create video source
            videoSource = new VideoCaptureDevice(filterInfo.MonikerString);
            //videoSource = new VideoCaptureDevice(videoDevices[1].MonikerString);

            //VideoCapabilities bestIThink = videoSource.VideoCapabilities[videoSource.VideoCapabilities.Length - 1];
            VideoCapabilities bestIThink = videoSource.VideoCapabilities[0];

            foreach(VideoCapabilities type in videoSource.VideoCapabilities)
            {
                if(type.FrameSize.Width* type.FrameSize.Height > bestIThink.FrameSize.Width * bestIThink.FrameSize.Height)
                {
                    bestIThink = type;
                } else if(type.FrameSize.Width * type.FrameSize.Height == bestIThink.FrameSize.Width * bestIThink.FrameSize.Height)
                {
                    if(type.MaximumFrameRate > bestIThink.MaximumFrameRate)
                    {
                        bestIThink = type;
                    }
                }
            }


            videoSource.VideoResolution = bestIThink;
            width = bestIThink.FrameSize.Width;
            height = bestIThink.FrameSize.Height;
            frameAddBuffer = new float[3 * width * height];
            lastFrameBuffer = new float[3 * width * height];
            // set NewFrame event handler
            videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
            // start the video source
            videoSource.Start();
            // ...
            // signal to stop
            //videoSource.SignalToStop();
            // ...

        }

        // all these 3 below are covered by a lock on frameaddbuffer.
        // dont access either of them without locking on frameaddbuffer.
        float[] frameAddBuffer;
        float[] lastFrameBuffer;
        float framesAddedCount;

        int width;
        int height;

        LinearAccessByteImageUnsignedHusk imageHusk = null;
        string imageHuskLock = "abc";

        ConcurrentDictionary<long,Task> framesBeingProcessed = new ConcurrentDictionary<long,Task>();
        int maxFramesBeingProcessed = Environment.ProcessorCount;

        long index = 0;
        private void video_NewFrame(object sender,
                NewFrameEventArgs eventArgs)
        {
            index++;
            if(framesBeingProcessed.Count < Math.Min(maxFramesBeingProcessed,_settings.maxSimultaneouslyProcessedFrames))
            {
                Bitmap imgHere = (Bitmap) eventArgs.Frame.Clone();
                long indexHere = index;
                //Task.Run()
                //Task processingTask = Task.Run(() =>
                //Task processingTask = Task.Factory.StartNew(() =>
                Task processingTask = new Task(() =>
                {

                    // get new frame
                    LinearAccessByteImageUnsignedNonVectorized img = LinearAccessByteImageUnsignedNonVectorized.FromBitmap(imgHere);
                    imgHere.Dispose();

                    lock (imageHuskLock)
                    {
                        if (imageHusk == null)
                        {
                            imageHusk = img.toHusk();
                        }
                    }
                    // process the frame
                    //bitmap.Save(GetUnusedFilename("frame.png"));
                    float n;
                    float[] floatBuffer = new float[frameAddBuffer.Length];

                    // Todo: Allow limited RGB output for output (to not lose detail)
                    // Todo: Allow dithering for output.
                    if(_settings.inputType == TimelapseSettings.InputType.Rec709_FULL)
                    {
                        for (int i = 0; i < frameAddBuffer.Length; i++)
                        {
                            n = (float)img.imageData[i] / 255.0f;
                            floatBuffer[i] = (float)Math.Pow(Math.Abs(n), 2.4)*Math.Sign(n);
                        }
                    } else if(_settings.inputType == TimelapseSettings.InputType.Rec709_LIMITED)
                    {
                        Vector3 tmp,tmpAbs,tmpSign;

                        for (int i = 0; i < frameAddBuffer.Length-2; i+=3)
                        {
                            tmp.X = img.imageData[i];
                            tmp.Y = img.imageData[i+1];
                            tmp.Z = img.imageData[i+2];
                            tmp = Vector3.Transform(tmp, Color.FixRGBConvertedAsPC709fromLimitedSource_transposed);
                            tmpAbs = Vector3.Abs(tmp);
                            tmpSign = tmp/tmpAbs;
                            floatBuffer[i] = (float)Math.Pow(tmpAbs.X,2.4)*tmpSign.X;
                            floatBuffer[i+1] = (float)Math.Pow(tmpAbs.Y, 2.4) * tmpSign.Y;
                            floatBuffer[i+2] = (float)Math.Pow(tmpAbs.Z, 2.4) * tmpSign.Z;
                        }
                    } else //if (_settings.inputType == TimelapseSettings.InputType.SRGB)
                    {
                        for (int i = 0; i < frameAddBuffer.Length; i++)
                        {
                            n = (float)img.imageData[i] / 255.0f;
                            floatBuffer[i] = (n > 0.04045f ? (float)Math.Pow((n + 0.055) / 1.055, 2.4) : n / 12.92f);
                        }
                    }

                    lock (frameAddBuffer)
                    {
                        floatArrayAddVectorized(ref frameAddBuffer, ref floatBuffer);
                        framesAddedCount++;
                        /*for (int i = 0; i < frameAddBuffer.Length; i++)
                        {
                            n = (float)img.imageData[i] / 255.0f;
                            frameAddBuffer[i] += (n > 0.04045f ? (float)Math.Pow((n + 0.055) / 1.055, 2.4) : n / 12.92f);
                        }
                        framesAddedCount++;*/

                        if (framesAddedCount > _framesPerFrame)
                        {
                            Flush();
                        }
                    }

                    if (framesBeingProcessed.ContainsKey(indexHere))
                    {

                        bool success = false;
                        while (!success)
                        {

                            success = framesBeingProcessed.TryRemove(indexHere, out _);
                        }
                    }
                });
                bool success = false;
                while (!success)
                {

                    success = framesBeingProcessed.TryAdd(indexHere,processingTask);
                }
                processingTask.Start();
            }
        }

        public static void floatArrayAddVectorized(ref float[] inputArray, ref float[] addArray)
        {

            int vectorMultiplyLength = inputArray.Length / Vector<float>.Count * Vector<float>.Count;

            Vector<float> input, add;
            for (int i = 0; i < vectorMultiplyLength; i+=Vector<float>.Count)
            {
                input = new Vector<float>(inputArray, i);
                add = new Vector<float>(addArray, i);
                input += add;
                input.CopyTo(inputArray, i);
            }
            // Do the leftovers
            for (int i = vectorMultiplyLength-1; i < inputArray.Length; i++)
            {
                inputArray[i] += addArray[i];
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
        static public BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

    }
}
