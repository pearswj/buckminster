using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rhino.Geometry;

namespace Buckminster
{
    public class Face
    {
        #region constructors
        public Face(Halfedge edge)
        {
            Halfedge = edge;
            Name = Guid.NewGuid().ToString("N").Substring(0, 8);
        }
        #endregion

        #region properties
        public Halfedge Halfedge { get; set; }
        public String Name { get; private set; }
        public Point3f Centroid
        {
            get
            {
                Point3f avg = new Point3f();
                List<Vertex> vertices = GetVertices();
                foreach (Vertex v in vertices)
                {
                    avg.X += v.Position.X;
                    avg.Y += v.Position.Y;
                    avg.Z += v.Position.Z;
                }
                avg.X /= vertices.Count;
                avg.Y /= vertices.Count;
                avg.Z /= vertices.Count;

                return avg;
            }
        }
        public Vector3f Normal
        {
            get
            {
                Vector3d normal = new Vector3d();
                Halfedge edge = Halfedge;
                do
                {
                    normal += Vector3d.CrossProduct(edge.Vector, edge.Next.Vector);
                    edge = edge.Next; // move on to next halfedge
                } while (edge != Halfedge);
                normal.Unitize();
                return new Vector3f((float)normal.X, (float)normal.Y, (float)normal.Z);
            }
        }
        #endregion

        #region methods
        public List<Vertex> GetVertices()
        {
            List<Vertex> vertices = new List<Vertex>();
            Halfedge edge = Halfedge;
            do
            {
                vertices.Add(edge.Vertex); // add vertex to list
                edge = edge.Next; // move on to next halfedge
            } while (edge != Halfedge);
            return vertices;
        }
        
        /// <summary>
        /// Constructs a close polyline which follows the edges bordering the face
        /// </summary>
        /// <returns>a closed polyline representing the face</returns>
        public Polyline ToClosedPolyline()
        {
            Polyline polyline = new Polyline();
            foreach (Vertex v in GetVertices())
            {
                polyline.Add(v.Position);
            }
            polyline.Add(polyline.First); // close polyline
            return polyline;
        }

        public void Split(Vertex v1, Vertex v2, out Face f_new, out Halfedge he_new, out Halfedge he_new_pair)
        {
            Halfedge e1 = Halfedge;
            while (e1.Vertex != v1)
            {
                e1 = e1.Next;
            }

            if (v2 == e1.Next.Vertex) { throw new Exception("Vertices adjacent"); }
            if (v2 == e1.Prev.Vertex) { throw new Exception("Vertices adjacent"); }

            f_new = new Face(e1.Next);

            Halfedge e2 = e1;
            while (e2.Vertex != v2)
            {
                e2 = e2.Next;
                e2.Face = f_new;
            }

            he_new = new Halfedge(v1, e1.Next, e2, f_new);
            he_new_pair = new Halfedge(v2, e2.Next, e1, this, he_new);
            he_new.Pair = he_new_pair;

            e1.Next.Prev = he_new;
            e1.Next = he_new_pair;
            e2.Next.Prev = he_new_pair;
            e2.Next = he_new;

        }
        #endregion
    }
}
