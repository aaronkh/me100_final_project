# ME100 Fa21 Final Project 

Controlling lights via Kinect... just by pointing 

## How to run?
1. Grab a release build from Github. The `.exe` file is built for any computer running Windows 8+ and Kinect v1 connected via a specific [USB adapter](https://www.amazon.com/Microsoft-OEM-Kinect-Adapter-Windows/dp/B00NMSHT7E).

2. Start the Python server on the same computer (`localhost`). Within the `python` directory, install `requirements.txt` with Pip. Make sure to start on port 2021 as this is hard-coded into the Kinect code. For computers running a Linux shell i.e. WSL, simply run `./start.sh`.

3. Launch the Kinect app. If the sensor is connected successfully, you should see a video feed. Within the app there are some controls to use: 
    * <kbd>Space</kbd> performs the primary function of a mode. For object detection, this will save any currently detected devices globally. For body tracking, this will send a signal to the ESP32 server with information about the nearest detected object as determined by the captured arm joints from the Kinect
    * <kbd>M</kbd> switches between body tracking and object detection modes
    * <kbd>P</kbd> saves a snapshot of the video feed to `out.jpg`
    * <kbd>ESC</kbd> closes the app

    Alternatively, there are also several voice commands built in:
    * `ON`, `OFF`, `SWITCH` turns the state of the currently highlighted (magenta) object on, off, or the opposite of the current state respectively. In object detection mode, this does nothing
    * `INSERT`, `SAVE`, `CLEAR` adds the currently highlighted object, overwrites all detected objects, and clears all detected devices respectively. In body tracking mode, this does nothing
    * `MODE` switches between body tracking and object detection modes 
    * `CAPTURE` saves a snapshot of the video feed to `out.jpg`
    * `STOP` closes the app

## How to build?
### Prerequisites 
* Visual Studio 
* Python 3
* Arduino IDE
* [Kinect SDK and driver implementation](https://docs.microsoft.com/en-us/previous-versions/windows/kinect-1.8/hh855354(v=ieb.10))
1. Follow the Kinect SDK setup instructions linked above. The code in this repo has only been tested for Visual Studio Express 2012, so some features may not work.
2. Download and compile the Arduino code. You may need to change your SSID and password depending on your network security settings. 
3. In `HttpClientWrapper.cs`, change `ESP32_IP` to point to the IP as printed by the Arduino code to the serial monitor.
4. Run the built Kinect app. 

## Implementation
### TODO
