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

namespace me100_kinect {
    class TemplateMatcher: KinectController {
        public override string mode {get { return "Object Detection";} }

        // Waiting for response from Python service 
        private bool isWaiting = false;
        private readonly Brush translucentBrush = new SolidColorBrush(Color.FromArgb(99, 0, 0, 0));
        private readonly Pen devicePen = new Pen(Brushes.Navy, 2);
        private readonly DepthImageFormat DEPTH_IMAGE_FORMAT = DepthImageFormat.Resolution640x480Fps30;
        private readonly int HEIGHT = 480;
        private readonly int WIDTH = 640;

        private WriteableBitmap depthBitmap;
        private DepthImagePixel[] depthPixels;
        private byte[] depthRGB;

        public TemplateMatcher(KinectSensor sensor): base(sensor) { }

        public override void initialize() {
            this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);
            this.sensor.DepthStream.Enable(DEPTH_IMAGE_FORMAT);

            this.sensor.ColorFrameReady += colorFrameReady;
            this.sensor.DepthFrameReady += depthFrameReady;
        }

        public override object performAction() {
            if (colorBitmap == null || blocked || isWaiting) return null;
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
                HttpClientWrapper.PYTHON_API_ADDRESS+"/detect", "out.jpg", data).ContinueWith(onMatchResponse);
            
            return null; 
        }

        private async void onMatchResponse(Task<System.Net.Http.HttpResponseMessage> task) {
            // startX,Y at loc/scale 
            // get depth image and scale accordingly
            // discard matches under certain corr_coeff threshold
            try {
                System.Net.Http.HttpResponseMessage res = task.Result;
                if (res.Content == null) return;
                if (deviceLocations == null) return;
                string content = await res.Content.ReadAsStringAsync();
                string[] pts = content.Split('|');
                deviceLocations.Clear();
                
                SkeletonPoint[] temp = new SkeletonPoint[depthPixels.Length];
                this.sensor.CoordinateMapper.MapDepthFrameToSkeletonFrame(
                    DEPTH_IMAGE_FORMAT,
                    depthPixels,
                    temp);
                foreach (string pt in pts) {
                    if (pt.Length == 0) continue;
                    string[] coords = pt.Split(',');
                    
                    // Divide by 2 since video stream has 2x resolution
                    int x = Int32.Parse(coords[0])/2;
                    int y = Int32.Parse(coords[1])/2;
                        
                    int z = 2; // TODO, get actual depth from stream
                    deviceLocations.Add(temp[x+y*WIDTH]);
                }
                
            } catch(Exception e) {
                Trace.WriteLine(e);
            }
            isWaiting = false;
        }

        private void depthFrameReady(object _, DepthImageFrameReadyEventArgs e) {
            if (!willUpdateFrame()) {
                renderBlocked();
                return;
            } 

            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame()) {
                if (!isWaiting && depthFrame != null) {
                    if (depthPixels == null)
                        depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                    depthFrame.CopyDepthImagePixelDataTo(depthPixels);

                    if (depthBitmap == null)
                        depthBitmap =
                            new WriteableBitmap(
                                this.sensor.DepthStream.FrameWidth,
                                this.sensor.DepthStream.FrameHeight,
                                96.0, 96.0, PixelFormats.Bgr32, null);
                    if(depthRGB == null)
                        depthRGB = new byte[this.sensor.DepthStream.FramePixelDataLength * sizeof(int)];
                    
                    // Get the min and max reliable depth for the current frame
                    int minDepth = depthFrame.MinDepth;
                    int maxDepth = depthFrame.MaxDepth;

                    // Convert the depth to RGB
                    int colorPixelIndex = 0;
                    for (int i = 0; i < this.depthPixels.Length; ++i) {
                        // Get the depth for this pixel
                        short depth = depthPixels[i].Depth;

                        // To convert to a byte, we're discarding the most-significant
                        // rather than least-significant bits.
                        // We're preserving detail, although the intensity will "wrap."
                        // Values outside the reliable depth range are mapped to 0 (black).

                        // Note: Using conditionals in this loop could degrade performance.
                        // Consider using a lookup table instead when writing production code.
                        // See the KinectDepthViewer class used by the KinectExplorer sample
                        // for a lookup table example.
                        byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);
                        
                        // Write out blue byte
                        this.depthRGB[colorPixelIndex++] = 0;

                        // Write out green byte
                        this.depthRGB[colorPixelIndex++] = 0;

                        // Write out red byte                        
                        this.depthRGB[colorPixelIndex++] = intensity;

                        // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                        // If we were outputting BGRA, we would write alpha here.
                        ++colorPixelIndex;
                    }
      
                    depthBitmap.WritePixels(
                      new Int32Rect(0, 0, depthBitmap.PixelWidth, depthBitmap.PixelHeight),
                          depthRGB,
                          depthBitmap.PixelWidth * sizeof(int), 0);
                }
            }
        }

        private void colorFrameReady(object _, ColorImageFrameReadyEventArgs e) {
            if (!willUpdateFrame()) {
                renderBlocked();
                return;
            }
            
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
                if (!isWaiting && colorFrame != null) {
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
                dc.DrawRectangle(translucentBrush, null, new Rect(0.0, 0.0, this.drawWidth, this.drawHeight));
                dc.PushOpacity(0.5);
                dc.DrawImage(
                    depthBitmap,
                    new Rect(0.0d, 0.0d, drawWidth, drawHeight));
                dc.PushOpacity(1.0);
                if (deviceLocations != null) {
                    foreach (SkeletonPoint pt in deviceLocations) {
                        // DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(pt, DepthImageFormat.Resolution640x480Fps30);
                        // Trace.WriteLine(pt.X);
                        dc.DrawEllipse(null, devicePen, new Point(pt.X, pt.Y), 20, 20);
                    }
                
                }
            }

            this.draw.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, drawWidth, drawHeight));
        }

        private void renderBlocked() {
            if (blocked) return;
            using (DrawingContext dc = this.draw.Open()) {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.drawWidth, this.drawHeight));
                FormattedText text = new FormattedText("Detecting...",
                        CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight,
                        new Typeface("Verdana"),
                        16, Brushes.White);
                text.TextAlignment = TextAlignment.Center;
                dc.DrawText(text, new Point(drawWidth/2, drawHeight/2));
            }

            this.draw.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, drawWidth, drawHeight));
        }

        private bool willUpdateFrame() {
            return !blocked && !isWaiting;
        }
    }
}

