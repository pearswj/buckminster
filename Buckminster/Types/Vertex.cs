using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rhino.Geometry;

namespace Buckminster
{
    public class Vertex
    {
        #region constructors
        public Vertex(Point3f point)
        {
            Position = point;
            Name = Guid.NewGuid().ToString("N").Substring(0, 8);
        }
        public Vertex(Point3f point, Halfedge edge)
            : this(point)
        {
            Halfedge = edge;
        }
        #endregion

        #region properties
        /// <summary>
        /// The coordinates of the vertex.
        /// </summary>
        public Point3f Position { get; set; }

        /// <summary>
        /// One of the halfedges that points towards the vertex.
        /// Used as a starting point for traversal from the vertex.
        /// </summary>
        public Halfedge Halfedge { get; set; }
        public string Name { get; private set; }
        public Vector3f Normal
        {
            get
            {
                Vector3d normal = new Vector3d();
                foreach (Face f in GetVertexFaces())
                {
                    normal += f.Normal;
                }
                normal.Unitize();
                return new Vector3f((float)normal.X, (float)normal.Y, (float)normal.Z);
            }
        }
        #endregion

        #region methods
        /// <summary>
        /// Finds the faces which share this vertex
        /// </summary>
        /// <returns>a list of incident faces, ordered counter-clockwise around the vertex</returns>
        public List<Face> GetVertexFaces()
        {
            // TODO: add reverse sort (in case we hit a boundary)
            List<Face> adjacent = new List<Face>();
            bool boundary = false;
            Halfedge edge = Halfedge;
            do
            {
                adjacent.Add(edge.Face);
                if (edge.Pair == null)
                {
                    boundary = true; // boundary hit
                    break;
                }
                edge = edge.Pair.Prev;
            } while (edge != Halfedge);

            if (boundary)
            {
                List<Face> rAdjacent = new List<Face>();
                edge = Halfedge;
                while (edge.Next.Pair != null)
                {
                    edge = edge.Next.Pair;
                    rAdjacent.Add(edge.Face);
                }
                if (rAdjacent.Count > 1) { rAdjacent.Reverse(); }
                rAdjacent.AddRange(adjacent);
                return rAdjacent;
            }

            return adjacent;
        }
        #endregion
    }
}
