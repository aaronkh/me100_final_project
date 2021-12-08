import cv2 
from CircleDetector import CircleDetector 

d = CircleDetector() 
d.process(cv2.imread('hsv.png'))
