using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if (!TEST)
using Rhino.Geometry;
#else
using System.Windows.Media.Media3D;
using Vector3d = System.Windows.Media.Media3D.Vector3D;
using Point3d = System.Windows.Media.Media3D.Point3D;
#endif

namespace Buckminster.Types
{
    /// <summary>
    /// Simple data structure for bars and nodes.
    /// Build to interface with PS TopOpt code. Needs refining...
    /// </summary>
    public class Molecular
    {
        public List<Node> listVertexes;
        public List<Bar> listEdges;
        //public List<Node> listOuterVertexes;
        //public List<Node> listInnerVertexes;

        public Molecular(int vcount)
        {
            listVertexes = new List<Node>(vcount);
            listEdges = new List<Bar>(Convert.ToInt32(vcount * (vcount - 1) * 0.5));
            //listOuterVertexes = new List<Node>();
            //listInnerVertexes = new List<Node>();
        }

        public Bar NewEdge(Node start, Node end)
        {
            var edge = new Bar { StartVertex = start, EndVertex = end , Index = listEdges.Count };
            listEdges.Add(edge);
            start.listEdgesStarting.Add(edge);
            end.listEdgesEnding.Add(edge);
            return edge;
        }

        public Node NewVertex(double x, double y, double z)
        {
            return NewVertex(new Point3d(x, y, z));
        }

        public Node NewVertex(Point3d point)
        {
            var vertex = new Node { Coord = point, Index = listVertexes.Count };
            listVertexes.Add(vertex);
            return vertex;
        }

        public void DeleteElements(IEnumerable<Bar> listEdgesToRemove)
        {
            foreach (var edge in listEdgesToRemove)
            {
                edge.StartVertex.listEdgesStarting.Remove(edge);
                edge.EndVertex.listEdgesEnding.Remove(edge);
                listEdges.Remove(edge);
            }
            for (int i = 0; i < listEdges.Count; i++)
			{
			    listEdges[i].Index = i;
			}
        }

        public Bar FindEdge(Node node1, Node node2)
        {
            foreach (var edge in listEdges)
            {
                if (edge.StartVertex == node1 && edge.EndVertex == node2) return edge;
                if (edge.StartVertex == node2 && edge.EndVertex == node1) return edge;
            }
            return null;
        }

        public Constraint NewConstraint(bool x, bool y, bool z)
        {
            return new Constraint(x, y, z);
        }

        public class Node
        {
            public Node()
            {
                listEdgesStarting = new List<Bar>();
                listEdgesEnding = new List<Bar>();
            }
            public int Index { get; set; }
            public int Number { get { return Index; } }
            public Point3d Coord { get; set; }
            public List<Bar> listEdgesStarting { get; set; }
            public List<Bar> listEdgesEnding { get; set; }
            public Vector3d Force { get; set; }
            public Constraint Fixity { get; set; }
            public Vector3d Velocity { get; set; }
            public override string ToString()
            {
                return base.ToString() + Coord.ToString();
            }
        }

        public class Bar
        {
            public int Index { get; set; }
            public int Number { get { return Index; } }
            public Node StartVertex { get; set; }
            public Node EndVertex { get; set; }
            public double Radius { get; set; }
            public System.Drawing.Color Colour { get; set; }
            public double Length { get { return (EndVertex.Coord - StartVertex.Coord).Length; } }
            public override string ToString()
            {
                return base.ToString() + string.Format("from {0} to {1}", StartVertex, EndVertex);
            }
        }

        public class Constraint
        {
            public bool X { get; set; }
            public bool Y { get; set; }
            public bool Z { get; set; }

            public Constraint(bool x, bool y, bool z)
            {
                X = x;
                Y = y;
                Z = z;
            }
            //public Constraint(Tuple<bool, bool, bool> triple)
            //    : this(triple.Item1, triple.Item2, triple.Item3)
            //{
            //}
            public Constraint(Vector3d vector)
                : this(vector.X > 0, vector.Y > 0, vector.Z > 0)
            {
            }
            //public Constraint(System.Object obj)
            //{
            //    if (obj is Tuple<bool, bool, bool>)
            //    {
            //        var triple = (Tuple<bool, bool, bool>)obj;
            //        X = triple.Item1;
            //        Y = triple.Item2;
            //        Z = triple.Item3;
            //    }
            //    else if (obj is Vector3d || obj is Vector3f)
            //    {
            //        var vector = (Vector3d)obj;
            //        X = vector.X > 0;
            //        Y = vector.Y > 0;
            //        Z = vector.Z > 0;
            //    }
            //    else throw new ArgumentException();
            //}
        }
    }
}
