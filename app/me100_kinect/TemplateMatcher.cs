using System;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using System.Collections.Generic;

namespace me100_kinect {
    class TemplateMatcher: KinectController {
        public override string mode {get { return "Object Detection";} }

        // Waiting for response from Python service 
        private bool isWaiting = false;
        private readonly Brush translucentBrush = new SolidColorBrush(Color.FromArgb(99, 0, 0, 0));
        private readonly Pen newDevicePen = new Pen(Brushes.Magenta, 2);
        private readonly Pen devicePen = new Pen(Brushes.Lime, 2);
        private readonly DepthImageFormat DEPTH_IMAGE_FORMAT = DepthImageFormat.Resolution640x480Fps30;
        private readonly int WIDTH = 640;

        private DepthImagePixel[] depthPixels;

        private DeviceLocation? tempDeviceLocation = null;
        private int frameCounter = 1;

        public TemplateMatcher(KinectSensor sensor): base(sensor) { }

        public override void initialize() {
            this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
            this.sensor.DepthStream.Enable(DEPTH_IMAGE_FORMAT);

            this.sensor.ColorFrameReady += colorFrameReady;
            this.sensor.DepthFrameReady += depthFrameReady;
        }

        public override object performAction(string action) {
            if (action == "clear") {
                deviceLocations.Clear();
                return null;
            } else if (action == "save") {
                deviceLocations.Clear();
            }
            
            if(tempDeviceLocation != null)
                deviceLocations.Add((DeviceLocation) tempDeviceLocation);

            tempDeviceLocation = null;
            return null; 
        }

        private void sendRequest() {
            if (isWaiting) return;
            // Only send about once per second
            if (frameCounter % 60 != 0) {
                frameCounter += 1;
                return;
            }
            frameCounter = 0;
            isWaiting = true;
            byte[] data;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();

            // Python API accepts JPG images so encode BGRA frame in memory before sending
            encoder.Frames.Add(BitmapFrame.Create(colorBitmap));
            using (MemoryStream ms = new MemoryStream()) {
                encoder.Save(ms);
                data = ms.ToArray();
            }
            HttpClientWrapper.postFile(
                  HttpClientWrapper.PYTHON_API_ADDRESS + "/detect", "out.jpg", data).ContinueWith(onMatchResponse);
            
        }

        private async void onMatchResponse(Task<System.Net.Http.HttpResponseMessage> task) {
            // startX,Y at loc/scale 
            // get depth image and scale accordingly
            // discard matches under certain corr_coeff threshold
            try {
                if (depthPixels == null) return;
                System.Net.Http.HttpResponseMessage res = task.Result;
                if (res.Content == null) return;
                if (deviceLocations == null) return;
                string content = await res.Content.ReadAsStringAsync();
                string[] pts = content.Split('|');
     
                if (pts.Length != 0 && pts[0] != "") {
                    string pt = pts[0];
                    string[] coords = pt.Split(',');
                    
                    // Divide by 2 since video stream has 2x resolution
                    int x = Int32.Parse(coords[0])/2;
                    int y = Int32.Parse(coords[1])/2;
                    int rad = Int32.Parse(coords[2])/2;
                    float z = depthPixels[(y-1)*WIDTH + x].Depth;
                    
                    z /= 1000.0f; // Convert mm to m for depth points
                    if(z != 0) // for points with uncertain depth kinect will report distance = 0
                        tempDeviceLocation = new DeviceLocation(Utils.createSkeletonPoint(x, y, z), rad); 
                } else {
                    tempDeviceLocation = null;
                }
                
            } catch(Exception e) {
                Trace.WriteLine(e);
            }
            isWaiting = false;
        }

        private void depthFrameReady(object _, DepthImageFrameReadyEventArgs e) {
            if (blocked) return;

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame()) {
                if (depthFrame != null) {
                    if (depthPixels == null)
                        depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                    depthFrame.CopyDepthImagePixelDataTo(depthPixels);
                }
            }
        }

        private void colorFrameReady(object _, ColorImageFrameReadyEventArgs e) {
            if (blocked) return;
            
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
                if (colorFrame != null) {
                    if (colorPixels == null)
                        colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                    colorFrame.CopyPixelDataTo(colorPixels);

                    if (colorBitmap == null)
                        colorBitmap =
                            new WriteableBitmap(
                                this.sensor.ColorStream.FrameWidth,
                                this.sensor.ColorStream.FrameHeight,
                                96.0, 96.0, PixelFormats.Bgr32, null);
                    colorBitmap.WritePixels(
                      new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                          colorPixels,
                          colorBitmap.PixelWidth * sizeof(int), 0);
                    sendRequest();
                    renderImages();
                }
            }
        }

        private void renderImages() {
            using (DrawingContext dc = this.draw.Open()) {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.drawWidth, this.drawHeight));
                dc.DrawImage(
                    colorBitmap,
                    new Rect(0.0d, 0.0d, drawWidth, drawHeight));
                
                if (tempDeviceLocation != null) {
                    DeviceLocation d = ((DeviceLocation) tempDeviceLocation);
                    SkeletonPoint pt = d.location;
                    dc.DrawEllipse(null, newDevicePen, new Point(pt.X, pt.Y), d.radius, d.radius);
                }

                if (deviceLocations != null) {
                    foreach (DeviceLocation loc in deviceLocations) {
                        SkeletonPoint pt = loc.location;
                        Utils.drawDeviceCircle(dc, new Point(pt.X, pt.Y), loc.radius, devicePen, String.Format("{0:0.00}m", pt.Z));
                    }
                }
            }

            this.draw.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, drawWidth, drawHeight));
        }
    }
}