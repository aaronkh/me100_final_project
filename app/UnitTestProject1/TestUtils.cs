using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Kinect;
using me100_kinect;

namespace TestUtils {
    [TestClass]
    public class TestUtils {
        Random rand = new Random();

        [TestMethod]
        public void testExtendLine() { 
            // p1, p2, p3 colinear iff diff(p1, p3) = C diff(p1, p2) 
            // for some constant scalar C
            for (int i = 0; i < 10000; ++i) {
                float x1 = 8*(float)rand.NextDouble();
                float y1 = 8*(float)rand.NextDouble();
                float z1 = 8*(float)rand.NextDouble();
                SkeletonPoint p1 = Utils.createSkeletonPoint(x1, y1, z1);

                float x2 = 8*(float)rand.NextDouble();
                float y2 = 8*(float)rand.NextDouble();
                float z2 = 8*(float)rand.NextDouble();
                SkeletonPoint p2 = Utils.createSkeletonPoint(x2, y2, z2);

                float z3 = 8*(float)rand.NextDouble();
                SkeletonPoint p3 = Utils.extendLine(p1, p2, z3);

                SkeletonPoint p1p3 = Utils.pointDiff(p1, p3);
                SkeletonPoint p1p2 = Utils.pointDiff(p1, p2);

                float C = p1p3.X / p1p2.X;
                Assert.IsTrue(C != 0);
                Assert.IsTrue(Utils.isClose(p1p2.Y * C, p1p3.Y));
                Assert.IsTrue(Utils.isClose(p1p2.Z * C, p1p3.Z));
            }
        }
    }
}
