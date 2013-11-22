using System;
using System.Collections.Generic;
//using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Mesh = Buckminster.Types.Mesh;

namespace Buckminster.Components
{
    public class DeconstructMeshComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public DeconstructMeshComponent()
            : base("Buckminster's Deconstruct Mesh", "Parts",
                "Deconstruct mesh into its component parts.",
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
            pManager.AddPointParameter("Vertices", "V", "Mesh vertices", GH_ParamAccess.list);
            pManager.AddVectorParameter("Normals", "N", "Mesh vertex normals", GH_ParamAccess.list);
            pManager.HideParameter(1);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            if (!DA.GetData(0, ref mesh)) { return; }

            int n = mesh.Vertices.Count;
            var points = new Point3d[n];
            var normals = new Vector3d[n];
            for (int i = 0; i < n; i++)
            {
                points[i] = mesh.Vertices[i].Position;
                normals[i] = mesh.Vertices[i].Normal;
            }

            DA.SetDataList(0, points);
            DA.SetDataList(1, normals);

            //DA.SetDataList(0, mesh.Vertices.Select(v => v.Position));
            //DA.SetDataList(1, mesh.Vertices.Select(v => v.Normal));
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
            get { return new Guid("{a89d2669-a60a-4957-b332-8c9468d7295f}"); }
        }
    }
}