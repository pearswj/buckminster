using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Rhino.Geometry;
//using Solver = Google.OrTools.LinearSolver.Solver;

using Buckminster.Types;
//using Mesh = Buckminster.Types.Mesh;

using SharpSLO;

namespace Buckminster.Components
{
    public class SharpSLOComponent : GH_Component
    {
        private enum Mode
        {
            None,
            FullyConnected,
            MemberAdding
        }
        private Mode m_mode;
        private List<string> m_output;
        private bool m_mosek;
        private SharpSLO.Optimiser m_optimiser;
        private double m_runtime;
        private SharpSLO.SolType m_solType;
        private bool m_triggerReset;

        /// <summary>
        /// Initializes a new instance of the TopOptComponent class.
        /// </summary>
        public SharpSLOComponent()
            : base("Buckminster's Topology Optimisation", "SharpSLO",
                "Sheffield layout optimisation.",
                "Buckminster", "Analysis")
        {
            m_mode = Mode.None;
            m_output = new List<string>();
            m_solType = SharpSLO.SolType.Clp;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new MolecularParam(), "Molecular", "Molecular", "Input structure. (Accepts a Rhino mesh, a PolyMesh or a native Molecular data type.)", GH_ParamAccess.item);
            pManager.AddParameter(new MolecularParam(), "Potentials", "PCL", "Potential connections list for member-additive ", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddVectorParameter("Fixities", "Fixities", "Nodal support conditions, represented as a vector (0: fixed, 1: free)", GH_ParamAccess.list);
            pManager.AddVectorParameter("Forces", "Forces", "Nodal load conditions, represented as a vector", GH_ParamAccess.list);
            pManager.AddNumberParameter("Tensile", "-Limit", "Tensile capacity", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Compressive", "+Limit", "Compressive capacity", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Joint cost", "Joint", "Joint cost", GH_ParamAccess.item, 0.0);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output", "Output", "Output", GH_ParamAccess.list);
            pManager.AddNumberParameter("Volume", "Volume", "Volume", GH_ParamAccess.item);
            var p = new MolecularParam();
            p.Name = "Molecular";
            p.NickName = "M";
            p.Access = GH_ParamAccess.item;
            pManager.AddParameter(p);
        }

        /// <summary>
        /// Called before SolveInstance. (Equivalent to DA.Iteration == 0.)
        /// </summary>
        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            this.ValuesChanged();
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Collect inputs
            SharpSLO.Types.Molecular molecular = null;
            SharpSLO.Types.Molecular pcl = null;
            List<Vector3d> fixities = new List<Vector3d>();
            List<Vector3d> forces = new List<Vector3d>();
            double limitT, limitC, jCost;
            limitT = limitC = jCost = double.NaN;
            bool reset = true;

            if (!DA.GetData(0, ref molecular)) return;
            DA.GetData(1, ref pcl); // Optional
            if (!DA.GetDataList<Vector3d>(2, fixities)) return;
            if (!DA.GetDataList<Vector3d>(3, forces)) return;
            if (!DA.GetData<double>(4, ref limitT)) return;
            if (!DA.GetData<double>(5, ref limitC)) return;
            if (!DA.GetData<double>(6, ref jCost)) return;
            if (!DA.GetData<bool>(7, ref reset)) return;

            if (reset || m_triggerReset) // Rebuild model from external source
            {
                var copy = molecular.Duplicate();

                // Add boundary conditions
                for (int i = 0; i < molecular.Nodes.Count; i++)
                {
                    copy.Nodes[i].Fixity = new SharpSLO.Geometry.Vector(fixities[i].X, fixities[i].Y, fixities[i].Z);
                    copy.Nodes[i].Force = new SharpSLO.Geometry.Vector(forces[i].X, forces[i].Y, forces[i].Z);
                }

                if (pcl == null)
                {
                    m_optimiser = new SharpSLO.Optimiser(copy, m_solType);
                }
                else
                {
                    var potentials = pcl.Bars.Select(b => new Tuple<int, int>(b.Start, b.End)).ToArray();
                    m_optimiser = new SharpSLO.Optimiser(copy, potentials, m_solType);
                }

                m_optimiser.TensileStressLimit = limitT;
                m_optimiser.CompressiveStressLimit = limitC;
                m_optimiser.JointCost = jCost;

                if (m_output.Count > 0) m_output.Clear();
                m_runtime = 0.0;
                m_triggerReset = false;
            }
            
            // solve
            int members_added = m_optimiser.SolveStep();
            if (members_added < 0) { return; }

            if (members_added == 0) this.StopTimer(); // Disable timer if solution converges

            m_output.Add(string.Format("{0,3:D}: vol.: {1,9:F6} add. :{2,4:D} ({3,2:F3}s)",
                m_output.Count, m_optimiser.Volume, members_added, m_optimiser.Runtime));

            m_runtime += m_optimiser.Runtime;

            // set outputs
            DA.SetDataList(0, m_output);
            DA.SetData(1, m_optimiser.Volume);
            DA.SetData(2, m_optimiser.GroundStructure);
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
            get { return new Guid("{6bbb5ac2-7ff8-4f64-8d77-a38dda085679}"); }
        }

        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            ToolStripMenuItem toolStripMenuItem;
            toolStripMenuItem = Menu_AppendItem(menu, "GLPK (Google or-tools)", new EventHandler(this.Menu_GLPKClicked), true, m_solType == SharpSLO.SolType.GLPK);
            toolStripMenuItem.ToolTipText = "Use the GLPK solver (provided by Google's or-tools).";
            toolStripMenuItem = Menu_AppendItem(menu, "Clp (Google or-tools)", new EventHandler(this.Menu_ClpClicked), true, m_solType == SharpSLO.SolType.Clp);
            toolStripMenuItem.ToolTipText = "Use the Clp solver (provided by Google's or-tools).";
            toolStripMenuItem = Menu_AppendItem(menu, "Mosek", new EventHandler(this.Menu_MosekClicked), true, m_solType == SharpSLO.SolType.Mosek);
            toolStripMenuItem.ToolTipText = "Use the Mosek solver (if you have it installed).";
        }

        private void Menu_GLPKClicked(Object sender, EventArgs e)
        {
            RecordUndoEvent("GLPK");
            m_solType = SharpSLO.SolType.GLPK;
            m_triggerReset = true;
            ExpireSolution(true);
        }

        private void Menu_ClpClicked(Object sender, EventArgs e)
        {
            RecordUndoEvent("Clp");
            m_solType = SharpSLO.SolType.Clp;
            m_triggerReset = true;
            ExpireSolution(true);
        }
        
        private void Menu_MosekClicked(Object sender, EventArgs e)
        {
            RecordUndoEvent("Mosek");
            m_solType = SharpSLO.SolType.Mosek;
            m_triggerReset = true;
            ExpireSolution(true);
        }

        private bool StopTimer()
        {
            // http://www.grasshopper3d.com/forum/topics/how-to-stop-the-timer-component-in-the-vb-script
            // We need to disable the timer that is associated with this component.
            // First, find the document that contains this component
            GH_Document ghdoc = OnPingDocument();
            if (ghdoc == null) return false;
            // Then, iterate over all objects in the document to find all timers.
            foreach (IGH_DocumentObject docobj in ghdoc.Objects)
            {
                // Try to cast the object to a GH_Timer
                Grasshopper.Kernel.Special.GH_Timer timer = docobj as Grasshopper.Kernel.Special.GH_Timer;
                if (timer == null) continue;
                // If the cast was successful, then see if this component is part of the timer target list.
                if (timer.Targets.Contains(InstanceGuid))
                {
                    // If it is, lock the timer.
                    timer.Locked = true;
                    return timer.Locked;
                }
            }
            return false; // Didn't find a timer attached to this component...
        }
    }
}