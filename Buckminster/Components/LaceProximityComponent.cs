using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Geometry;
using Rhino.Geometry;

using Buckminster.Types;
using Mesh = Buckminster.Types.Mesh;

namespace Buckminster.Components
{
    public class LaceProximityComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the LaceProximityComponent class.
        /// </summary>
        public LaceProximityComponent()
            : base("Buckminster's Lace Proximity", "Lace",
                "Lace one mesh to another by proximity of vertices.",
                "Buckminster", "Lace")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Mesh", "M1", "A manifold mesh.", GH_ParamAccess.item);
            pManager.AddParameter(new MeshParam(), "Another Mesh", "M2", "Another manifold mesh.", GH_ParamAccess.item);
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
            Mesh mesh1, mesh2;
            mesh1 = mesh2 = null;
            double max_distance = Rhino.RhinoMath.UnsetValue;

            if (!DA.GetData<Mesh>(0, ref mesh1)) return;
            if (!DA.GetData<Mesh>(1, ref mesh2)) return;
            if (!DA.GetData<double>(2, ref max_distance)) return;

            var molecular = new Molecular(mesh1);
            // create molecular structure for second mesh and add into first
            molecular.Append(new Molecular(mesh2));
            //var temp = new Molecular(mesh2);
            //molecular.listVertexes.AddRange(temp.listVertexes);
            //molecular.listEdges.AddRange(temp.listEdges);

            var points = molecular.listVertexes.Select(v => v.Coord);

            var node3List = new Node3List(points);
            Node3Tree node3Tree = node3List.CreateTree(0.0, false, 10);
            if (node3Tree == null) return;

            int max_results = molecular.listVertexes.Count - 1;
            double min_distance = 0;

            int n = mesh1.Vertices.Count;
            for (int i = 0; i < n; i++)
            {
                Node3Proximity node3Proximity = new Node3Proximity(node3List[i], i, max_results, min_distance, max_distance);
                node3Tree.SolveProximity(node3Proximity);
                foreach (var j in node3Proximity.IndexList)
                {
                    if (j >= n) molecular.NewEdge(molecular.listVertexes[i], molecular.listVertexes[j]);
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
            get { return new Guid("{9b515a1d-9dfc-4241-bfdc-eed2b2a44d4d}"); }
        }
    }
}