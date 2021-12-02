import numpy as np 

def get_distance(p1, p2):
    return np.sqrt(np.sum(np.pow(p1-p2), 2))

# Extends a line d efined by p1 and p2 to a specified z distance
def extend_line(p1, p2, distance):
    # https://www.geeksforgeeks.org/equation-of-a-line-in-3d/
    x1, y1, z1 = p1 
    x2, y2, z2 = p2 
    l, m, n = p2 - p1 
    target = (distance - z1)/n
    x3 = target*l+x1 
    y3 = target*m+y1
    return np.array([x3, y3, distance])

