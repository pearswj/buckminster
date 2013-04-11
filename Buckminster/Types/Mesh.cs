using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

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
            Halfedges = new MeshHalfedgeList(this);
            Vertices = new List<Vertex>();
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

            //var faces = Enumerable.Range(0, source.Faces.Count).Select(i => source.TopologyVertices.IndicesFromFace(i));
            //InitIndexed(source.TopologyVertices, faces);

            // Add vertices
            Vertices.Capacity = source.TopologyVertices.Count;
            foreach (Point3f p in source.TopologyVertices)
            {
                Vertices.Add(new Vertex(p));
            }

            // Add faces (and construct halfedges and store in hash table)
            for (int i = 0; i < source.Faces.Count; i++)
            {
                var vertices = source.TopologyVertices.IndicesFromFace(i).Select(v => Vertices[v]);
                Faces.Add(vertices);
            }

            // Find and link halfedge pairs
            Halfedges.MatchPairs();
        }
        private Mesh(IEnumerable<Point3f> verticesByPoints, IEnumerable<IEnumerable<int>> facesByVertexIndices)
            : this()
        {
            //InitIndexed(verticesByPoints, facesByVertexIndices);

            // Add vertices
            foreach (Point3f p in verticesByPoints)
            {
                Vertices.Add(new Vertex(p));
            }

            foreach (IEnumerable<int> indices in facesByVertexIndices)
            {
                Faces.Add(indices.Select(i => Vertices[i]));
            }

            // Find and link halfedge pairs
            Halfedges.MatchPairs();
        }
        private void InitIndexed(IEnumerable<Point3f> verticesByPoints, IEnumerable<IEnumerable<int>> facesByVertexIndices)
        {
            // Add vertices
            foreach (Point3f p in verticesByPoints)
            {
                Vertices.Add(new Vertex(p));
            }

            // Add faces
            foreach (IEnumerable<int> indices in facesByVertexIndices)
            {
                Faces.Add(indices.Select(i => Vertices[i]));
            }

            // Find and link halfedge pairs
            Halfedges.MatchPairs();
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
        #endregion

        #region methods
        /// <summary>
        /// A string representation of the mesh, mimicking Grasshopper's mesh class.
        /// </summary>
        /// <returns>a string representation of the mesh</returns>
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
        /// <summary>
        /// Convert to Rhino mesh type.
        /// </summary>
        /// <returns></returns>
        public Rhino.Geometry.Mesh ToRhinoMesh()
        {
            Rhino.Geometry.Mesh target = new Rhino.Geometry.Mesh();

            // TODO: duplicate mesh and triangulate
            Mesh source = Duplicate();//.Triangulate();
            for (int i = 0; i < source.Faces.Count; i++)
            {
                if (source.Faces[i].Sides > 3)
                {
                    source.Faces.Triangulate(i, true);
                }
            }

            // Strip down to Face-Vertex structure
            Point3f[] points = source.ListVerticesByPoints();
            List<int>[] faceIndices = source.ListFacesByVertexIndices();
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
            return Halfedges.GetUnique().Select(h => new Rhino.Geometry.Line(h.Prev.Vertex.Position, h.Vertex.Position)).ToList();
        }
        public Mesh Ribbon(float offset)
        {
            Mesh ribbon = Duplicate();
            //ribbon.Faces.Clear();

            List<List<Vertex>> all_new_vertices = new List<List<Vertex>>();
            
            for (int k = 0; k < Vertices.Count; k++)
            {
                Vertex v = ribbon.Vertices[k];
                List<Vertex> new_vertices = new List<Vertex>();
                Halfedge edge = v.Halfedge;
                do
                {
                    Vector3f normal = edge.Pair.Face.Normal;
                    Halfedge edge2 = edge.Pair.Prev;

                    Vector3f o1 = Vector3f.CrossProduct(edge.Vector, normal);
                    Vector3f o2 = Vector3f.CrossProduct(normal, edge2.Vector);
                    o1.Unitize();
                    o2.Unitize();
                    o1 *= offset;
                    o2 *= offset;

                    Line l1 = new Line(edge.Vertex.Position + o1, edge.Prev.Vertex.Position + o1);
                    Line l2 = new Line(edge2.Vertex.Position + o2, edge2.Prev.Vertex.Position + o2);
                    
                    double a, b;
                    Rhino.Geometry.Intersect.Intersection.LineLine(l1, l2, out a, out b);
                    Point3d new_point = l1.PointAt(a);
                    Vertex new_vertex = new Vertex(new Point3f((float)new_point.X, (float)new_point.Y, (float)new_point.Z));
                    ribbon.Vertices.Add(new_vertex);
                    new_vertices.Add(new_vertex);
                    edge = edge2;
                } while (edge != v.Halfedge);

                all_new_vertices.Add(new_vertices);
                ribbon.Faces.Add(new_vertices);
            }

            // change edges to reference new vertices (and cull old vertices)
            for (int k = 0; k < Vertices.Count; k++)
            {
                Vertex v = ribbon.Vertices[k];
                int c = 0;
                Halfedge edge = v.Halfedge;
                do
                {
                    //edge.Pair.Prev.Vertex = all_new_vertices[k][c++];
                    ribbon.Halfedges.SetVertex(edge.Pair.Prev, all_new_vertices[k][c++]);
                    edge = edge.Pair.Prev;
                } while (edge != v.Halfedge);
             }

            ribbon.Vertices.RemoveRange(0, Vertices.Count); // cull old vertices

            // use existing edges to create 'ribbon' faces
            MeshHalfedgeList temp = new MeshHalfedgeList();
            for (int i = 0; i < Halfedges.Count; i++)
            {
                temp.Add(ribbon.Halfedges[i]);
            }
            List<Halfedge> items = temp.GetUnique();
           
            foreach (Halfedge halfedge in items)
            {
                Vertex[] new_vertices = new Vertex[]{
                    halfedge.Vertex,
                    halfedge.Prev.Vertex,
                    halfedge.Pair.Vertex,
                    halfedge.Pair.Prev.Vertex
                };
                ribbon.Faces.Add(new_vertices);
            }

            // search and link pairs
            ribbon.Halfedges.MatchPairs();

            return ribbon;
        }
        #endregion
    }
}
