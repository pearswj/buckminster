using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;

namespace Buckminster.Components
{
    public class ExtractComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ExtractComponent class.
        /// </summary>
        public ExtractComponent()
            : base("Buckminster's Extract Component", "Extract",
                "Extract geometrical and topological information from the mesh.",
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
            pManager.AddPointParameter("Nodes", "N", "Nodes as points", GH_ParamAccess.list);
            //pManager.AddIntegerParameter("Bars", "B1", "Bars by node indices (start)", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Bars", "B1", "Bars by node indices (start)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Bars", "B2", "Bars by node indices (end)", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            if (!DA.GetData(0, ref mesh)) { return; }

            DA.SetDataList(0, mesh.Vertices.Select(v => v.Position));

            //DataTree<int> bars = new DataTree<int>();
            List<Halfedge> unique = mesh.Halfedges.GetUnique();
            var bars_s = new int[unique.Count];
            var bars_e = new int[unique.Count];

            Dictionary<string, int> vlookup = new Dictionary<string, int>();
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                vlookup.Add(mesh.Vertices[i].Name, i);
            }

            for (int i = 0; i < unique.Count; i++)
            {
                //bars.Add(vlookup[unique[i].Vertex.Name], new GH_Path(i));       // start
                //bars.Add(vlookup[unique[i].Prev.Vertex.Name], new GH_Path(i));  // end
                bars_s[i] = vlookup[unique[i].Vertex.Name];
                bars_s[i] = vlookup[unique[i].Prev.Vertex.Name];
            }

            //DA.SetDataTree(1, bars);
            DA.SetDataList(1, bars_s);
            DA.SetDataList(1, bars_e);
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
            get { return new Guid("{4ee31bc1-73c4-4bd7-8932-ff8484282f4e}"); }
        }
    }
}