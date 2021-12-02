#!/usr/bin/env python
import freenect
import cv2
import frame_convert2
import numpy as np 
import time 

ctx = freenect.init()
ptr = freenect.open_device(ctx, 0)

# High-res depth
# freenect.set_depth_mode(ptr, 2, 2)
action = None
# Video camera doesn't go higher than 640x480
# freenect.set_video_mode(ptr, 2, 0)


# cv2.namedWindow('Depth')
cv2.namedWindow('Video')

depth = None 
video = None

def get_depth():
    return frame_convert2.pretty_depth_cv(freenect.sync_get_depth()[0])

def get_video():
    return frame_convert2.video_cv(freenect.sync_get_video()[0])

def get_depth_async(dev, data, ts):
    global depth  
    depth = data

def get_video_async(dev, data, ts):
    global action 
    global video
    # data = np.repeat(data[:,:,np.newaxis], 3, axis=2)
    video = frame_convert2.video_cv(data)
    cv2.imshow('Video', video)
    # importing matplotlib will cause this to segfault on ubuntu
    k = cv2.waitKey(30) 
    if k == 27: # esc 
        action = 'kill server'
    elif k == 112: # p
        action = 'write video'

def body(*args):
    global action 
    if action == 'kill server': 
        raise freenect.Kill
    elif action == 'write video': 
        cv2.imwrite('out.jpg', video)
    action = None 

def main():
    freenect.runloop(
            depth=get_depth_async,
            video=get_video_async, 
            body=body, 
            dev=ptr
        )

if __name__ == '__main__': 
    main()
