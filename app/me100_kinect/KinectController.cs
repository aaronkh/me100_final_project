using Microsoft.Kinect;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace me100_kinect {
    public abstract class KinectController {
        protected KinectSensor sensor;

        protected DrawingGroup draw; 
        protected float drawHeight, drawWidth;

        protected WriteableBitmap colorBitmap;
        protected byte[] colorPixels;

        public bool blocked;

        public abstract string mode {get;}
        public List<DeviceLocation> deviceLocations;

        protected KinectController(KinectSensor sensor) {
            this.sensor = sensor;
        }

        public void setRenderer(DrawingGroup draw, float height, float width) {
            this.draw = draw; 
            this.drawHeight = height; 
            this.drawWidth = width;
        }

        public virtual void saveImage(string path) {
            if (colorBitmap != null) {
                // Save the bitmap into a file.
                using (FileStream stream =
                    new FileStream(path, FileMode.Create)) {
                    BitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(colorBitmap));
                    encoder.Save(stream);
                }
            }
        }
         
        public abstract void initialize();

        public abstract object performAction();
    }

    public struct DeviceLocation {
        public SkeletonPoint location;
        public bool isOn;

        public DeviceLocation(SkeletonPoint pt) {
            this.location = pt;
            this.isOn = false;
        }
    }
}

