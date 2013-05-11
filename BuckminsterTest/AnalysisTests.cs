using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Windows.Media.Media3D;
using Buckminster;

namespace BuckminsterTest
{
    [TestClass]
    public class AnalysisTests
    {
        [TestMethod]
        public void TestSimpleStiffnessMethod()
        {
            // Set-up model (see Pinned2D - Nodes_X.txt and Lines_X.txt)

            // nodes
            var nodes_temp = new double[,]{
                {0.0, 0.0},
                {1.0, 0.0},
                {1.0, 1.0},
                {0.0, 1.0}
            };
            int n = nodes_temp.GetLength(0);
            Point3D[] nodes = new Point3D[n];
            for (int i = 0; i < n; i++)
            {
                nodes[i] = new Point3D(nodes_temp[i, 0], nodes_temp[i, 1], 0.0);
            }

            // bars
            var bars_s = new int[] { 0, 1, 2, 3, 0, 1 };
            var bars_e = new int[] { 1, 2, 3, 0, 2, 3 };
            var bars_mat = new double[] { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            // loads
            var loads_temp = new double[,] {
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, 0.0},
                {20000.0, 0.0}
            };
            Vector3D[] loads = new Vector3D[n];
            for (int i = 0; i < n; i++)
            {
                loads[i] = new Vector3D(loads_temp[i, 0], loads_temp[i, 1], 0.0);
            }

            // supports
            var supports_temp = new double[,] {
                {1, 1},
                {0, 1},
                {0, 0},
                {0, 0}
            };
            Vector3D[] supports = new Vector3D[supports_temp.GetLength(0)];
            for (int i = 0; i < supports.Length; i++)
            {
                supports[i] = new Vector3D(supports_temp[i, 0], supports_temp[i, 1], 0.0);
            }

            // Call the method

            Vector3D[] displacements;
            Buckminster.Analysis.StiffnessMethod(nodes, bars_s, bars_e, bars_mat, loads, supports, out displacements);

            var target_displacements = new double[] {
                0.0, 0.0,
                0.04878051,0.0,
                0.18675262,-0.048780516,
                0.23553312,0.048780512
            };

            var results = new double[n * 2];
            for (int i = 0; i < n; i++)
            {
                results[i * 2] = displacements[i].X;
                results[i * 2 + 1] = displacements[i].Y;
            }

            // compare results (to a tolerance)
            
            for (int i = 0; i < n * 2; i++)
            {
                Assert.AreEqual(target_displacements[i], results[i], 1E-06);
            }
        }

        [TestMethod]
        public void TestSimpleStiffnessMethod2()
        {
            // Set-up model (see Pinned2D - Nodes.txt and Lines.txt)

            // nodes
            var nodes_temp = new double[,]{
                {0, 0},
                {0, 1},
                {1, 0},
                {1, 1},
                {2, 0},
                {2, 1},
                {3, 0},
                {3, 1},
                {4, 0},
                {4, 1},
                {5, 0},
                {5, 1},
                {6, 0},
                {6, 1},
            };
            int n = nodes_temp.GetLength(0);
            Point3D[] nodes = new Point3D[n];
            for (int i = 0; i < n; i++)
            {
                nodes[i] = new Point3D(nodes_temp[i, 0], nodes_temp[i, 1], 0.0);
            }

            // bars
            var bars_s = new int[] { 0, 2, 4, 6,  8, 10, 1, 3, 5, 7,  9, 11, 0, 2, 4, 6,  8, 10, 12, 0, 2, 4, 6,  8, 10 };
            var bars_e = new int[] { 2, 4, 6, 8, 10, 12, 3, 5, 7, 9, 11, 13,  1, 3, 5, 7, 9, 11, 13, 3, 5, 7, 9, 11, 13 };
            var bars_mat = new double[] { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 };

            // loads
            var loads_temp = new double[,] {
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, -1000.0},
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, 0.0},
                {0.0, 0.0}
            };
            Vector3D[] loads = new Vector3D[n];
            for (int i = 0; i < n; i++)
            {
                loads[i] = new Vector3D(loads_temp[i, 0], loads_temp[i, 1], 0.0);
            }

            // supports
            var supports_temp = new double[,] {
                {1, 1},
                {0, 0},
                {0, 0},
                {0, 0},
                {0, 0},
                {0, 0},
                {0, 0},
                {0, 0},
                {0, 0},
                {0, 0},
                {0, 0},
                {0, 0},
                {0, 1},
                {0, 0}
            };
            Vector3D[] supports = new Vector3D[supports_temp.GetLength(0)];
            for (int i = 0; i < supports.Length; i++)
            {
                supports[i] = new Vector3D(supports_temp[i, 0], supports_temp[i, 1], 0.0);
            }

            // Call the method

            Vector3D[] displacements;
            Buckminster.Analysis.StiffnessMethod(nodes, bars_s, bars_e, bars_mat, loads, supports, out displacements);

            var target_displacements = new double[] {
                0.0, 0.0,
                0.02276444,0.0,
                0.0024390612,-0.03210214,
                0.02276444,-0.029663099,
                0.007317163,-0.059326164,
                0.020325394,-0.056887124,
                0.014634298,-0.07191591,
                0.0154473055,-0.07435493,
                0.019512393,-0.056074113,
                0.00813018,-0.058513153,
                0.021951437,-0.030476114,
                0.00325209,-0.03291516,
                0.021951437,0.0,
                8.130413E-4,-0.0024390498
            };

            var results = new double[n * 2];
            for (int i = 0; i < n; i++)
            {
                results[i * 2] = displacements[i].X;
                results[i * 2 + 1] = displacements[i].Y;
            }

            // Compare results (to a tolerance)

            for (int i = 0; i < n * 2; i++)
            {
                Assert.AreEqual(target_displacements[i], results[i], 1E-06);
            }
        }
    }
}
