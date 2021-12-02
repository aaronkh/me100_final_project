#!/usr/bin/env python
import freenect
import cv2
import frame_convert2
import numpy as np 

OBJECT_DETECTION = 0
BODY_TRACKING = 1

ctx = freenect.init()
ptr = freenect.open_device(ctx, 0)

# Expect array (x, y, z, state)
objects = []

# High-res depth
# freenect.set_depth_mode(ptr, 2, 2)
# Video doesn't go higher than 640x480 on Kinect 1
# freenect.set_video_mode(ptr, 2, 0)

cv2.namedWindow('Depth')
cv2.namedWindow('Video')

key = -1
block = False # Pause stream/input for compute heavy tasks
depth = None 
video = None
mode = OBJECT_DETECTION

def init():
    global objects 
    global key 
    global block 
    global depth 
    global video 
    global mode
    objects = []
    key = -1 
    block = False 
    depth = None 
    video = None
    mode = OBJECT_DETECTION
    cv2.destroyAllWindows()
    cv2.namedWindow('Depth')
    cv2.namedWindow('Video')

def get_depth():
    return frame_convert2.pretty_depth_cv(freenect.sync_get_depth()[0])

def get_video():
    return frame_convert2.video_cv(freenect.sync_get_video()[0])

def waitKey():
    # importing matplotlib will cause this to segfault on ubuntu
    global key
    if block: return
    k = cv2.waitKey(30) 
    if key == -1: key = k 

def get_depth_async(dev, data, ts):
    global depth  
    if block: return
    depth = frame_convert2.pretty_depth_cv(data)
    cv2.imshow('Depth', data)
    waitKey()

def get_video_async(dev, data, ts):
    global video
    if block: return
    # data = np.repeat(data[:,:,np.newaxis], 3, axis=2)
    video = frame_convert2.video_cv(data)
    cv2.imshow('Video', video)
    waitKey()

def body(*args):
    global key 
    global block

    k = key 
    key = -1
    if k == 27: # esc
        raise freenect.Kill
    elif k == 112: # p
        cv2.imwrite('out.jpg', video)
    elif k == 32: # space 
        # de-facto 'action' key
        if mode == OBJECT_DETECTION: 
            block = True
            # Performs template matching
            block = False 
            return 
        elif mode == BODY_TRACKING: 
            # Toggles closest item to be on/off
            return 
    elif k == 82: # r 
        return init()

def main():
    freenect.runloop(
            depth=get_depth_async,
            video=get_video_async, 
            body=body, 
            dev=ptr
        )

if __name__ == '__main__': 
    main()
