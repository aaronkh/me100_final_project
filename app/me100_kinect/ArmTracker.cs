using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace me100_kinect {
    class ArmTracker : KinectController {
        public override string mode { get { return "Body tracking"; } }

        private readonly int INTERACTION_THRESHOLD = 30;

        private readonly Brush translucentBrush = new SolidColorBrush(Color.FromArgb(99, 0, 0, 0));

        private const double JointThickness = 3;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        private readonly Pen extendedBonePen = new Pen(Brushes.Magenta, 2);

        private readonly Pen deviceOnPen = new Pen(Brushes.Yellow, 2);
        private readonly Pen deviceOffPen = new Pen(Brushes.CornflowerBlue, 2);
        private readonly Pen deviceHighlightPen = new Pen(Brushes.Magenta, 2);

        private readonly HashSet<JointType> armJoints = new HashSet<JointType> {
            JointType.ElbowLeft,
            JointType.ElbowRight, 
            JointType.HandLeft,
            JointType.HandRight
        };

        private float[] deviceDistance;

        public ArmTracker(KinectSensor sensor): base(sensor) { }

        public override void initialize() {
            // Turn on the skeleton stream to receive skeleton frames
            this.sensor.SkeletonStream.Enable();
            this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);

            // Add an event handler to be called whenever there is new color frame data
            this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
            this.sensor.SkeletonStream.EnableTrackingInNearRange = true;
            this.sensor.ColorFrameReady += colorFrameReady;
        }

        public override object performAction(string action) {
            for(int i = 0; i < deviceLocations.Count; ++i) {
                DeviceLocation d = deviceLocations[i];
                if(deviceDistance[i] <= d.radius + INTERACTION_THRESHOLD) { 
                    switch(action) {
                        case "on":
                            d.isOn = true;
                            HttpClientWrapper.get(HttpClientWrapper.ESP32_IP+"/on");
                            break;
                        case "off":
                            d.isOn = false;
                            HttpClientWrapper.get(HttpClientWrapper.ESP32_IP + "/off");
                            break;
                        case "switch":
                            if (d.isOn) {
                                HttpClientWrapper.get(HttpClientWrapper.ESP32_IP + "/off");
                            } else {
                                HttpClientWrapper.get(HttpClientWrapper.ESP32_IP + "/on");
                            }
                            d.isOn = !d.isOn;
                            break;
                    }
                    deviceLocations[i] = d;
                }
            }
    
            return null;
        }

       /* * * * * * * * * *
        *                 *
        * DRAWING METHODS *
        *                 *
        * * * * * * * * * */
        private void drawBone(
            Skeleton skeleton, 
            DrawingContext drawingContext, 
            JointType jointType0, 
            JointType jointType1) {
                
            Joint joint0 = skeleton.Joints[jointType0];
            
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked) return;
            
            // Don't draw if both points are inferred
            // if (joint0.TrackingState == JointTrackingState.Inferred &&
            //    joint1.TrackingState == JointTrackingState.Inferred) return;
            
            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && 
                joint1.TrackingState == JointTrackingState.Tracked)
                drawPen = this.trackedBonePen;

            this.drawRay(joint0.Position, joint1.Position, drawingContext);

            // Loop through devices and start highlighting them 
            this.highlightDevices(joint0.Position, joint1.Position);

            drawingContext.DrawLine(drawPen, this.skeletonPointToScreen(joint0.Position), this.skeletonPointToScreen(joint1.Position));
        }

        private void drawArmsAndColor(Skeleton skeleton, DrawingContext drawingContext) {
            if (deviceDistance == null || deviceDistance.Length != deviceLocations.Count) {
                deviceDistance = new float[deviceLocations.Count];
            }

            for (int i = 0; i < deviceDistance.Length; ++i)
                deviceDistance[i] = Int32.MaxValue;

            // Left Arm
            // this.drawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.drawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.HandLeft);
            // this.drawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            // this.drawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.drawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.HandRight);
            // this.drawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Render Joints (yellow = inferred, green = tracked)
            foreach (Joint joint in skeleton.Joints) {
                Brush drawBrush = null;
                if (!armJoints.Contains(joint.JointType)) continue;

                if (joint.TrackingState == JointTrackingState.Tracked)
                    drawBrush = this.trackedJointBrush;
                else if (joint.TrackingState == JointTrackingState.Inferred) 
                    drawBrush = this.inferredJointBrush;

                if (drawBrush != null) 
                    drawingContext.DrawEllipse(drawBrush, null, this.skeletonPointToScreen(joint.Position), JointThickness, JointThickness);
            }
        }

        private void drawRay(
            SkeletonPoint p1, SkeletonPoint p2, 
            DrawingContext drawingContext) {
                
                // Either draw line towards the screen or to max depth
                int rayEnd = 0;
                if (p1.Z == p2.Z) rayEnd = (int) (p1.Z * 1000);
                if (p1.Z < p2.Z) rayEnd = 10 * 1000;

                // d points in format (x, y, mm)
                DepthImagePoint d1 = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(p1, DepthImageFormat.Resolution640x480Fps30);
                DepthImagePoint d2 = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(p2, DepthImageFormat.Resolution640x480Fps30);
                
                // Extends points in (x, y, mm) space
                SkeletonPoint endpoint = Utils.extendLine(d1, d2, rayEnd);
                Point e1 = this.skeletonPointToScreen(p1);
                Point e2 = new Point(endpoint.X, endpoint.Y);
                drawingContext.DrawLine(extendedBonePen, e1, e2);
        }

        // Returns the X, Y point created by continuing the line from p1 to p2 at depth `depth`
        private Point extendLine(SkeletonPoint p1, SkeletonPoint p2, float depth) {
            // Convert back into depth space but in meters
            // SkeletonPoint ret = Utils.extendLine(d1, d2, intersection * 1000);
            
            // d points in format (x, y, mm)
            DepthImagePoint d1 = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(p1, DepthImageFormat.Resolution640x480Fps30);
            DepthImagePoint d2 = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(p2, DepthImageFormat.Resolution640x480Fps30);

            // Convert depth to mm
            depth *= 1000;

            // Extends points in (x, y, mm) space
            SkeletonPoint endpoint = Utils.extendLine(d1, d2, depth);

            return new Point(endpoint.X, endpoint.Y);
        }

        private void highlightDevices(SkeletonPoint p1, SkeletonPoint p2) { 
            
            for (int i = 0; i < deviceLocations.Count; ++i) {
                DeviceLocation loc = deviceLocations[i];
                
                // Extend line to depth of loc 
                Point intersection = extendLine(p1, p2, loc.location.Z);
                
                SkeletonPoint skelPoint = Utils.createSkeletonPoint((float) intersection.X, (float) intersection.Y, loc.location.Z);

                //Trace.WriteLine(Utils.skelPointFormat(skelPoint));
                //Trace.WriteLine(Utils.skelPointFormat(loc.location));
                //Trace.WriteLine(Utils.getDistance(skelPoint, loc.location));
                //Trace.WriteLine("-----");

                deviceDistance[i] = System.Math.Min(deviceDistance[i], Utils.getDistance(skelPoint, loc.location));
            }
        }

        /* * * * * * * * * * *
         *                   *
         * KINECT CALLBACKS  *
         *                   *
         * * * * * * * * * * */
        private void SensorSkeletonFrameReady(object _, SkeletonFrameReadyEventArgs e) {
            if (blocked) return;

            Skeleton[] skeletons = new Skeleton[0];

            // Populate skeletons array
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) {
                if (skeletonFrame != null) {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.draw.Open()) {
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.drawWidth, this.drawHeight));

                if (colorBitmap != null) {
                    dc.DrawImage(
                        colorBitmap,
                        new Rect(0.0d, 0.0d, drawWidth, drawHeight));
                }

                // Dim video stream
                dc.DrawRectangle(translucentBrush, null, new Rect(0.0, 0.0, this.drawWidth, this.drawHeight));

                if (skeletons.Length != 0) {
                    foreach (Skeleton skel in skeletons) {
                        if (skel.TrackingState == SkeletonTrackingState.Tracked) {
                            this.drawArmsAndColor(skel, dc);
                            break;
                        }
                    }
                }

                for (int i = 0; i < deviceLocations.Count; ++i) {
                    DeviceLocation loc = deviceLocations[i];
                    SkeletonPoint pt = loc.location;
                    Pen devicePen = loc.isOn ? deviceOnPen : deviceOffPen;

                    if (deviceDistance != null && deviceDistance[i] <= loc.radius + INTERACTION_THRESHOLD)
                        devicePen = deviceHighlightPen;

                    Utils.drawDeviceCircle(dc, new Point(pt.X, pt.Y), loc.radius, devicePen, String.Format("{0:0.00}m", pt.Z));
                    dc.PushOpacity(0.25);
                    Utils.drawDeviceCircle(dc, new Point(pt.X, pt.Y), loc.radius + INTERACTION_THRESHOLD, devicePen, "");
                    dc.PushOpacity(1.0);
                }

                // prevent drawing outside of our render area
                this.draw.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, drawWidth, drawHeight));
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
                }
            }
        }

        private Point skeletonPointToScreen(SkeletonPoint skelpoint) {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }
    }
}

