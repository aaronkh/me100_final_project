namespace me100_kinect {
    static class Utils {
        public static float getDistance(Point3D p1, Point3D p2) {
            Point3D diff = p1.pointDiff(p2);
            float sum = diff.X*diff.X + diff.Y*diff.Y + diff.Z*diff.Z;
            return (float) System.Math.Sqrt(sum);
        }

        // Extends a line defined by p1 and p2 to a specified z distance
        public static Point3D extendLine(Point3D p1, Point3D p2, float distance) {
            // https://www.geeksforgeeks.org/equation-of-a-line-in-3d/
            Point3D diff = p2.pointDiff(p1);
            float l = diff.X; 
            float m = diff.Y; 
            float n = diff.Z;

            float target = (distance - p1.Z) / n;
            float x3 = target * l + p1.X;
            float y3 = target * m + p1.Y;
            return new Point3D(x3, y3, distance);
        }
            
        // ...because the native point is only in 2D space
        public struct Point3D { 
            public float X, Y, Z;
            public Point3D(float x, float y, float z) {
                X = x;
                Y = y;
                Z = z;
            }
            
            public override string ToString() {
                return X + " " + Y + " " + Z;
            }

            public Point3D pointDiff(Point3D p2) {
                return new Point3D(X - p2.X, Y - p2.Y, Z - p2.Z);     
            }
        }
    }
}
