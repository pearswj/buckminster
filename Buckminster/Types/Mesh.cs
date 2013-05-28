using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;
using Buckminster.Types.Collections;

namespace Buckminster.Types
{
    /// <summary>
    /// A class for manifold meshes which uses the Halfedge data structure.
    /// </summary>
    public class Mesh
    {
        #region constructors
        public Mesh()
        {
            Halfedges = new MeshHalfedgeList(this);
            Vertices = new MeshVertexList(this);
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
            bool isOriented, hasBoundary;
            var isManifold = source.IsManifold(true, out isOriented, out hasBoundary);
            if (!isManifold || !isOriented) return;

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

            //Vertices.CullUnused();
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
        public MeshVertexList Vertices { get; private set; }
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
            // Create vertices from faces
            List<Point3f> vertexPoints = new List<Point3f>(Faces.Count);
            foreach (Face f in Faces)
                vertexPoints.Add(f.Centroid);

            // Create sublist of non-boundary vertices
            var subset = new Dictionary<string, Vertex>(Vertices.Count);
            foreach (var he in Halfedges)
            {
		        if (he.Pair != null && !subset.ContainsKey(he.Vertex.Name))
                    subset.Add(he.Vertex.Name, he.Vertex);
            }

            // List new faces by their vertex indices
            // (i.e. old vertices by their face indices)
            Dictionary<string, int> flookup = new Dictionary<string, int>();
            for (int i = 0; i < Faces.Count; i++)
                flookup.Add(Faces[i].Name, i);

            var faceIndices = new List<List<int>>(subset.Count);
            foreach (var v in subset.Values)
            {
                List<int> fIndex = new List<int>();
                foreach (Face f in v.GetVertexFaces())
                    fIndex.Add(flookup[f.Name]);
                faceIndices.Add(fIndex);
            }

            return new Mesh(vertexPoints, faceIndices);
        }
        /// <summary>
        /// Conway's ambo operator
        /// </summary>
        /// <returns>the ambo as a new mesh</returns>
        public Mesh Ambo()
        {
            // Create points at midpoint of unique halfedges (edges to vertices) and create lookup table
            List<Point3f> vertexPoints = new List<Point3f>(); // vertices as points
            Dictionary<string, int> hlookup = new Dictionary<string, int>();
            int count = 0;
            foreach (var edge in Halfedges)
            {
                // if halfedge's pair is already in the table, give it the same index
                if (edge.Pair != null && hlookup.ContainsKey(edge.Pair.Name))
                    hlookup.Add(edge.Name, hlookup[edge.Pair.Name]);
                else // otherwise create a new vertex and increment the index
                {
                    hlookup.Add(edge.Name, count++);
                    vertexPoints.Add(edge.Midpoint);
                }
            }
            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            // faces to faces
            foreach (var face in Faces)
            {
                faceIndices.Add(face.GetHalfedges().Select(edge => hlookup[edge.Name]));
            }
            // vertices to faces
            foreach (var vertex in Vertices)
            {
                var he = vertex.Halfedges;
                if (he.Count == 0) continue; // no halfedges
                if (he[0].Next.Pair == null) continue; // boundary vertex
                    //he.Add(he[0].Next);
                faceIndices.Add(he.Select(edge => hlookup[edge.Name]));
            }

            return new Mesh(vertexPoints, faceIndices);
        }
        /// <summary>
        /// Conway's kis operator
        /// </summary>
        /// <returns>the kis as a new mesh</returns>
        public Mesh Kis()
        {
            // vertices and faces to vertices
            var vertexPoints = Enumerable.Concat(Vertices.Select(v => v.Position), Faces.Select(f => f.Centroid));
            // vertex lookup
            Dictionary<string, int> vlookup = new Dictionary<string, int>();
            int n = Vertices.Count;
            for (int i = 0; i < n; i++)
            {
                vlookup.Add(Vertices[i].Name, i);
            }
            // create new tri-faces (like a fan)
            var faceIndices = new List<IEnumerable<int>>(); // faces as vertex indices
            for (int i = 0; i < Faces.Count; i++)
            {
                foreach (var edge in Faces[i].GetHalfedges())
                {
                    // create new face from edge start, edge end and centroid
                    faceIndices.Add(new int[]{vlookup[edge.Prev.Vertex.Name], vlookup[edge.Vertex.Name], i + n});
                }
            }
            return new Mesh(vertexPoints, faceIndices);
        }
        #endregion

        #region geometry methods
        /// <summary>
        /// Offsets a mesh by moving each vertex by the specified distance along its normal vector.
        /// </summary>
        /// <param name="offset">Offset distance</param>
        /// <returns>The offset mesh</returns>
        public Mesh Offset(double offset)
        {
            var offsetList = Enumerable.Range(0, Vertices.Count).Select(i => offset).ToList();
            return Offset(offsetList);
        }
        public Mesh Offset(List<double> offset)
        {
            Point3f[] points = new Point3f[Vertices.Count];
            for (int i = 0; i < Vertices.Count && i < offset.Count; i++)
            {
                points[i] = Vertices[i].Position + (Vertices[i].Normal * (float)offset[i]);
            }
            /*var points = new List<Point3f>();
            foreach (var item in Vertices.Zip(offset, (v, o) => new { v, o }))
            {
                points.Add(item.v.Position + (item.v.Normal * (float)o);
            }*/
            return new Mesh(points, ListFacesByVertexIndices());
        }
        /// <summary>
        /// Thickens each mesh edge in the plane of the mesh surface.
        /// </summary>
        /// <param name="offset">Distance to offset edges in plane of adjacent faces</param>
        /// <param name="boundaries">If true, attempt to ribbon boundary edges</param>
        /// <returns>The ribbon mesh</returns>
        public Mesh Ribbon(float offset, Boolean boundaries, float smooth)
        {
            Mesh ribbon = Duplicate();
            var orig_faces = ribbon.Faces.ToArray();

            List<List<Halfedge>> incidentEdges = ribbon.Vertices.Select(v => v.Halfedges).ToList();

            // create new "vertex" faces
            List<List<Vertex>> all_new_vertices = new List<List<Vertex>>();
            for (int k = 0; k < Vertices.Count; k++)
            {
                Vertex v = ribbon.Vertices[k];
                List<Vertex> new_vertices = new List<Vertex>();
                List<Halfedge> halfedges = incidentEdges[k];
                Boolean boundary = halfedges[0].Next.Pair != halfedges[halfedges.Count - 1];

                // if the edge loop around this vertex is open, close it with 'temporary edges'
                if (boundaries && boundary)
                {
                    Halfedge a, b;
                    a = halfedges[0].Next;
                    b = halfedges[halfedges.Count - 1];
                    if (a.Pair == null)
                    {
                        a.Pair = new Halfedge(a.Prev.Vertex) {Pair = a};
                    }
                    if (b.Pair == null)
                    {
                        b.Pair = new Halfedge(b.Prev.Vertex) {Pair = b};
                    }
                    a.Pair.Next = b.Pair;
                    b.Pair.Prev = a.Pair;
                    a.Pair.Prev = a.Pair.Prev ?? a; // temporary - to allow access to a.Pair's start/end vertices
                    halfedges.Add(a.Pair);
                }

                foreach (Halfedge edge in halfedges)
                {
                    if (halfedges.Count < 2) { continue; }

                    Vector3f normal = edge.Face != null ? edge.Face.Normal : Vertices[k].Normal;
                    Halfedge edge2 = edge.Next;

                    Vector3f o1 = Vector3f.CrossProduct(normal, edge.Vector);
                    Vector3f o2 = Vector3f.CrossProduct(normal, edge2.Vector);
                    o1.Unitize();
                    o2.Unitize();
                    o1 *= offset;
                    o2 *= offset;

                    if (edge.Face == null)
                    {
                        // boundary condition: create two new vertices in the plane defined by the vertex normal
                        Vertex v1 = new Vertex(v.Position + (edge.Vector * (1 / edge.Vector.Length) * -offset) + o1);
                        Vertex v2 = new Vertex(v.Position + (edge2.Vector * (1 / edge2.Vector.Length) * offset) + o2);
                        ribbon.Vertices.Add(v2);
                        ribbon.Vertices.Add(v1);
                        new_vertices.Add(v2);
                        new_vertices.Add(v1);
                        Halfedge c = new Halfedge(v2, edge2, edge, null);
                        edge.Next = c;
                        edge2.Prev = c;
                    }
                    else
                    {
                        // internal condition: offset each edge in the plane of the shared face and create a new vertex where they intersect eachother
                        Line l1 = new Line(edge.Vertex.Position + o1, edge.Prev.Vertex.Position + o1);
                        Line l2 = new Line(edge2.Vertex.Position + o2, edge2.Prev.Vertex.Position + o2);

                        double a, b;
                        Rhino.Geometry.Intersect.Intersection.LineLine(l1, l2, out a, out b);
                        Point3d new_point = l1.PointAt(a);
                        Vertex new_vertex = new Vertex(new Point3f((float)new_point.X, (float)new_point.Y, (float)new_point.Z));
                        ribbon.Vertices.Add(new_vertex);
                        new_vertices.Add(new_vertex);
                    }
                }

                if ((!boundaries && boundary) == false) // only draw boundary node-faces in 'boundaries' mode
                    ribbon.Faces.Add(new_vertices);
                all_new_vertices.Add(new_vertices);
            }

            // change edges to reference new vertices
            for (int k = 0; k < Vertices.Count; k++)
            {
                Vertex v = ribbon.Vertices[k];
                if (all_new_vertices[k].Count < 1) { continue; }
                int c = 0;
                foreach (Halfedge edge in incidentEdges[k])
                {
                    if (!ribbon.Halfedges.SetVertex(edge, all_new_vertices[k][c++]))
                        edge.Vertex = all_new_vertices[k][c];
                }
                //v.Halfedge = null; // unlink from halfedge as no longer in use (culled later)
                // note: new vertices don't link to any halfedges in the mesh until later
            }

            // cull old vertices
            ribbon.Vertices.RemoveRange(0, Vertices.Count);

            // use existing edges to create 'ribbon' faces
            MeshHalfedgeList temp = new MeshHalfedgeList();
            for (int i = 0; i < Halfedges.Count; i++)
            {
                temp.Add(ribbon.Halfedges[i]);
            }
            List<Halfedge> items = temp.GetUnique();

            foreach (Halfedge halfedge in items)
            {
                if (halfedge.Pair != null)
                {
                    // insert extra vertices close to the new 'vertex' vertices to preserve shape when subdividing
                    if (smooth > 0.0)
                    {
                        if (smooth > 0.5) { smooth = 0.5f; }
                        Vertex[] new_vertices = new Vertex[]{
                            new Vertex(halfedge.Vertex.Position + (-smooth * halfedge.Vector)),
                            new Vertex(halfedge.Prev.Vertex.Position + (smooth * halfedge.Vector)),
                            new Vertex(halfedge.Pair.Vertex.Position + (-smooth * halfedge.Pair.Vector)),
                            new Vertex(halfedge.Pair.Prev.Vertex.Position + (smooth * halfedge.Pair.Vector))
                        };
                        ribbon.Vertices.AddRange(new_vertices);
                        Vertex[] new_vertices1 = new Vertex[]{
                            halfedge.Vertex,
                            new_vertices[0],
                            new_vertices[3],
                            halfedge.Pair.Prev.Vertex
                        };
                        Vertex[] new_vertices2 = new Vertex[]{
                            new_vertices[1],
                            halfedge.Prev.Vertex,
                            halfedge.Pair.Vertex,
                            new_vertices[2]
                        };
                        ribbon.Faces.Add(new_vertices);
                        ribbon.Faces.Add(new_vertices1);
                        ribbon.Faces.Add(new_vertices2);
                    }
                    else
                    {
                        Vertex[] new_vertices = new Vertex[]{
                            halfedge.Vertex,
                            halfedge.Prev.Vertex,
                            halfedge.Pair.Vertex,
                            halfedge.Pair.Prev.Vertex
                            };

                        ribbon.Faces.Add(new_vertices);
                    }
                }
            }

            // remove original faces, leaving just the ribbon
            //var orig_faces = Enumerable.Range(0, Faces.Count).Select(i => ribbon.Faces[i]);
            foreach (Face item in orig_faces)
            {
                ribbon.Faces.Remove(item);
            }

            // search and link pairs
            ribbon.Halfedges.MatchPairs();

            return ribbon;
        }
        /// <summary>
        /// Gives thickness to mesh faces by offsetting the mesh and connecting naked edges with new faces.
        /// </summary>
        /// <param name="distance">Distance to offset the mesh (thickness)</param>
        /// <param name="symmetric">Whether to extrude in both (-ve and +ve) directions</param>
        /// <returns>The extruded mesh (always closed)</returns>
        public Mesh Extrude(double distance, bool symmetric)
        {
            var offsetList = Enumerable.Range(0, Vertices.Count).Select(i => distance).ToList();
            return Extrude(offsetList, symmetric);
        }
        public Mesh Extrude(List<double> distance, bool symmetric)
        {
            Mesh ext, top; // ext (base) == target
            if (symmetric)
            {
                ext = Offset(distance.Select(d => 0.5 * d).ToList());
                top = Offset(distance.Select(d => -0.5 * d).ToList());
            }
            else
            {
                ext = Duplicate();
                top = Offset(distance);
            }

            top.Halfedges.Flip();

            // append top to ext (can't use Append() because copy would reverse face loops)
            foreach (var v in top.Vertices) ext.Vertices.Add(v);
            foreach (var h in top.Halfedges) ext.Halfedges.Add(h);
            foreach (var f in top.Faces) ext.Faces.Add(f);

            // get indices of naked halfedges in source mesh
            var naked = Halfedges.Select((item, index) => index)
                .Where(i => Halfedges[i].Pair == null).ToList();

            if (naked.Count > 0)
            {
                int n = Halfedges.Count;
                int failed = 0;
                foreach (var i in naked)
                {
                    Vertex[] vertices = new Vertex[] {
                        ext.Halfedges[i].Vertex,
                        ext.Halfedges[i].Prev.Vertex,
                        ext.Halfedges[i + n].Vertex,
                        ext.Halfedges[i + n].Prev.Vertex
                    };
                    if (ext.Faces.Add(vertices) == false) { failed++; }
                }
            }

            ext.Halfedges.MatchPairs();

            return ext;
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
        /// Gets the positions of all mesh vertices. Note that points are duplicated.
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
        /// <summary>
        /// Gets the indices of vertices in each face loop (i.e. index face-vertex data structure).
        /// Used for duplication and conversion to other mesh types, such as Rhino's.
        /// </summary>
        /// <returns>An array of lists of vertex indices.</returns>
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
        /// Recursively triangulates until only tri-/quad-faces remain.
        /// </summary>
        /// <returns>A Rhino mesh.</returns>
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
        /// <summary>
        /// Appends a copy of another mesh to this one.
        /// </summary>
        /// <param name="other">Mesh to append to this one.</param>
        public void Append(Mesh other)
        {
            Mesh dup = other.Duplicate();

            Vertices.AddRange(dup.Vertices);
            foreach (Halfedge edge in dup.Halfedges)
            {
                Halfedges.Add(edge);
            }
            foreach (Face face in dup.Faces)
            {
                Faces.Add(face);
            }
        }
        #endregion
    }
}