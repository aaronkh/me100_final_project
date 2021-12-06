#!/usr/bin/env python
from flask import Flask, request
import json
import os
app = Flask(__name__)

app.config['MAX_CONTENT_LENGTH'] = 1024 * 1024 * 3 # 3MB max size

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
        print(f'{round(r, 2)}:', corr_coeff)
        res.append(({'corr_coeff': corr_coeff, 'loc': loc, 'scale': r}))
    m = max(res, key=lambda x: x['corr_coeff'])
    return m
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
    path = os.path.join('./', 'out.jpg')
    if os.path.exists(path):
        return 'Matcher busy', 503

    print('File received!')
    file.save(path)
    print('Matching...')
    matches = match(matcher.open_image(path))
    res = json.dumps(matches)
    if os.path.exists(path):
        os.remove(path)
    print('Matches found, cleaning up.')
    return res, 200

