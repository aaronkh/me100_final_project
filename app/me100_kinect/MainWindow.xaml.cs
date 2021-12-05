//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace me100_kinect
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System;
    using System.Windows.Input;
    using System.Collections.Generic;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;

        private KinectSensor sensor;

        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        private string currentMode;

        private Dictionary<string, KinectController> controllers = new Dictionary<string,KinectController>();

        /* * * * * * * * * * *
         *                   *
         * LIFECYCLE METHODS *
         *                   *
         * * * * * * * * * * */
        public MainWindow() {
            InitializeComponent();
            this.currentMode = MODE.OBJECT_DETECTION;
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
                this.controllers.Add(MODE.BODY_TRACKING, new ArmTracker(this.sensor));
                this.controllers.Add(MODE.OBJECT_DETECTION, new TemplateMatcher(this.sensor));

                foreach (KeyValuePair<string, KinectController> entry in this.controllers) {
                    entry.Value.initialize();
                    entry.Value.setRenderer(this.drawingGroup, RenderHeight, RenderWidth);
                }

                // Start the sensor!
                try {
                    this.sensor.Start();
                } catch (IOException) {
                    this.sensor = null;
                }
            }

            if (this.sensor == null) {
                this.statusBarText.Text = "No Kinect found!";
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (this.sensor != null) this.sensor.Stop();
            setBlocked(true);
        }

        private void onKeyPress(object _, KeyEventArgs e) {
            Console.WriteLine(e.Key);
            switch (e.Key)
            {
                case Key.Space:
                    // Main action
                    setBlocked(true);
                    this.controllers[this.currentMode].performAction();
                    setBlocked(false);
                    return;
                case Key.M:
                    // Switch modes
                    if (this.currentMode == MODE.OBJECT_DETECTION)
                        this.currentMode = MODE.BODY_TRACKING;
                    else
                        this.currentMode = MODE.OBJECT_DETECTION;
                    return;
                case Key.P:
                    // Write to file
                    return;
                case Key.R:
                    // Restart program
                    return;
            }
        }

        private void setBlocked(bool blocked) {
            foreach (KeyValuePair<string, KinectController> entry in this.controllers)
                entry.Value.blocked = blocked;
        }
    }

    static class MODE { 
        public const string OBJECT_DETECTION = "Object detection";
        public const string BODY_TRACKING = "Body tracking";
    }
}