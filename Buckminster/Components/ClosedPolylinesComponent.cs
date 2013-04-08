using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Buckminster
{
    public class ClosedPolylinesComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ClosedPolylinesComponent class.
        /// </summary>
        public ClosedPolylinesComponent()
            : base("Buckminster's Face Polylines", "Polylines",
                "Returns the mesh as a group of closed polylines (representing the faces).",
                "Buckminster", "Utils")
        {
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
            pManager.AddCurveParameter("Face Polylines", "F",
                "A list of closed polylines representing the faces", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;

            if (!DA.GetData(0, ref mesh)) { return; }

            DA.SetDataList(0, mesh.ToClosedPolylines());
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
                return Properties.Resources.PolylinesComponent;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{9d5bf100-0893-4af3-bfcd-1b2ab29567f4}"); }
        }
    }
}