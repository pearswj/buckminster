using System;
using System.Linq;
using NUnit.Framework;
using Buckminster;
using Buckminster.Types;
using Vertex = Buckminster.Types.Molecular.Node;
using Edge = Buckminster.Types.Molecular.Bar;
using Rhino.Geometry;

namespace Buckminster.Tests
{
    [TestFixture]
    public class TopOptTests
    {
        private Molecular fully_connected, adjacents_connected, dead_simple;
        //private double TargetVolume = 4.4177951388; // Matlab (to 10 d.p.)
        private double TargetVolume = 4.4177951461618399; // Matlab (Mosek)

        [TestFixtureSetUp]
        public void BuildHempCantilevers()
        {
            int num_x, num_y;
            double width, height, force;
            num_x = num_y = 13;
            width = height = force = 1;

            fully_connected = new Molecular(num_x * num_y);
            adjacents_connected = new Molecular(num_x * num_y);
            for (int count_x = 0; count_x < num_x; count_x++)
            {
                for (int count_y = 0; count_y < num_y; count_y++)
                {
                    fully_connected.NewVertex(count_x * width / (num_x - 1), count_y * height / (num_y - 1), 0.0);
                    adjacents_connected.NewVertex(count_x * width / (num_x - 1), count_y * height / (num_y - 1), 0.0);
                }
            }
            //theWorld.MakeVertexIndex();

            Molecular.Constraint aFixity = fully_connected.NewConstraint(true, true, true);
            fully_connected.listVertexes[(num_y - 1) / 3].Fixity = aFixity;
            fully_connected.listVertexes[2 * (num_y - 1) / 3].Fixity = aFixity;
            adjacents_connected.listVertexes[(num_y - 1) / 3].Fixity = aFixity;
            adjacents_connected.listVertexes[2 * (num_y - 1) / 3].Fixity = aFixity;
            //for (int count = (num_y - 1) / 3; count <= 2 * (num_y - 1) / 3; count++)
            //    adjacents_connected.listVertexes[count].Fixity = aFixity;

            fully_connected.listVertexes[(num_x - 1) * num_y + (num_y - 1) / 2].Force = new Vector3d(0.0, -force, 0.0);
            adjacents_connected.listVertexes[(num_x - 1) * num_y + (num_y - 1) / 2].Force = new Vector3d(0.0, -force, 0.0);

            // Fully-Connected
            Vertex sVertex, eVertex;

            for (int start = 0; start < num_x * num_y; start++)
            {
                for (int end = start + 1; end < num_x * num_y; end++)
                {
                    sVertex = fully_connected.listVertexes[start];
                    eVertex = fully_connected.listVertexes[end];

                    fully_connected.NewEdge(sVertex, eVertex);
                }
            }

            
            // Adjacents-Connected
            for (int x_count = 0; x_count < num_x; x_count++)
            {
                for (int y_count = 0; y_count < num_y; y_count++)
                {
                    if (x_count < num_x - 1)
                    {
                        //  Add -
                        sVertex = adjacents_connected.listVertexes[x_count * num_y + y_count];
                        eVertex = adjacents_connected.listVertexes[(x_count + 1) * num_y + y_count];
                        adjacents_connected.NewEdge(sVertex, eVertex);
                    }
                    if (y_count < num_y - 1)
                    {
                        //  Add |
                        sVertex = adjacents_connected.listVertexes[x_count * num_y + y_count];
                        eVertex = adjacents_connected.listVertexes[x_count * num_y + y_count + 1];
                        adjacents_connected.NewEdge(sVertex, eVertex);
                    }
                    if ((x_count < num_x - 1) && (y_count < num_y - 1))
                    {
                        //  Add /
                        sVertex = adjacents_connected.listVertexes[x_count * num_y + y_count];
                        eVertex = adjacents_connected.listVertexes[(x_count + 1) * num_y + y_count + 1];
                        adjacents_connected.NewEdge(sVertex, eVertex);
                        //  Add \
                        sVertex = adjacents_connected.listVertexes[(x_count + 1) * num_y + y_count];
                        eVertex = adjacents_connected.listVertexes[x_count * num_y + y_count + 1];
                        adjacents_connected.NewEdge(sVertex, eVertex);
                    }
                }
            }
            Vertex aVertex;
            dead_simple = new Molecular(4);
            aVertex = dead_simple.NewVertex(new Point3d(0, 0, 0));
            aVertex.Fixity = dead_simple.NewConstraint(true, true, true);
            aVertex = dead_simple.NewVertex(new Point3d(0, 1, 0));
            aVertex.Fixity = dead_simple.NewConstraint(true, true, true);
            dead_simple.NewVertex(new Point3d(1, 0, 0));
            aVertex = dead_simple.NewVertex(new Point3d(1, 1, 0));
            aVertex.Force = new Vector3d(0, -0.5, 0);
            for (int start = 0; start < 4; start++)
            {
                for (int end = start + 1; end < 4; end++)
                {
                    sVertex = dead_simple.listVertexes[start];
                    eVertex = dead_simple.listVertexes[end];
                    dead_simple.NewEdge(sVertex, eVertex);
                }
            }

        }

        [Test]
        public void CanSolveHempGoogle()
        {
            TopOpt.SetProblem(fully_connected, 1, 1, 0);
            Assert.IsTrue(TopOpt.SolveProblemGoogle()); // Google Or-Tools
            Assert.AreEqual(TargetVolume, TopOpt.Volume, 1E-8); // Correct solution?
            Console.WriteLine("Volume {0,14:F12}", TopOpt.Volume);
        }

        [Test]
        public void CanSolveHempMosek()
        {
            TopOpt.SetProblem(fully_connected, 1, 1, 0);
            Assert.IsTrue(TopOpt.SolveProblemMosek()); // Mosek
            Assert.AreEqual(TargetVolume, TopOpt.Volume, 1E-8); // Correct solution?
            Console.WriteLine("Volume {0,14:F12}", TopOpt.Volume);
        }

        [Test]
        public void CanSolveHempMosekCross()
        {
            TopOpt.SetProblem(adjacents_connected, 1, 1, 0);
            Assert.IsTrue(TopOpt.SolveProblemMosek()); // Mosek
            Assert.AreEqual(5.0000000002442135, TopOpt.Volume, 1E-8); // Correct solution?
            Console.WriteLine("Volume {0,14:F12}", TopOpt.Volume);
        }
    }
}
