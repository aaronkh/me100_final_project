namespace me100_kinect {
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System;
    using System.Windows.Input;
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using System.Diagnostics;

    public partial class MainWindow : Window {

        private KinectSensor sensor;

        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;

        private int currentMode = -1; // Start at -1 as 'loading'
        private KinectController[] controllers;

        /* * * * * * * * * * *
         *                   *
         * LIFECYCLE METHODS *
         *                   *
         * * * * * * * * * * */

        public MainWindow() {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e) {
            this.statusBarText.Text = "Loading...";

            // Initialize key listener 
            this.KeyDown += new KeyEventHandler(onKeyPress);

            // Initialize drawing
            this.drawingGroup = new DrawingGroup();
            this.imageSource = new DrawingImage(this.drawingGroup);
            Image.Source = this.imageSource;
            
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.            
            foreach (var potentialSensor in KinectSensor.KinectSensors) {
                if (potentialSensor.Status == KinectStatus.Connected) {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (this.sensor != null) {
                this.controllers = new KinectController[2]{
                    new TemplateMatcher(this.sensor),
                    new ArmTracker(this.sensor)
                };

                foreach (KinectController controller in this.controllers) {
                    controller.initialize();
                    controller.setRenderer(this.drawingGroup, RenderHeight, RenderWidth);
                }
                cycleModes();

                // Start the sensor!
                try {
                    this.sensor.Start();
                } catch (IOException err) {
                    this.sensor = null;
                    this.statusBarText.Text = "Error opening Kinect";
                    Trace.WriteLine(err);
                }
            }

            if (this.sensor == null) {
                this.statusBarText.Text = "No Kinect found!";
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (this.sensor != null) this.sensor.Stop();
            for (int i = 0; i < this.controllers.Length; ++i)
                this.controllers[i].blocked = true;
        }

        private void onKeyPress(object _, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Escape:
                    Close();
                    return;
                case Key.Space:
                    // Main action
                    for (int i = 0; i < this.controllers.Length; ++i) 
                        this.controllers[i].performAction();
                    return;
                case Key.M:
                    // Switch modes
                    cycleModes();
                    return;
                case Key.P:
                    // Write to file
                    this.controllers[currentMode].saveImage("out.jpg");
                    return;
            }
        }

        /* * * * * * * * * *
         *                 *
         * GETTERS/SETTERS *
         *                 *
         * * * * * * * * * */
        
        //

        /* * * * * * * * * *
         *                 *
         * HELPER METHODS  *
         *                 *
         * * * * * * * * * */
        private void cycleModes() {
            // Clear current image
            using(DrawingContext dc = this.drawingGroup.Open())
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            
            currentMode += 1;
            if (currentMode >= controllers.Length)
                currentMode = 0;

            for (int i = 0; i < this.controllers.Length; ++i) { 
                if(i == this.currentMode) {
                    this.controllers[i].blocked = false;
                    this.statusBarText.Text = this.controllers[i].mode;
                } else {
                    this.controllers[i].blocked = true;
                }
            }
        }
    }
}
