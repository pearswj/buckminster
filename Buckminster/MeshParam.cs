using System;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Buckminster
{
    class MeshParam : GH_Param<MeshGoo>, IGH_PreviewObject/*, IGH_BakeAwareObject*/
    {
        public MeshParam()
            : base(new GH_InstanceDescription("Buckminster's Mesh", "Mesh", "Maintains a collection of Mesh data.", "Buckminster", "Seeds"))
        {
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{33f7d0ff-42fa-4cdb-a87d-759b97671d0c}"); }
        }
        public override GH_Exposure Exposure
        {
            get
            {
                // If you want to provide this parameter on the toolbars, use something other than hidden.
                return GH_Exposure.primary;
            }
        }
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.MeshParameter;
            }
        }

        #region preview methods
        public BoundingBox ClippingBox
        {
            get
            {
                return Preview_ComputeClippingBox();
            }
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            // Meshes aren't drawn.
            Preview_DrawMeshes(args);
        }

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            // Use a standard method to draw gunk, you don't have to specifically implement this.
            Preview_DrawWires(args);
        }

        private bool m_hidden = false;
        public bool Hidden
        {
            get { return m_hidden; }
            set { m_hidden = value; }
        }

        public bool IsPreviewCapable
        {
            get { return true; }
        }
        #endregion

        #region persistent param methods
        /*protected override GH_GetterResult Prompt_Plural(ref List<BmMeshGoo> values)
        {
            return GH_GetterResult.cancel; ;
        }
        protected override GH_GetterResult Prompt_Singular(ref BmMeshGoo value)
        {
            return GH_GetterResult.cancel; ;
        }
        protected override System.Windows.Forms.ToolStripMenuItem Menu_CustomSingleValueItem()
        {
            System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
            item.Text = "Not available";
            item.Visible = false;
            return item;
        }
        protected override System.Windows.Forms.ToolStripMenuItem Menu_CustomMultiValueItem()
        {
            System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
            item.Text = "Not available";
            item.Visible = false;
            return item;
        }*/
        #endregion

        #region bake methods
        /*public void BakeGeometry(Rhino.RhinoDoc doc, Rhino.DocObjects.ObjectAttributes att, List<Guid> obj_ids)
        {
            throw new NotImplementedException();
        }

        public void BakeGeometry(Rhino.RhinoDoc doc, List<Guid> obj_ids)
        {
            throw new NotImplementedException();
        }

        public bool IsBakeCapable
        {
            get { return true; }
        }*/
        #endregion
    }
}
