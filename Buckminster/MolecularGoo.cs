using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Buckminster.Types;
using Mesh = Buckminster.Types.Mesh;
using Molecular = SharpSLO.Types.Molecular;

namespace Buckminster
{
    class MolecularGoo : GH_GeometricGoo<Molecular>, IGH_PreviewData/*, IGH_BakeAwareData*/
    {
        #region constructors
        public MolecularGoo()
        {
            this.Value = new Molecular();
        }
        public MolecularGoo(Molecular molecular)
        {
            this.Value = molecular ?? new Molecular();
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return DuplicateMolecular();
        }
        public MolecularGoo DuplicateMolecular()
        {
            return new MolecularGoo(Value == null ? new Molecular() : Value.Duplicate());
        }
        #endregion

        #region properties
        public override string TypeDescription
        {
            get { return ("Defines a molecular structure, with link and node connectivity."); }
        }
        public override string TypeName
        {
            get { return ("Molecular"); }
        }
        public override string ToString()
        {
            if (Value == null)
                return "Null Molecular";
            else
                return Value.ToString();
        }
        public override bool IsValid
        {
            get
            {
                if (Value == null) { return false; }
                return Value.IsValid;
            }
        }
        public override string IsValidWhyNot
        {
            get
            {
                if (Value == null) { return "No internal Molecular instance"; }
                if (Value.IsValid) { return string.Empty; }
                return "Invalid Molecular instance"; // Todo: beef this up to be more informative.
            }
        }
        public override BoundingBox Boundingbox
        {
            get
            {
                if (this.m_value == null)
                {
                    return BoundingBox.Empty;
                }
                return this.m_value.GetBoundingBox();
            }
        }
        public override BoundingBox GetBoundingBox(Transform xform)
        {
            if (Value == null) return BoundingBox.Empty;
            var pointCloud = new Rhino.Geometry.PointCloud(Value.ToPoint3dArray());
            return pointCloud.GetBoundingBox(xform);
        }
        #endregion

        #region transformation methods
        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            if (Value == null) { return null; }
            var copy = Value.Duplicate();
            var pointCloud = new Rhino.Geometry.PointCloud(copy.ToPoint3dArray());
            xmorph.Morph(pointCloud);
            for (int i = 0; i < Value.Nodes.Count; i++)
            {
                copy.Nodes[i].X = pointCloud[i].Location.X;
                copy.Nodes[i].Y = pointCloud[i].Location.Y;
                copy.Nodes[i].Z = pointCloud[i].Location.Z;
            }
            return new MolecularGoo(copy);
        }
        public override IGH_GeometricGoo Transform(Transform xform)
        {
            if (Value == null) { return null; }
            var copy = Value.Duplicate();
            var pointCloud = new Rhino.Geometry.PointCloud(copy.ToPoint3dArray());
            pointCloud.Transform(xform);
            for (int i = 0; i < Value.Nodes.Count; i++)
            {
                copy.Nodes[i].X = pointCloud[i].Location.X;
                copy.Nodes[i].Y = pointCloud[i].Location.Y;
                copy.Nodes[i].Z = pointCloud[i].Location.Z;
            }
            return new MolecularGoo(copy);
        }
        #endregion

        #region casting methods
        public override bool CastTo<Q>(out Q target)
        {
            // Cast to Molecular
            if (typeof(Q).IsAssignableFrom(typeof(Molecular)))
            {
                if (Value == null)
                    target = default(Q);
                else
                    target = (Q)(object)Value;
                return true;
            }

            // Fresh out of options...
            target = default(Q);
            return false;
        }
        public override bool CastFrom(object source)
        {
            if (source == null) { return false; }

            // Cast from Molecular
            if (typeof(Molecular).IsAssignableFrom(source.GetType()))
            {
                Value = (Molecular)source;
                return true;
            }

            // Cast from Buckminster.Mesh
            if (typeof(Mesh).IsAssignableFrom(source.GetType()))
            {
                Value = ((Mesh)source).ToMolecular();
                return true;
            }

            // Cast from GH_GeometricGoo<Buckminster.Mesh>
            if (typeof(MeshGoo).IsAssignableFrom(source.GetType()))
            {
                var target = (MeshGoo)source;
                Value = target.Value.ToMolecular();
                return true;
            }
            
            // Cast from Rhino.Geometry.Mesh
            Rhino.Geometry.Mesh rmesh = null;
            if (GH_Convert.ToMesh(source, ref rmesh, GH_Conversion.Primary))
            {
                var target = new Molecular(rmesh.Vertices.Count);

                // add nodes
                foreach (var pt in rmesh.TopologyVertices)
                {
                    target.Add(pt.X, pt.Y, pt.Z);
                }

                // add bars (use edges from mesh)
                for (int i = 0; i < rmesh.TopologyEdges.Count; i++)
                {
                    var edge = rmesh.TopologyEdges.GetTopologyVertices(i);
                    target.Add(edge.I, edge.J);
                }

                Value = target;
                return true;
            }
            
            // Ah well, at least we tried...
            return false;
        }
        #endregion

        #region drawing methods
        public BoundingBox ClippingBox
        {
            get { return Boundingbox; }
        }
        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            // No meshes are drawn.  
        }
        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (Value == null) { return; }
            // TODO: handle colours and thicknesses here.
            args.Pipeline.DrawLines(Value.ToLineArray(), args.Color, args.Thickness);
            // maybe draw nodes too?
        }
        #endregion

        /*public bool BakeGeometry(Rhino.RhinoDoc doc, Rhino.DocObjects.ObjectAttributes att, out Guid obj_guid)
        {
            // TODO: list of guids
            foreach (Line l in Value.ToLines())
            {
                obj_guid = doc.Objects.AddLine(l, att);
            }
            // TODO: group guids
            // TODO: obj_guid = LAST LINE
            return true;
        }*/
    }
}
