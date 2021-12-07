using Microsoft.Kinect;
using System.Windows.Media;

namespace me100_kinect {
    public abstract class KinectController {
        protected KinectSensor sensor;

        protected DrawingGroup draw; 
        protected float drawHeight, drawWidth; 

        public bool blocked;

        public abstract string mode {get;}

        protected KinectController(KinectSensor sensor) {
            this.sensor = sensor;
        }

        public void setRenderer(DrawingGroup draw, float height, float width) {
            this.draw = draw; 
            this.drawHeight = height; 
            this.drawWidth = width;
        }

        public virtual void saveImage(string path) { }
         
        public abstract void initialize();

        public abstract object performAction();
    }
}

