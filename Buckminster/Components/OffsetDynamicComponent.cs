using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Buckminster.Types;

namespace Buckminster.Components
{
    public class OffsetDynamicComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the OffsetComponent class.
        /// </summary>
        public OffsetDynamicComponent()
            : base("Buckminster's Dynamic Offset Mesh", "Offset",
                "Offsets a mesh by moving each vertex in the normal direction. Allows individual offsets to be specified for each vertex.",
                "Buckminster", "Modify")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Mesh", "M", "Input mesh", GH_ParamAccess.item);
            pManager.AddNumberParameter("Offset distance", "O", "Distance by which to offste the mesh", GH_ParamAccess.list);
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

            List<double> distance = new List<double>();
            if (!DA.GetDataList<double>(1, distance)) { return; }

            if (distance.Count != mesh.Vertices.Count)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Length of offset list does not match mesh vertex list.");
                return;
            }

            DA.SetData(0, mesh.Offset(distance));
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
            get { return new Guid("{85e0e518-b055-48bd-986b-3f51e0846a36}"); }
        }
    }
}