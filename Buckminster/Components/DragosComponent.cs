using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Buckminster.Components
{
    public class DragosComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DragosComponent class.
        /// </summary>
        public DragosComponent()
            : base("Create Dragos' gridshell analysis model", "DragosGridshell",
                "Outputs node positions, bars by node IDs and gamma rotations.",
                "Buckminster", "Dragos")
        {
        }

        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.hidden;
            }
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Input quad mesh", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Warp/Weft Indicator", "W", "A list corresponding to the input mesh's topology edges: 0 - warp; 1 - weft.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Offset", "O", "Offset (or timber depth) for each layer", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "N", "Node positions", GH_ParamAccess.list);
            pManager.AddTextParameter("Bars", "B", "Bars by node IDs", GH_ParamAccess.list);
            pManager.AddNumberParameter("Gamma", "G", "Gamma rotations", GH_ParamAccess.list);
            pManager.AddLineParameter("Lines", "L", "Preview model", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Rhino.Geometry.Mesh M = null;
            if (!DA.GetData(0, ref M)) { return; }

            List<int> L = new List<int>();
            if (!DA.GetDataList(1, L)) { return; }

            List<double> offset = new List<double>();
            if (!DA.GetDataList(2, offset)) { return; }

            // Check that the mesh is valid, weld all edges, recalculate vertex normals
            if (!M.IsValid) { return; }
            /*M.Weld(Math.PI); // Not compatible with Rhino 4!
            M.Compact();
            M.UnifyNormals();
            M.Flip(true, true, true);*/
            M.Normals.ComputeNormals();
            //M.Normals.UnitizeNormals();

            int n = M.TopologyVertices.Count;
            int m = M.TopologyEdges.Count;

            List<Point3f[]> layers = new List<Point3f[]>();
            List<string> bars = new List<string>();

            // Add dummy zero offset
            offset.Insert(0, 0);

            // for each layer, add two center line "layers"
            // (note: the first layer of nodes will be "on" the surface of the mesh, otherwise remove "- 0.5 * offset[1]"!)
            for (int i = 1; i < offset.Count; i++)
            {
                Point3f[] l0 = new Point3f[n];
                Point3f[] l1 = new Point3f[n];
                for (int j = 0; j < n; j++)
                {
                    int vertex = M.TopologyVertices.MeshVertexIndices(j)[0];
                    l0[j] = M.TopologyVertices[j] + (float)(2 * offset[i - 1] + 0.5 * offset[i] - 0.5 * offset[1]) * M.Normals[vertex];
                    l1[j] = M.TopologyVertices[j] + (float)(2 * offset[i - 1] + 1.5 * offset[i] - 0.5 * offset[1]) * M.Normals[vertex];
                }
                layers.Add(l0);
                layers.Add(l1);
            }

            // Remove dummy zero offset
            offset.RemoveAt(0);

            // Bars - for each, list by 'line' and by start/end node indices (and calculate gamma angle)
            List<Line> lines = new List<Line>();
            double[] gamma = new double[m];
            for (int i = 0; i < m; i++)
            {
                Rhino.IndexPair se = M.TopologyEdges.GetTopologyVertices(i);

                // ------------------------------------------------------------------------------
                // GAMMA
                Vector3d[] normals = new Vector3d[]{
                    M.Normals[M.TopologyVertices.MeshVertexIndices(se.I)[0]],
                    M.Normals[M.TopologyVertices.MeshVertexIndices(se.J)[0]]};
                Vector3d avg = normals[0] + normals[1];
                Plane vertical = new Plane(M.TopologyVertices[se.I], M.TopologyVertices[se.J],
                  M.TopologyVertices[se.J] + Vector3f.ZAxis);
                Plane bar = new Plane(M.TopologyVertices[se.I], M.TopologyVertices[se.J],
                  new Point3d(M.TopologyVertices[se.J]) + avg);
                gamma[i] = (180 * (Vector3d.VectorAngle(vertical.Normal, bar.Normal))) / Math.PI;
                gamma[i] *= Vector3d.CrossProduct(vertical.Normal, bar.Normal).IsParallelTo(M.TopologyVertices[se.J] - M.TopologyVertices[se.I]);
                // ------------------------------------------------------------------------------

                if (L[i] == 0)
                { // WARP, add to layers 0, 2, 4, ...
                    for (int j = 0; j < offset.Count; j++)
                    {
                        lines.Add(new Line(layers[2 * j][se.I], layers[2 * j][se.J]));
                        bars.Add(string.Format("{0,6} {1,6}", (2 * j) * n + se.I + 1, (2 * j) * n + se.J + 1));
                    }
                }
                else
                { // WEFT, add to layers 1, 3, 5, ...
                    for (int j = 0; j < offset.Count; j++)
                    {
                        lines.Add(new Line(layers[2 * j + 1][se.I], layers[2 * j + 1][se.J]));
                        bars.Add(string.Format("{0,6} {1,6}", (2 * j + 1) * n + se.I + 1, (2 * j + 1) * n + se.J + 1));
                    }
                }
            }
            // Links
            for (int i = 0; i < M.TopologyVertices.Count; i++)
            {
                for (int j = 0; j < layers.Count - 1; j++)
                {
                    lines.Add(new Line(layers[j][i], layers[j + 1][i]));
                    bars.Add(string.Format("{0,6} {1,6}", (j) * n + i + 1, (j + 1) * n + i + 1));
                }
            }

            // Nodes - list all by coordinates
            List<Point3f> nodes = new List<Point3f>();
            for (int i = 0; i < layers.Count; i++)
            {
                nodes.AddRange(layers[i]);
            }

            DA.SetDataList(0, nodes);
            DA.SetDataList(1, bars);
            DA.SetDataList(2, gamma);
            DA.SetDataList(3, lines);
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
            get { return new Guid("{8b32ae16-703e-499a-beb7-77478eb5eaac}"); }
        }
    }
}