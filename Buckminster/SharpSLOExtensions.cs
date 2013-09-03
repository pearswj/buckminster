using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace Buckminster
{
    static class SharpSLOExtensions
    {
        public static Point3d[] ToPoint3dArray(this SharpSLO.Types.Molecular molecular)
        {
            return molecular.Nodes.Select(n => new Point3d(n.X, n.Y, n.Z)).ToArray();
        }

        public static BoundingBox GetBoundingBox(this SharpSLO.Types.Molecular molecular)
        {
            if (!molecular.IsValid) { return BoundingBox.Empty; }
            BoundingBox result = new BoundingBox(molecular.ToPoint3dArray());
            result.MakeValid();
            return result;
        }

        public static Line ToLine(this SharpSLO.Types.Molecular molecular, int barIndex)
        {
            var pt1 = molecular.Nodes[molecular.Bars[barIndex].Start];
            var pt2 = molecular.Nodes[molecular.Bars[barIndex].End];
            return new Line(new Point3d(pt1.X, pt1.Y, pt1.Z), new Point3d(pt2.X, pt2.Y, pt2.Z));
        }

        public static Line[] ToLineArray(this SharpSLO.Types.Molecular molecular)
        {
            Line[] lines = new Line[molecular.Bars.Count];
            for (int i = 0; i < lines.Length; i++)
			{
                var pt1 = molecular.Nodes[molecular.Bars[i].Start];
                var pt2 = molecular.Nodes[molecular.Bars[i].End];
                lines[i] = new Line(new Point3d(pt1.X, pt1.Y, pt1.Z), new Point3d(pt2.X, pt2.Y, pt2.Z));
			}

            return lines;
        }

        public static SharpSLO.Types.Molecular ToMolecular(this Buckminster.Types.Mesh mesh)
        {
            var molecular = new SharpSLO.Types.Molecular(mesh.Vertices.Count);

            foreach (var vertex in mesh.Vertices.Select(v => v.Position))
            {
                molecular.Add(vertex.X, vertex.Y, vertex.Z);
            }

            Dictionary<string, int> vlookup = new Dictionary<string, int>();
            for (int i = 0; i < mesh.Vertices.Count; i++)
                vlookup.Add(mesh.Vertices[i].Name, i);

            foreach (var edge in mesh.Halfedges.GetUnique())
            {
                molecular.Add(vlookup[edge.Vertex.Name], vlookup[edge.Prev.Vertex.Name]);
            }

            return molecular;
        }

        public static Vector3d ToVector3d(this SharpSLO.Geometry.Vector vector)
        {
            return new Vector3d(vector.X, vector.Y, vector.Z);
        }
    }
}
