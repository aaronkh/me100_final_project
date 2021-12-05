import cv2 
from template import Template_Matcher 
t = Template_Matcher() 

template = t.create_template() 
th, tw  = template.shape[:2]
cv2.imwrite('o.jpg', template) 

img = cv2.imread('out.jpg')
processed_img = t.process_image(img) 
cv2.imwrite('processed.jpg', processed_img)
scaled = t.scale(processed_img)
res = []
for mat, r in scaled: 
    corr_coeff, loc = (t.match(mat, template))
    print(corr_coeff)
    res.append((corr_coeff, loc, mat, r))
m = max(res, key=lambda x: x[0])
corr_coeff, loc, mat, r = m
(startX, startY) = (int(loc[0] * r), int(loc[1] * r))
(endX, endY) = startX + 200, startY + 50 

# mat = cv2.rectangle(mat, loc, (endX, endY), (255, 0, 255), 2)
mat = cv2.circle(mat, loc, 150, (255, 0, 255), 20)
cv2.imwrite(str(r)+'.jpg', mat)

'''
for m in res:
    corr_coeff, loc, mat, r = m
    (startX, startY) = (int(loc[0] * r), int(loc[1] * r))
    (endX, endY) = startX + 200, startY + 50 

    # mat = cv2.rectangle(mat, loc, (endX, endY), (255, 0, 255), 2)
    mat = cv2.circle(mat, loc, 150, (255, 0, 255), 20)
    cv2.imwrite(str(r)+'.jpg', mat)
'''
