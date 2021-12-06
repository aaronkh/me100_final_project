#!/usr/bin/env python
from flask import Flask
app = Flask(__name__)

from template import Template_Matcher

matcher = Template_Matcher()

template = matcher.create_template() 
th, tw  = template.shape[:2]

def match(img):
    processed = matcher.process_image(img)
    scaled = matcher.scale(processed)
    res = []
    for mat, r in scaled: 
        corr_coeff, loc = (matcher.match(mat, template))
        print(corr_coeff)
        res.append((corr_coeff, loc, mat, r))
    m = max(res, key=lambda x: x[0])
    return m
    '''
    corr_coeff, loc, mat, r = m
    (startX, startY) = (int(loc[0] * r), int(loc[1] * r))
    (endX, endY) = startX + 200, startY + 50 
    '''

@app.route('/detect', methods=['POST'])
def detect():
    return ''

