using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Geometry;
using Rhino.Geometry;

using Buckminster.Types;
using Mesh = Buckminster.Types.Mesh;

using Molecular = SharpSLO.Types.Molecular;

namespace Buckminster.Components
{
    public class GridProximityComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GridProximityComponent class.
        /// </summary>
        public GridProximityComponent()
            : base("Buckminster's Grid Proximity", "Grid",
                "Re-lace mesh by connecting vertices within a specified distance of eachother."
                + System.Environment.NewLine + "Returns a Molecular structure (a non-manifold mesh without faces).",
                "Buckminster", "Lace")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Mesh", "M", "A manifold mesh.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "D", "Maximum length of connections.", GH_ParamAccess.item, 1.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new MolecularParam(), "Space Truss", "T", "A 3D space truss.", GH_ParamAccess.item);
            pManager.AddPointParameter("Nodes", "V", "Use these node coordinates to define boundary conditions.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            double max_distance = Rhino.RhinoMath.UnsetValue;

            if (!DA.GetData<Mesh>(0, ref mesh)) return;
            if (!DA.GetData<double>(1, ref max_distance)) return;

            int n = mesh.Vertices.Count;

            var molecular = new Molecular(n);
            var points = mesh.Vertices.Select(v => (Point3d)v.Position);
            foreach (var pt in points)
            {
                molecular.Add(pt.X, pt.Y, pt.Z);
            }

            var node3List = new Node3List(points);
            Node3Tree node3Tree = node3List.CreateTree(0.0, false, 10);
            if (node3Tree == null) return;

            int max_results = n - 1;
            double min_distance = 0;

            for (int i = 0; i < n; i++)
            {
                Node3Proximity node3Proximity = new Node3Proximity(node3List[i], i, max_results, min_distance, max_distance);
                node3Tree.SolveProximity(node3Proximity);
                foreach (var j in node3Proximity.IndexList)
                {
                    molecular.Add(i, j);
                }
            }

            DA.SetData(0, molecular);
            DA.SetDataList(1, points);
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
            get { return new Guid("{a59e634b-a8c6-4679-bec5-553c65e91066}"); }
        }
    }
}