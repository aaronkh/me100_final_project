import cv2 
import numpy as np
import imutils 

class CircleDetector:
	def __init__(self):
		pass
	def process(self, img):
		cv2.imwrite('in.jpg', img)
		hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)
		## mask of green (36,25,25) ~ (86, 255,255)
		# mask = cv2.inRange(hsv, (36, 25, 25), (86, 255,255))
		mask = cv2.inRange(hsv, (40, 128, 128), (176/2, 255, 255))

		## slice the green
		imask = mask>0
		green = np.zeros_like(img, np.uint8)
		green[imask] = img[imask]
		# Green is only bright green items isolated 

		image = green 
		
		# https://www.pyimagesearch.com/2016/02/08/opencv-shape-detection/
		resized = imutils.resize(image, width=300)
		ratio = image.shape[0] / float(resized.shape[0])
		gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
		blur = cv2.medianBlur(gray, 5)
		sharpen_kernel = np.array([[-1,-1,-1], [-1,9,-1], [-1,-1,-1]])
		sharpen = cv2.filter2D(blur, -1, sharpen_kernel)
		thresh = cv2.threshold(sharpen,160,255, cv2.THRESH_BINARY_INV)[1]
		kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (3,3))
		close = cv2.morphologyEx(thresh, cv2.MORPH_CLOSE, kernel, iterations=2)
		close = cv2.bitwise_not(gray)
		cv2.imwrite('test.jpg', gray)
		return close 

	def get_centers(self, img):
		cnts = cv2.findContours(img, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
		cnts = imutils.grab_contours(cnts)
		
		ret = []
		for c in cnts:
			# compute the center of the contour, then detect the name of the
			# shape using only the contour
			M = cv2.moments(c)
			area = round(np.sqrt(M['m00']/3.1415))
			ratio = 1.0
			cX = int((M["m10"] / M["m00"]) * ratio)
			cY = int((M["m01"] / M["m00"]) * ratio)
			shape = self.identify(c)
			# multiply the contour (x, y)-coordinates by the resize ratio,
			# then draw the contours and the name of the shape on the image
			# c = c.astype("float")
			# c *= ratio
			# c = c.astype("int")
			# cv2.drawContours(image, [c], -1, (255, 0, 0), 2)
			if shape == 'circle':
				ret.append(f'{cX},{cY},{area}')
		if len(ret) > 100:
			cv2.imwrite('e.jpg', img)
		return '|'.join(ret)


	def open_image(self, path):
		return cv2.imread(path)
	
	def decode_image(self, f): 
		return cv2.imdecode(np.frombuffer(f.read(), np.uint8), cv2.IMREAD_COLOR)
	
	def identify(self, c):
		# initialize the shape name and approximate the contour
		shape = "unidentified"
		peri = cv2.arcLength(c, True)
		approx = cv2.approxPolyDP(c, 0.04 * peri, True)
		# if the shape is a triangle, it will have 3 vertices
		if len(approx) == 3:
			shape = "triangle"
		# if the shape has 4 vertices, it is either a square or
		# a rectangle
		elif len(approx) == 4:
			# compute the bounding box of the contour and use the
			# bounding box to compute the aspect ratio
			(x, y, w, h) = cv2.boundingRect(approx)
			ar = w / float(h)
			# a square will have an aspect ratio that is approximately
			# equal to one, otherwise, the shape is a rectangle
			shape = "square" if ar >= 0.95 and ar <= 1.05 else "rectangle"
		# if the shape is a pentagon, it will have 5 vertices
		elif len(approx) == 5:
			shape = "pentagon"
		# otherwise, we assume the shape is a circle
		else:
			shape = "circle"
		# return the name of the shape
		return shape
