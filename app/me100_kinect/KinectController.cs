using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media;

namespace me100_kinect
{
    public abstract class KinectController
    {
        protected KinectSensor sensor;

        protected DrawingGroup draw; 
        protected float drawHeight; 
        protected float drawWidth;

        public bool blocked;

        protected KinectController(KinectSensor sensor) {
            this.sensor = sensor;
            this.blocked = false;
        }

        public void setRenderer(DrawingGroup draw, float height, float width) {
            this.draw = draw; 
            this.drawHeight = height; 
            this.drawWidth = width;
        }

        public abstract void initialize();

        public abstract object performAction();

        protected void render(DrawingContext draw, int height, int width) { 
            if(draw == null) return;
            if (this.blocked) {
                this.renderBlocked();
                return;
            }
            return;
        }

        protected void renderBlocked() {
            return;
        }
    }
}
