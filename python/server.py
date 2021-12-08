#!/usr/bin/env python
from flask import Flask, request
app = Flask(__name__)

# app.config['MAX_CONTENT_LENGTH'] = 1024 * 1024 * 3 # 3MB max size

from CircleDetector import CircleDetector

detector = CircleDetector()

def match(img):
    processed = detector.process(img)
    centers = detector.get_centers(processed)
    return centers
    '''
    corr_coeff, loc, mat, r = m
    (startX, startY) = (int(loc[0] * r), int(loc[1] * r))
    (endX, endY) = startX + 200, startY + 50 
    '''

@app.route('/detect', methods=['POST'])
def detect():
    # receive posted image 
    if 'file' not in request.files:
        return 'No file attached.', 400
    file = request.files['file']
    if file.filename == '':
        return 'Missing filename.', 400
    if not file:
        return 'No file attached.', 400
    if  file.filename.rsplit('.',1)[1].lower() not in ['jpeg', 'jpg']:
        return 'Invalid file format.', 400

    matches = match(detector.decode_image(file))
    return matches, 200
