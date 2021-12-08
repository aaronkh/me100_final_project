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

## How to build?
TODO
