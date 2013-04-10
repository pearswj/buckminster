using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Buckminster
{
    class MeshGoo : GH_GeometricGoo<Mesh>, IGH_PreviewData/*, IGH_BakeAwareData*/
    {
        #region constructors
        public MeshGoo()
        {
            this.Value = new Mesh();
        }
        public MeshGoo(Mesh mesh)
        {
            if (mesh == null)
                mesh = new Mesh();
            this.Value = mesh;
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return DuplicateBmMesh();
        }
        public MeshGoo DuplicateBmMesh()
        {
            return new MeshGoo(Value == null ? new Mesh() : Value.Duplicate());
        }
        #endregion

        #region properties
        public override string TypeDescription
        {
            get { return ("Defines a mesh, with greater flexibility than Rhino's"); }
        }
        public override string TypeName
        {
            get { return ("Mesh"); }
        }
        public override string ToString()
        {
            if (Value == null)
                return "Null Mesh";
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
                if (Value == null) { return "No internal Mesh instance"; }
                if (Value.IsValid) { return string.Empty; }
                return "Invalid Mesh instance"; // Todo: beef this up to be more informative.
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
                return this.m_value.BoundingBox;
            }
        }
        public override BoundingBox GetBoundingBox(Transform xform)
        {
            if (Value == null)
            {
                return BoundingBox.Empty;
            }
            if (Value.Halfedges.Count < 1) { return BoundingBox.Empty; }
            // TODO: cast to Rhino.Geometry.Mesh
            Rhino.Geometry.Mesh mesh;
            CastTo<Rhino.Geometry.Mesh>(out mesh);
            return mesh.GetBoundingBox(xform);
        }
        #endregion

        #region transformation methods
        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            if (Value == null) { return null; }
            if (Value.Halfedges.Count < 1) { return null; }
            // TODO: cast to Rhino.Geometry.Mesh
            Rhino.Geometry.Mesh mesh;
            CastTo<Rhino.Geometry.Mesh>(out mesh);
            xmorph.Morph(mesh);
            return new GH_Mesh(mesh);
        }
        public override IGH_GeometricGoo Transform(Transform xform)
        {
            if (Value == null) { return null; }
            if (Value.Halfedges.Count < 1) { return null; }
            // TODO: cast to Rhino.Geometry.Mesh
            Rhino.Geometry.Mesh mesh;
            CastTo<Rhino.Geometry.Mesh>(out mesh);
            mesh.Transform(xform);
            return new GH_Mesh(mesh);
        }
        #endregion

        #region conway methods
        public MeshGoo Dual()
        {
            return new MeshGoo(Value.Dual());
        }
        #endregion

        #region casting methods
        public override bool CastTo<Q>(out Q target)
        {
            // Cast to Buckminster.Mesh
            if (typeof(Q).IsAssignableFrom(typeof(Mesh)))
            {
                if (Value == null)
                    target = default(Q);
                else
                    target = (Q)(object)Value;
                return true;
            }

            // Cast to Rhino.Geometry.Mesh
            if (typeof(Q).IsAssignableFrom(typeof(Rhino.Geometry.Mesh)))
            {
                if (Value == null)
                    target = default(Q);
                else if (Value.Halfedges.Count < 1)
                    target = default(Q);
                else
                    target = (Q)(object)Value.ToRhinoMesh();
                return true;
            }

            // Cast to GH_Mesh
            if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
            {
                if (Value == null)
                    target = default(Q);
                else if (Value.Halfedges.Count < 1)
                    target = default(Q);
                else
                    target = (Q)(object)new GH_Mesh(Value.ToRhinoMesh());
                return true;
            }

            // Cast to closed polylines?

            // Fresh out of options...
            target = default(Q);
            return false;
        }
        public override bool CastFrom(object source)
        {
            if (source == null) { return false; }

            // Cast from Buckminster.Mesh
            if (typeof(Mesh).IsAssignableFrom(source.GetType()))
            {
                Value = (Mesh)source;
                return true;
            }
            
            // Cast from Rhino.Geometry.Mesh
            Rhino.Geometry.Mesh mesh = null;
            if (GH_Convert.ToMesh(source, ref mesh, GH_Conversion.Primary))
            {
                 Value = new Mesh(mesh);
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
            args.Pipeline.DrawMeshShaded(Value.ToRhinoMesh(), args.Material);
        }
        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (Value == null) { return; }
            args.Pipeline.DrawLines(Value.ToLines().ToArray(), args.Color, args.Thickness);
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
