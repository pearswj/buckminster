using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Buckminster
{
    public class OffsetComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the OffsetComponent class.
        /// </summary>
        public OffsetComponent()
            : base("Buckminster's Offset Mesh", "Offset",
                "Offsets a mesh by moving each vertex in the normal direction",
                "Buckminster", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Mesh", "M", "Input mesh", GH_ParamAccess.item);
            pManager.AddNumberParameter("Offset distance", "O", "Distance by which to offste the mesh", GH_ParamAccess.item, 1.0);
            //pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Mesh", "M", "Offset mesh", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            if (!DA.GetData(0, ref mesh)) { return; }

            double distance = double.NaN;
            if (!DA.GetData<double>(1, ref distance)) { return; }

            DA.SetData(0, mesh.Offset((float)distance));
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
                return Properties.Resources.OffsetComponent;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{a1b26292-6daf-4d83-8827-dcd162436c4d}"); }
        }
    }
}