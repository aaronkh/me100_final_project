namespace me100_kinect {
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System;
    using System.Windows.Input;
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using System.Diagnostics;

    using Microsoft.Speech.AudioFormat;
    using Microsoft.Speech.Recognition;
    using System.Windows.Documents;
    using System.Text;

    public partial class MainWindow : Window {
        private KinectSensor sensor;

        private DrawingGroup drawingGroup;
        private DrawingImage imageSource;
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;

        private int currentMode = -1; // Start at -1 as 'loading'
        private KinectController[] controllers;
        private List<DeviceLocation> deviceLocations = new List<DeviceLocation>();

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
                    controller.deviceLocations = deviceLocations;
                    controller.initialize();
                    controller.setRenderer(this.drawingGroup, RenderHeight, RenderWidth);
                }
                cycleModes();

                // Start the sensor!
                try {
                    this.sensor.Start();
                    initSpeechRecognizer();
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
            if (this.sensor != null) {
                this.sensor.AudioSource.Stop();
                this.sensor.Stop();
                this.sensor = null;
            }
            for (int i = 0; i < this.controllers.Length; ++i)
                this.controllers[i].blocked = true;
            if (this.speechEngine != null) {
                this.speechEngine.SpeechRecognized -= speechRecognized;
                this.speechEngine.RecognizeAsyncStop();
            }
        }

        private void onKeyPress(object _, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Escape:
                    Close();
                    return;
                case Key.Space:
                    // Main action
                    for (int i = 0; i < this.controllers.Length; ++i) 
                        this.controllers[i].performAction("switch");
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

        /* * * * * * * * * *
         *                 *
         * SPEECH METHODS  *
         *                 *
         * * * * * * * * * */
        // "In a full-fledged application, the SpeechRecognitionEngine object should be properly disposed. For the sake of simplicity, we're omitting that code in this sample."
        private SpeechRecognitionEngine speechEngine;
        private static RecognizerInfo getKinectRecognizer() {
            foreach (RecognizerInfo recognizer in SpeechRecognitionEngine.InstalledRecognizers()) {
                string value;
                recognizer.AdditionalInfo.TryGetValue("Kinect", out value);
                if ("True".Equals(value, StringComparison.OrdinalIgnoreCase) && "en-US".Equals(recognizer.Culture.Name, StringComparison.OrdinalIgnoreCase)) {
                    return recognizer;
                }
            }

            return null;
        }

        private string[] words = { 
            "on", 
            "off",
            "switch",
            "stop",
            "mode",
            "capture",
            "save",
            "insert",
            "clear"
        };

        private void speechRecognized(object sender, SpeechRecognizedEventArgs e) {
            // Speech utterance confidence below which we treat speech as if it hadn't been heard
            const double ConfidenceThreshold = 0.3;

            if (e.Result.Confidence >= ConfidenceThreshold) {
                string word = e.Result.Semantics.Value.ToString().ToLower();
                Trace.WriteLine("word found: "+word);
                switch (word) {
                    case "stop":
                        // Close();
                        return;
                    case "mode":
                        cycleModes();
                        return;
                    case "capture":
                        this.controllers[currentMode].saveImage("out.jpg");
                        return;
                    default:
                        this.controllers[currentMode].performAction(word);
                        return;
                }
            }
        }
        private void initSpeechRecognizer() {
            RecognizerInfo ri = getKinectRecognizer();
            Trace.Write("Kinect recognizer OK: ");
            Trace.WriteLine(ri != null);
            if (ri != null) {
                this.speechEngine = new SpeechRecognitionEngine(ri.Id);

                var choices = new Choices();
                foreach (string s in words) {
                    choices.Add(new SemanticResultValue(s, s.ToUpper()));
                }
                
                GrammarBuilder gb = new GrammarBuilder { Culture = ri.Culture };
                gb.Append(choices);
                speechEngine.LoadGrammar(new Grammar(gb));


                speechEngine.SpeechRecognized += speechRecognized;

                // For long recognition sessions (a few hours or more), it may be beneficial to turn off adaptation of the acoustic model. 
                // This will prevent recognition accuracy from degrading over time.
                speechEngine.UpdateRecognizerSetting("AdaptationOn", 0);

                // We also recommend, for long recognition sessions, that the SpeechRecognitionEngine be recycled 
                // (destroyed and recreated) periodically, say every 2 minutes based on your resource constraints.
                speechEngine.SetInputToAudioStream(
                    sensor.AudioSource.Start(), new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
                speechEngine.RecognizeAsync(RecognizeMode.Multiple);
            }
        }
    }
}
