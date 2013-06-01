using System;
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
        private Molecular theWorld;

        [SetUp]
        public void BuildHempCantilever()
        {
            int num_x, num_y;
            double width, height, force;
            num_x = num_y = 1;
            width = height = force = 1;

            theWorld = new Molecular(num_x * num_y);
            for (int count_x = 0; count_x < num_x; count_x++)
            {
                for (int count_y = 0; count_y < num_y; count_y++)
                {
                    theWorld.NewVertex(count_x * width / (num_x - 1), count_y * height / (num_y - 1), 0.0);
                }
            }
            //theWorld.MakeVertexIndex();

            Molecular.Constraint aFixity = theWorld.NewConstraint(true, true, true);
            theWorld.listVertexes[(num_y - 1) / 3].Fixity = aFixity;
            theWorld.listVertexes[2 * (num_y - 1) / 3].Fixity = aFixity;
            //for (int count = (num_y-1) / 3; count <= 2 * (num_y-1) / 3; count++)
            //{
            //    theWorld.listVertexes[count].Fixity = aFixity;
            //}

            theWorld.listVertexes[(num_x - 1) * num_y + (num_y - 1) / 2].Force = new Vector3d(0.0, -force, 0.0);

            //MakeGroundStructure(which_neighbours, num_x, num_y);

            Vertex sVertex, eVertex;

            for (int start = 0; start < num_x * num_y; start++)
            {
                for (int end = start + 1; end < num_x * num_y; end++)
                {
                    sVertex = theWorld.listVertexes[start];
                    eVertex = theWorld.listVertexes[end];

                    theWorld.NewEdge(sVertex, eVertex);
                }
            }

            //for (int x_count = 0; x_count < num_x; x_count++)
            //{
            //    for (int y_count = 0; y_count < num_y; y_count++)
            //    {
            //        if (x_count < num_x - 1)
            //        {
            //            //  Add -
            //            sVertex = theWorld.listVertexes[x_count * num_y + y_count];
            //            eVertex = theWorld.listVertexes[(x_count + 1) * num_y + y_count];
            //            theWorld.NewEdge(sVertex, eVertex);
            //        }
            //        if (y_count < num_y - 1)
            //        {
            //            //  Add |
            //            sVertex = theWorld.listVertexes[x_count * num_y + y_count];
            //            eVertex = theWorld.listVertexes[x_count * num_y + y_count + 1];
            //            theWorld.NewEdge(sVertex, eVertex);
            //        }
            //        if ((x_count < num_x - 1) && (y_count < num_y - 1))
            //        {
            //            //  Add /
            //            sVertex = theWorld.listVertexes[x_count * num_y + y_count];
            //            eVertex = theWorld.listVertexes[(x_count + 1) * num_y + y_count + 1];
            //            theWorld.NewEdge(sVertex, eVertex);
            //            //  Add \
            //            sVertex = theWorld.listVertexes[(x_count + 1) * num_y + y_count];
            //            eVertex = theWorld.listVertexes[x_count * num_y + y_count + 1];
            //            theWorld.NewEdge(sVertex, eVertex);
            //        }
            //    }
            //}
        }

        [Test]
        public void CanSetProblem()
        {
            Assert.IsTrue(TopOpt.SetWorld(theWorld, 1, 1, 0));
        }

        [Test]
        public void CanSolve()
        {
            int code;
            var result = TopOpt.SolveProblem(out code);
            Console.WriteLine(code.ToString());
            Assert.IsTrue(result);
        }

        [Test]
        public void CorrectSolution()
        {
            Console.WriteLine(string.Format("Volume: {0}, Time: {1}", TopOpt.Volume, TopOpt.RunTime));
            Assert.AreEqual(5.0, TopOpt.Volume);
        }
    }
}
