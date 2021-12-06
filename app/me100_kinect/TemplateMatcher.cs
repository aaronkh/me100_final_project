﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace me100_kinect {
    class TemplateMatcher: KinectController {
        public override string mode {get { return "Object Detection";} }

        public TemplateMatcher(KinectSensor sensor): base(sensor) { }

        public override void initialize() {}

        public override object performAction() { return null; }
    }
}
