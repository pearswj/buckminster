using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using Buckminster.Types.Collections;

namespace Buckminster
{
    public class Mesh
    {
        #region constructors
        public Mesh()
        {
            Halfedges = new MeshHalfedgeList();
            Vertices = new List<Vertex>();
            //Faces = new List<Face>();
            Faces = new MeshFaceList(this);
        }
        /// <summary>
        /// Constructor to build a custom mesh from Rhino's mesh type
        /// </summary>
        /// <param name="source">the Rhino mesh</param>
        public Mesh(Rhino.Geometry.Mesh source)
            : this()
        {
            // Check that the mesh is oriented and manifold
            bool isManifold, isOriented, hasBoundary;
            isManifold = source.IsManifold(true, out isOriented, out hasBoundary);
            if (!isManifold || !isOriented) { return; }

            // Remove unused vertices
            source.Vertices.CullUnused();

            // Add vertices
            Vertices.Capacity = source.TopologyVertices.Count;
            foreach (Point3f p in source.TopologyVertices)
            {
                Vertices.Add(new Vertex(p));
            }

            // Add faces (and construct halfedges and store in hash table)
            //Faces.Capacity = source.Faces.Count;
            for (int i = 0; i < source.Faces.Count; i++)
            {
                int[] corners = source.TopologyVertices.IndicesFromFace(i);
                int n = corners.Length;
                Halfedge[] edges = new Halfedge[n];
                // Create halfedges
                for (int j = 0; j < n; j++)
                {
                    edges[j] = new Halfedge(Vertices[corners[(j + 1) % n]], null, null, null);
                    Vertices[corners[(j + 1) % n]].Halfedge = edges[j];
                }
                Face newFace = new Face(edges[0]);
                // Link halfedges to face, next and prev
                for (int j = 0; j < n; j++)
                {
                    edges[j].Face = newFace;
                    edges[j].Next = edges[(j + 1) % n];
                    edges[j].Prev = edges[(j + n - 1) % n];
                }
                Faces.Add(newFace);
                foreach (Halfedge h in edges) { Halfedges.Add(h); }
            }

            // Find and link halfedge pairs
            for (int i = 0; i < Halfedges.Count; i++)
            {
                String rname = Halfedges[i].Prev.Vertex.Name + Halfedges[i].Vertex.Name;
                try
                {
                    Halfedges[i].Pair = Halfedges[rname];
                }
                catch (KeyNotFoundException)
                {
                    // halfedge must be a boundary
                }
            }
        }
        private Mesh(IEnumerable<Point3f> verticesByPoints, ICollection<int>[] facesByVertexIndices)
            : this()
        {
            // Add vertices
            foreach (Point3f p in verticesByPoints)
            {
                Vertices.Add(new Vertex(p));
            }

            // Add faces (and construct halfedges and store in hash table)
            //Faces.Capacity = facesByVertexIndices.Length;
            for (int i = 0; i < facesByVertexIndices.Length; i++)
            {
                List<int> corners = new List<int>(facesByVertexIndices[i]);
                int n = corners.Count;
                Halfedge[] edges = new Halfedge[n];
                // Create halfedges
                for (int j = 0; j < n; j++)
                {
                    edges[j] = new Halfedge(Vertices[corners[(j + 1) % n]]);
                    if (Vertices[corners[(j + 1) % n]].Halfedge == null)
                    {
                        Vertices[corners[(j + 1) % n]].Halfedge = edges[j];
                    }
                }
                Face newFace = new Face(edges[0]);
                // Link halfedges to face, next and prev (and add)
                for (int j = 0; j < n; j++)
                {
                    edges[j].Face = newFace;
                    edges[j].Next = edges[(j + 1) % n];
                    edges[j].Prev = edges[(j + n - 1) % n];
                    Halfedges.Add(edges[j]);
                    // TODO: merge halfedges here
                }
                Faces.Add(newFace);
            }

            // Find and link halfedge pairs
            for (int i = 0; i < Halfedges.Count; i++)
            {
                String rname = Halfedges[i].Prev.Vertex.Name + Halfedges[i].Vertex.Name;
                if (Halfedges.Contains(rname))
                {
                    Halfedges[i].Pair = Halfedges[rname];
                }
            }
        }
        public Mesh Duplicate()
        {
            // Export to face/vertex and rebuild
            return new Mesh(ListVerticesByPoints(), ListFacesByVertexIndices());
        }
        #endregion

        #region properties
        public MeshHalfedgeList Halfedges { get; private set; }
        public List<Vertex> Vertices { get; private set; }
        //public List<Face> Faces { get; private set; }
        public MeshFaceList Faces { get; private set; }
        public bool IsValid
        {
            get
            {
                if (Halfedges.Count == 0) { return false; }
                if (Vertices.Count == 0) { return false; }
                if (Faces.Count == 0) { return false; }

                // TODO: beef this up (check for a valid mesh)

                return true;
            }
        }
        public BoundingBox BoundingBox
        {
            get
            {
                if (!IsValid) { return BoundingBox.Empty; }
                List<Point3d> points = new List<Point3d>();
                foreach (Vertex v in this.Vertices)
                {
                    points.Add(v.Position);
                }
                BoundingBox result = new BoundingBox(points);
                result.MakeValid();
                return result;
            }
        }
        #endregion

        #region conway methods
        /// <summary>
        /// Conway's dual operator
        /// </summary>
        /// <returns>the dual as a new mesh</returns>
        public Mesh Dual()
        {
            //Mesh dual = new Mesh();

            // Create vertices from faces
            //dual.Vertices.Capacity = Faces.Count;
            List<Point3f> vertexPoints = new List<Point3f>(Faces.Count);
            foreach (Face f in Faces)
            {
                //dual.Vertices.Add(new Vertex(f.Centroid()));
                vertexPoints.Add(f.Centroid);
            }

            // Create sublist of non-boundary vertices
            MeshVertexList subset = new MeshVertexList();
            foreach (Vertex v in Vertices)
            {
                subset.Add(v);
            }
            foreach (Halfedge h in Halfedges)
            {
                if (h.Pair == null)
                {
                    subset.Remove(h.Vertex.Name);
                }
            }

            // List new faces by their vertex indices
            // (i.e. old vertices by their face indices)
            Dictionary<string, int> flookup = new Dictionary<string, int>();
            for (int i = 0; i < Faces.Count; i++)
            {
                flookup.Add(Faces[i].Name, i);
            }
            List<int>[] faceIndices = new List<int>[subset.Count];
            for (int i = 0; i < subset.Count; i++)
            {
                List<int> fIndex = new List<int>();
                foreach (Face f in subset[i].GetVertexFaces())
                {
                    fIndex.Add(flookup[f.Name]);
                }
                faceIndices[i] = fIndex;
            }

            return new Mesh(vertexPoints, faceIndices);
        }
        #endregion

        #region geometry methods
        public Mesh Offset(float offset)
        {
            Point3f[] points = new Point3f[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                points[i] = Vertices[i].Position + (Vertices[i].Normal * offset);
            }
            return new Mesh(points, ListFacesByVertexIndices());
        }
        public Boolean Split(Face f, Vertex v1, Vertex v2)
        {

            return true;
        }
        #endregion

        #region methods
        public override string ToString()
        {
            return base.ToString() + string.Format(" (V:{0} F:{1})", Vertices.Count, Faces.Count);
        }
        /// <summary>
        /// Get the positions of all mesh vertices. Note that points are duplicated.
        /// </summary>
        /// <returns>a list of vertex positions</returns>
        private Point3f[] ListVerticesByPoints()
        {
            Point3f[] points = new Point3f[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                Point3f pos = Vertices[i].Position;
                points[i] = new Point3f(pos.X, pos.Y, pos.Z);
            }
            return points;
        }
        private List<int>[] ListFacesByVertexIndices()
        {
            List<int>[] fIndex = new List<int>[Faces.Count];
            Dictionary<String, int> vlookup = new Dictionary<String, int>();
            for (int i = 0; i < Vertices.Count; i++)
            {
                vlookup.Add(Vertices[i].Name, i);
            }
            for (int i = 0; i < Faces.Count; i++)
            {
                List<int> vertIdx = new List<int>();
                foreach (Vertex v in Faces[i].GetVertices())
                {
                    vertIdx.Add(vlookup[v.Name]);
                }
                fIndex[i] = vertIdx;
            }
            return fIndex;
        }
        public Rhino.Geometry.Mesh ToRhinoMesh()
        {
            Rhino.Geometry.Mesh target = new Rhino.Geometry.Mesh();

            // TODO: duplicate mesh and triangulate
            //Mesh source = Duplicate();//.Triangulate();

            // Strip down to Face-Vertex structure
            Point3f[] points = ListVerticesByPoints();
            List<int>[] faceIndices = ListFacesByVertexIndices();
            // Add vertices
            for (int i = 0; i < points.Length; i++)
            {
                target.Vertices.Add(points[i]);
            }
            // Add faces
            foreach (List<int> f in faceIndices)
            {
                if (f.Count == 3)
                {
                    target.Faces.AddFace(f[0], f[1], f[2]);
                }
                else if (f.Count == 4)
                {
                    target.Faces.AddFace(f[0], f[1], f[2], f[3]);
                }
            }
            target.Compact();

            return target;
        }
        public List<Polyline> ToClosedPolylines()
        {
            List<Polyline> polylines = new List<Polyline>(Faces.Count);
            foreach (Face f in Faces)
            {
                polylines.Add(f.ToClosedPolyline());
            }
            return polylines;
        }
        public List<Line> ToLines()
        {
            // Draw edges (without duplication)
            MeshHalfedgeList marker = new MeshHalfedgeList();
            List<Rhino.Geometry.Line> lines = new List<Rhino.Geometry.Line>();
            for (int i = 0; i < Halfedges.Count; i++)
            {
                Halfedge halfedge = Halfedges[i];

                // Do not add line if halfedge is on the "do not draw" list
                if (marker.Contains(halfedge.Name)) { continue; }

                // Add line to list for halfedge
                lines.Add(new Rhino.Geometry.Line(halfedge.Prev.Vertex.Position, halfedge.Vertex.Position));

                // If halfedge has a pair, add it to the "do not draw" list
                if (halfedge.Pair != null)
                {
                    marker.Add(Halfedges[halfedge.Pair.Name]);
                }
            }
            return lines;
        }
        #endregion
    }
}
