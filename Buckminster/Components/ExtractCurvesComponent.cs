using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Buckminster.Components
{
    public class ExtractCurvesComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ExtractCurvesComponent class.
        /// </summary>
        public ExtractCurvesComponent()
            : base("Buckminster's Extract curves from quad mesh", "Extract",
                "Extract warp/weft curves from a quad mesh",
                "Buckminster", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Input quad mesh", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Passthrough mesh", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Warp/Weft Indicator", "W", "A list corresponding to the input mesh's topology edges: 0 - warp; 1 - weft.", GH_ParamAccess.list);
            pManager.AddLineParameter("Warp", "X", "A list of warp edges", GH_ParamAccess.list);
            pManager.AddLineParameter("Weft", "Y", "A list of weft edges", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Rhino.Geometry.Mesh M = null;
            if (!DA.GetData(0, ref M)) { return; }

            // Check that the mesh is valid, weld all edges, recalculate vertex normals
            if (!M.IsValid) { return; }
            if (M.Faces.QuadCount != M.Faces.Count)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Are you sure this is a quad mesh?");
                return;
            }

            /*M.Weld(Math.PI);
            M.Compact();
            M.UnifyNormals();
            M.Normals.ComputeNormals();*/

            // This list will store the warp/weft of each edge in the mesh.
            // direction array: 0 - warp, 1 - weft, -1 - not yet determined
            List<int> dir = new List<int>(M.TopologyEdges.Count);
            // initialise
            for (int i = 0; i < M.TopologyEdges.Count; i++)
            {
                dir.Add(-1);
            }

            // ------------------------------------------------------------- //
            // As a starting point, we need to define the edges of one face. //
            // ------------------------------------------------------------- //

            // pick a face
            int face = 0;
            // get edges
            int[] edges = M.TopologyEdges.GetEdgesForFace(face);

            // add first edges
            dir[edges[0]] = 1;
            dir[edges[1]] = 0;
            dir[edges[2]] = 1;
            dir[edges[3]] = 0;

            // -------------------------------------------------------------------------- //
            // Now we search through the rest of the faces and if one of the "face edges" //
            // has already been defined we can define the other 3!                        //
            // -------------------------------------------------------------------------- //

            // While we have undefined edges...
            while (dir.Contains(-1))
            {
                // Search through all edges (except the first which was defined above)...
                for (int i = 1; i < M.Faces.Count; i++)
                {
                    int found = -1;
                    // Get face edges
                    edges = M.TopologyEdges.GetEdgesForFace(i);
                    int count;
                    // Search face edges for a defined edge
                    for (count = 0; count < 4; count++)
                    {
                        if (dir[edges[count]] != -1)
                        {
                            // We've found a defined edge! (edge index = edges[count])
                            found = dir[edges[count]]; // store the type of edge (warp or weft)
                            break;
                        }
                    }
                    // Starting from the edge we found, define all the edges in the face
                    if (found == 0) // we found a warp edges, start with that
                    {
                        dir[edges[(count + 0) % 4]] = 0;
                        dir[edges[(count + 1) % 4]] = 1;
                        dir[edges[(count + 2) % 4]] = 0;
                        dir[edges[(count + 3) % 4]] = 1;
                    }
                    else if (found == 1) // we found a weft edge, start with that
                    {
                        dir[edges[(count + 0) % 4]] = 1;
                        dir[edges[(count + 1) % 4]] = 0;
                        dir[edges[(count + 2) % 4]] = 1;
                        dir[edges[(count + 3) % 4]] = 0;
                    }
                }
            }

            // ------------------------------ //
            // Create lines for visual check. //
            // ------------------------------ //

            List<Line> warp = new List<Line>();
            List<Line> weft = new List<Line>();
            for (int i = 0; i < dir.Count; i++)
            {
                if (dir[i] == 0) // warp
                {
                    warp.Add(M.TopologyEdges.EdgeLine(i));
                }
                else // weft
                {
                    weft.Add(M.TopologyEdges.EdgeLine(i));
                }
            }

            // --------------- //
            // Assign outputs. //
            // --------------- //

            DA.SetData(0, M);
            DA.SetDataList(1, dir); // direction array
            DA.SetDataList(2, warp); // warp lines
            DA.SetDataList(3, weft); // weft lines
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
                return Properties.Resources.QuadCurvesComponent;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{25227c6c-38c2-4d44-9620-9a597431778a}"); }
        }
    }
}