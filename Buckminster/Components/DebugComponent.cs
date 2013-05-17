using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Buckminster.Types;
using Mesh = Buckminster.Types.Mesh;

namespace Buckminster.Components
{
    public class DebugComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public DebugComponent()
            : base("Buckminster's Debug Component", "Debug",
                "A quick and dirty passthrough component for debugging",
                "Buckminster", "Debug")
        {
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Mesh", "M", "Input mesh", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Mesh", "M", "Output mesh", GH_ParamAccess.item);
            pManager.AddPointParameter("Halfedge pairs", "HP", "Points showing the links between opposite halfedges", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            if (!DA.GetData(0, ref mesh)) { return; }

            mesh = mesh.Duplicate();
            
            List<Point3f> hp = new List<Point3f>();
            foreach (Halfedge h in mesh.Halfedges)
            {
                if (h.Pair.Pair == h)
                {
                    hp.Add(h.Midpoint);
                }
            }

            DA.SetData(0, mesh);
            DA.SetDataList(1, hp);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{3b704eca-2d1e-4b6f-bb68-a176f5e013d6}"); }
        }
    }
}