using System;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Engine.Models;

namespace Engine.Utilities
{
    public class KinectScanner
    {
        public bool Completed { get; set; }
        public KinectSensor sensor;
        public bool KinectConnected { get; set; }
        private VoxelGrid scaningGrid;
        short[] pixelData;
        private readonly int minDepth = 1000;
        private readonly int maxDepth = 3000;
        float angle = 0;

        public KinectScanner()
        {
            Completed = false;
            //sensor = KinectSensor.KinectSensors.FirstOrDefault();
            if (sensor == null)
            {
                KinectConnected = false;
            }
            else
            {
                sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                sensor.Start();
                KinectConnected = true;
            }
        }

        public void StartScanning(VoxelGrid grid)
        {
            scaningGrid = grid;
            //grid.UpdateWithKinectData(pixelData, 640, 480, angle, maxDepth, minDepth);
            //angle += (float)Math.PI / 2;
        }

        public VoxelGrid NextScan()
        {
            scaningGrid.UpdateWithKinectData(pixelData, 640, 480, angle, maxDepth, minDepth);
            angle += (float)Math.PI / 2;
            if (Math.Abs(angle - Math.PI * 2) < 0.1)
            {
                angle = 0;
                scaningGrid = null;
                Completed = true;
            }
            return scaningGrid;
        }

        public WriteableBitmap CreateBitMapFromDepthFrame(DepthImageFrame frame)
        {
            if (frame != null)
            {
                var bitmapImage = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr565, null);
                pixelData = new short[frame.PixelDataLength];
                frame.CopyPixelDataTo(pixelData);
                for (int i = 0; i < pixelData.Length; i++)
                {
                    pixelData[i] = (short)(pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth);
                }
                bitmapImage.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height), pixelData, 2 * frame.Width, 0);

                return bitmapImage;
            }
            return null;
        }
    }
}
