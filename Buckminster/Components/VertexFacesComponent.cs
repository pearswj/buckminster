using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Buckminster
{
    public class VertexFacesComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the VertexFacesComponent class.
        /// </summary>
        public VertexFacesComponent()
            : base("Buckminster's Vertex Adjacent Faces", "AdjFaces",
                "Finds the mesh faces which border the 'i'th vertex.",
                "Buckminster", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Mesh", "M", "Input mesh", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Vertex Index", "i", "The index of the vertex", GH_ParamAccess.item, 1);
            //pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Adjacent Face Polylines", "A",
                "A list of closed polylines representing the faces adjacent to V", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            if (!DA.GetData(0, ref mesh)) { return; }

            int i = 0; // null?
            if (!DA.GetData<int>(1, ref i)) { return; }
            if (i >= mesh.Vertices.Count) { return; }

            List<Polyline> polylines = new List<Polyline>();
            foreach (Face f in mesh.Vertices[i].GetVertexFaces())
            {
                polylines.Add(f.ToClosedPolyline());
            }

            DA.SetDataList(0, polylines);
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
                return Properties.Resources.AdjFacesComponent;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{baabc0c2-487a-46c2-8cc7-784c1de373a8}"); }
        }
    }
}