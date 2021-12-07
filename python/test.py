import cv2 
import numpy as np 

def preprocess(mat):
    green = mat.copy()
    green[:,:,0] = 0
    green[:,:,2] = 0
    return green

cv2.namedWindow('test')

cam = cv2.VideoCapture(0)
while True:
    ret_val, img = cam.read()
    if mirror:
        img = cv2.flip(img, 1)
    img = preprocess(img)
    cv2.imshow('test', img)
    if cv2.waitKey(1) == 27:
        break  # esc to quit
cv2.destroyAllWindows()
