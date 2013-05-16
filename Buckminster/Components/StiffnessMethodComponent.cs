using System;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Buckminster.Components
{
    public class StiffnessMethodComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the StiffnessMethodComponent class.
        /// </summary>
        public StiffnessMethodComponent()
            : base("Buckminster's Stiffness Method", "Analysis",
                "Determines internal forces in the structure using the stiffness method.",
                "Buckminster", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "N", "Nodes as points", GH_ParamAccess.list);
            //pManager.AddIntegerParameter("Bars", "B", "Bars by node indices (start/end)", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("Bars", "B1", "Bars by node indices (start)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Bars", "B2", "Bars by node indices (end)", GH_ParamAccess.list);
            pManager.AddVectorParameter("Supports", "S", "Nodal support conditions, represented as a vector", GH_ParamAccess.list);
            pManager.AddVectorParameter("Loads", "L", "Nodal load conditions, represented as a vector", GH_ParamAccess.list);
            pManager.AddNumberParameter("Materials", "P", "Bar properties", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("Output", "out", "Output for debugging", GH_ParamAccess.list);
            pManager.AddVectorParameter("Displacements", "displacements", "Displacements of nodes under load", GH_ParamAccess.list);
            pManager.AddNumberParameter("Forces", "forces", "Axial bar forces", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> nodes = new List<Point3d>();
            if (!DA.GetDataList<Point3d>(0, nodes)) { return; }

            List<int> bars_s = new List<int>();
            if (!DA.GetDataList<int>(1, bars_s)) { return; }

            List<int> bars_e = new List<int>();
            if (!DA.GetDataList<int>(2, bars_e)) { return; }

            List<Vector3d> supports = new List<Vector3d>();
            if (!DA.GetDataList<Vector3d>(3, supports)) { return; }

            List<Vector3d> loads = new List<Vector3d>();
            if (!DA.GetDataList<Vector3d>(4, loads)) { return; }

            List<Double> properties = new List<Double>();
            if (!DA.GetDataList<Double>(5, properties)) { return; }
            
            //List<string> output = new List<string>();

            Vector3d[] displacements;
            double[] forces;
            if (!Buckminster.Analysis.StiffnessMethod(nodes, bars_s, bars_e, properties, loads, supports, out displacements, out forces))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Analysis failed - check the lengths of your input lists.");
                return;
            }

            DA.SetDataList(0, displacements);
            DA.SetDataList(1, forces);

            //DA.SetDataList(0, output);
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
            get { return new Guid("{982cba62-2452-45c3-beab-1340ee477283}"); }
        }
    }
}