using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Buckminster.Components
{
    public class WriteRobotSTRComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the WriteRobotSTRComponent class.
        /// </summary>
        public WriteRobotSTRComponent()
            : base("Simple write Robot STR", "RobotSTR",
                "Write an STR file to be loaded into Robot.",
                "Buckminster", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "N", "Node positions", GH_ParamAccess.list);
            pManager.AddTextParameter("Bars", "B", "Bars by node IDs", GH_ParamAccess.list);
            pManager.AddNumberParameter("Gamma", "G", "Gamma rotations", GH_ParamAccess.list);
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Robot STR file", "STR", "Robot STR file as a string", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> N = new List<Point3d>();
            if (!DA.GetDataList(0, N)) { return; }

            List<string> B = new List<string>();
            if (!DA.GetDataList(1, B)) { return; }

            List<double> G = new List<double>();
            DA.GetDataList(2, G);


            List<string> txt = new List<string>();
            txt.Add(string.Format("ROBOT97\n\nFRAme SPAce\n\nNUMbering DIScontinuous\n\nNODes {0}  ELEments {1}\n\nUNIts\nLENgth = m	Force = kN", N.Count, B.Count));
            txt.Add("\nNODes\n;+------+-------------------+--------------------+--------------------+\n;! No.  !        X          !         Y          !         Z          !\n;+------+-------------------+--------------------+--------------------+\n");
            for (int i = 0; i < N.Count; i++)
            {
                txt.Add(string.Format("{0,-7} {1,10:F6} {2,20:F6} {3,20:F6}", i + 1, N[i].X, N[i].Y, N[i].Z));
            }
            txt.Add("\nELEments\n;+------+-------+-------+\n;! No.  ! STRT  ! END   !\n;+------+-------+-------+\n");
            for (int i = 0; i < B.Count; i++)
            {
                txt.Add(string.Format("{0,-7} {1}", i + 1, B[i]));
            }
            txt.Add("\nPROperties");
            
            if (G != null)
            {
                for (int i = 0; i < G.Count; i++)
                {
                    txt.Add(string.Format("{0} {1} GAmma = {2}", (2 * i - 1) + 1, (2 * i) + 1, G[i]));
                }
            }
            
            txt.Add("\nEND");


            DA.SetDataList(0, txt);
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
                return Properties.Resources.STRComponent;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{837f50ae-19d3-4ece-bef2-6c19137bd7be}"); }
        }
    }
}