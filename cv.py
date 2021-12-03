#!/usr/bin/env python
import freenect
import cv2
import frame_convert2
import numpy as np 

from template import Template_Matcher

OBJECT_DETECTION = 0
BODY_TRACKING = 1

# High-res depth
# freenect.set_depth_mode(ptr, 2, 2)
# Video doesn't go higher than 640x480 on Kinect 1
# freenect.set_video_mode(ptr, 2, 0)

def init():
    global objects 
    global key 
    # Pause stream/input for compute heavy tasks
    global block 
    global depth 
    global video 
    global mode
    global template 
    global dims
    global matcher
    global pipeline 

    # Expect array (x, y, z, state)
    objects = []
    key = -1 
    block = False 
    depth = None 
    video = None
    mode = OBJECT_DETECTION
    matcher = Template_Matcher()
    template = matcher.create_template()
    dims = template.shape[:2]

    pipeline = {
        'Video': {
            'preprocess': lambda x: x,
            'postprocess': lambda x: x
        }, 
        'Depth': {
            'preprocess': lambda x: x,
            'postprocess': lambda x: x
        }
    }

    cv2.destroyAllWindows()
    cv2.namedWindow('Depth')
    cv2.namedWindow('Video')

def display(mode='Video'):
    if mode == 'Video': 
        img = video
    else: 
        img = depth
    img = pipeline[mode]['preprocess'](img)
    cv2.imshow(mode, img)        
    waitKey()
    img = pipeline[mode]['postprocess'](img)

def display_block():
    windowWidth=cv2.getWindowImageRect("Video")[2]
    windowHeight=cv2.getWindowImageRect("Video")[3] 
    print(windowHeight, windowWidth)
    mat = np.zeros((windowHeight, windowWidth, 3))
    loc = (windowHeight//2, windowWidth//2)
    mat = cv2.putText(mat, 'Loading...', (50, 50), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2, cv2.LINE_AA)
    for n in pipeline.keys():
        cv2.imshow(n, mat)
    cv2.waitKey(1)

def waitKey():
    # importing matplotlib will cause this to segfault on ubuntu
    global key
    if block: return
    k = cv2.waitKey(30) 
    if key == -1: key = k 

def get_depth_async(dev, data, ts):
    global depth  
    if block: return display_block()
    depth = frame_convert2.pretty_depth_cv(data)
    display('Depth')

def get_video_async(dev, data, ts):
    global video
    if block: return display_block()
    video = frame_convert2.video_cv(data)
    display('Video')

def body(*args):
    global key 
    global block
    global depth
    global mode 

    k = key 
    key = -1
    if k == -1: return 

    print(k)
    if k == 27: # esc
        raise freenect.Kill
    elif k == 112: # p
        cv2.imwrite('out.jpg', video)
    elif k == 77: # m
        if mode == OBJECT_DETECTION: 
            mode = BODY_TRACKING 
        elif mode == BODY_TRACKING: 
            mode = OBJECT_DETECTION
    elif k == 32: # space 
        # de-facto 'action' key
        if mode == OBJECT_DETECTION: 
            block = True
            display_block()
            img = matcher.process_image(video)
            scaled = matcher.scale(img)
            res = []
            for mat, r in scaled: 
                corr_coeff, loc = matcher.match(mat, template)
                print(corr_coeff, loc)
                res.append((corr_coeff, loc, mat, r))
            m = max(res, key=lambda x: x[0]) 
            corr_coeff, loc, mat, r = m
            loc = tuple([int(v/r) for i, v in enumerate(loc)])
            print('Found!', loc)
            print(np.unique(depth, return_counts=1))
            print('Depth:', depth[loc[0]][loc[1]])
            pipeline['Video']['preprocess'] = \
                lambda x: cv2.circle(x.astype(np.uint8).copy(), loc, 3, (255, 0, 255), -1)
            pipeline['Depth']['preprocess'] = \
                lambda x: cv2.circle(x.astype(np.uint8).copy(), loc, 3, (255, 0, 255), -1)
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
        )

if __name__ == '__main__': 
    init()
    main()
