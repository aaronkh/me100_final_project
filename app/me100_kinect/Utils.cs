using Microsoft.Kinect;

namespace me100_kinect {
    public static class Utils {
        public static float getDistance(SkeletonPoint p1, SkeletonPoint p2) {
            SkeletonPoint diff = pointDiff(p1, p2);
            float sum = diff.X*diff.X + diff.Y*diff.Y + diff.Z*diff.Z;
            return (float) System.Math.Sqrt(sum);
        }

        // Extends a line defined by p1 and p2 to a specified z distance
        public static SkeletonPoint extendLine(SkeletonPoint p1, SkeletonPoint p2, float distance) {
            // https://www.geeksforgeeks.org/equation-of-a-line-in-3d/
            SkeletonPoint diff = pointDiff(p2, p1);
            float l = diff.X; 
            float m = diff.Y; 
            float n = diff.Z;

            float target = (distance - p1.Z) / n;
            float x3 = target * l + p1.X;
            float y3 = target * m + p1.Y;
            
            return createSkeletonPoint(x3, y3, distance);
        }

        public static SkeletonPoint createSkeletonPoint(float X, float Y, float Z) {
            SkeletonPoint ret = new SkeletonPoint();
            ret.X = X;
            ret.Y = Y;
            ret.Z = Z;

            return ret;
        }

        public static SkeletonPoint pointDiff(SkeletonPoint p1, SkeletonPoint p2) {
            SkeletonPoint ret = new SkeletonPoint();
            ret.X = p1.X - p2.X;
            ret.Y = p1.Y - p2.Y;
            ret.Z = p1.Z - p2.Z;

            return ret;
        }

        // Account for floating point error
        public static bool isClose(float f1, float f2) {
            return System.Math.Abs(f1 - f2) < 0.01;
        }
    }
}
