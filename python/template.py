import cv2 
import numpy as np
import imutils

class Template_Matcher:
    def create_template(self, template_path='template.jpg'):
        template = cv2.imread(template_path)
        
        # Resize to height 100 due to low-res images 
        max_height = 100
        h = max_height
        w = int(h * template.shape[1]/template.shape[0]) 
        template = cv2.GaussianBlur(template, (9, 9), 0)
        template = cv2.resize(template, (w, h))
        template = self.process_image(template)
        return template 

    def process_image(self, mat): 
        mat = cv2.cvtColor(mat, cv2.COLOR_BGR2GRAY)
        mat = cv2.Canny(mat, 150, 200)
        return mat

    def set_template(self, mat):
        self.template = mat

    def scale(self, mat):
        ret = []
        # loop over the scales of the image
        for scale in np.linspace(2, 10, 20)[::-1]:
            # resize the image according to the scale, and keep track
            # of the ratio of the resizing
            resized = imutils.resize(mat, width = int(mat.shape[1] * scale))
            # r = mat.shape[1]/float(resized.shape[1])
            r = scale
            ret.append((resized, r))
        return ret
   
    # Returns a tuple with a corr coeff and location of a match
    def match(self, mat, template):
        result = cv2.matchTemplate(mat, template, cv2.TM_CCORR_NORMED)
        (_, maxVal, _, maxLoc) = cv2.minMaxLoc(result)
        return (maxVal, maxLoc)

