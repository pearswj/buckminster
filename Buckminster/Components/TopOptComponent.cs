using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Buckminster.Types;
using Mesh = Buckminster.Types.Mesh;

namespace Buckminster.Components
{
    public class TopOptComponent : GH_Component
    {
        private enum Mode
        {
            None,
            FullyConnected,
            MemberAdding
        }
        private Mode m_mode;
        private Molecular theWorld;

        /// <summary>
        /// Initializes a new instance of the TopOptComponent class.
        /// </summary>
        public TopOptComponent()
            : base("Buckminster's Topology Optimisation", "TopOpt",
                "Sheffield layout optimisation.",
                "Buckminster", "Analysis")
        {
            m_mode = Mode.None;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Mesh", "Mesh", "Input mesh", GH_ParamAccess.item);
            pManager.AddVectorParameter("Fixities", "Fixities", "Nodal support conditions, represented as a vector (0: fixed, 1: free)", GH_ParamAccess.list);
            pManager.AddVectorParameter("Forces", "Forces", "Nodal load conditions, represented as a vector", GH_ParamAccess.list);
            //pManager.AddIntegerParameter("IterLimit", "IterLimit", "IterLimit", GH_ParamAccess.item, 10);
            //pManager.HideParameter(3);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Volume", "Volume", "Volume", GH_ParamAccess.item);
            pManager.AddLineParameter("Bars", "Bars", "Bars", GH_ParamAccess.list);
            pManager.AddNumberParameter("Radii", "Radii", "Radii", GH_ParamAccess.list);
            pManager.AddColourParameter("Colours", "Colours", "Colours", GH_ParamAccess.list);
            pManager.AddVectorParameter("Displacements", "Displacements", "Displacements", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (DA.Iteration == 0) this.ValuesChanged();

            // Collect inputs
            Mesh mesh = null;
            List<Vector3d> fixities = new List<Vector3d>();
            List<Vector3d> forces = new List<Vector3d>();
            bool reset = true;

            if (!DA.GetData(0, ref mesh)) return;
            if (!DA.GetDataList<Vector3d>(1, fixities)) return;
            if (!DA.GetDataList<Vector3d>(2, forces)) return;
            if (!DA.GetData<bool>(3, ref reset)) return;

            if (reset) // Rebuild model from external source
            {
                // Create molecular structure
                theWorld = new Molecular(mesh.Vertices.Count);

                Dictionary<string, int> vlookup = new Dictionary<string, int>();
                for (int i = 0; i < mesh.Vertices.Count; i++)
                    vlookup.Add(mesh.Vertices[i].Name, i);

                // add nodes
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    Buckminster.Types.Molecular.Node vertex = theWorld.NewVertex(mesh.Vertices[i].Position);
                    vertex.Fixity = new Buckminster.Types.Molecular.Constraint(fixities[i]);
                    vertex.Force = new Vector3d(forces[i]);
                }

                // add bars
                if (m_mode == Mode.FullyConnected) // discard mesh edges and used a fully-connected ground-structure
                {
                    for (int i = 0; i < mesh.Vertices.Count; i++)
                        for (int j = i + 1; j < mesh.Vertices.Count; j++)
                            theWorld.NewEdge(theWorld.listVertexes[i], theWorld.listVertexes[j]);
                }
                else // use edges from mesh
                {
                    foreach (var edge in mesh.Halfedges.GetUnique())
                    {
                        Buckminster.Types.Molecular.Node end = theWorld.listVertexes[vlookup[edge.Vertex.Name]];
                        Buckminster.Types.Molecular.Node start = theWorld.listVertexes[vlookup[edge.Prev.Vertex.Name]];
                        theWorld.NewEdge(start, end);
                    }
                }

                TopOpt.SetWorld(theWorld, 1, 1, 0);
            }
            
            // solve
            if (m_mode == Mode.MemberAdding)
            {
                TopOpt.AddEdges(0.1, 0);
                //if (TopOpt.MembersAdded == 0) return;
                // note: first time member adding is called, reset is still true so world
                // is rebuilt and all displacements get reset (hence no members added)
            }
            TopOpt.SolveProblem();
            //TopOpt.RemoveUnstressed(1E-6);
            // Don't remove unstressed bars, just don't show them! (See below.)

            DA.SetData(0, TopOpt.Volume);
            var subset = theWorld.listEdges.Where(e => e.Radius > 1E-6);
            DA.SetDataList(1, subset.Select(e => new Line(e.StartVertex.Coord, e.EndVertex.Coord)));
            DA.SetDataList(2, subset.Select(e => e.Radius));
            DA.SetDataList(3, subset.Select(e => e.Colour));
            DA.SetDataList(4, theWorld.listVertexes.Select(v => v.Velocity));
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
            get { return new Guid("{2622766f-eb60-4a9c-ba82-7ce226866ec7}"); }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            foreach (var edge in theWorld.listEdges)
	        {
                if (edge.Radius > 1E-6) // don't draw unstressed
                {
                    System.Drawing.Color colour = this.Attributes.Selected ? args.WireColour_Selected : edge.Colour;
                    var thickness = (int)Math.Floor(edge.Radius * 1000);
                    if (thickness < args.DefaultCurveThickness) thickness = args.DefaultCurveThickness;
                    args.Display.DrawLine(edge.StartVertex.Coord, edge.EndVertex.Coord, colour, thickness);
                }
            }
        }

        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            ToolStripMenuItem toolStripMenuItem1 = Menu_AppendItem(menu, "Fully-Connected", new EventHandler(this.Menu_FullyConnectedClicked), true, m_mode == Mode.FullyConnected);
            ToolStripMenuItem toolStripMenuItem2 = Menu_AppendItem(menu, "Member-Adding", new EventHandler(this.Menu_MemberAddingClicked), true, m_mode == Mode.MemberAdding);
            toolStripMenuItem1.ToolTipText = "Discard mesh-edges and use a fully-connected ground-structure (slow).";
            toolStripMenuItem2.ToolTipText = "Use the member-adding algorithm (fast).";
        }

        private void Menu_FullyConnectedClicked(Object sender, EventArgs e)
        {
            RecordUndoEvent("Toggle TopOpt Mode");
            if (m_mode == Mode.FullyConnected)
                m_mode = Mode.None;
            else
                m_mode = Mode.FullyConnected;
            ExpireSolution(true);
        }

        private void Menu_MemberAddingClicked(Object sender, EventArgs e)
        {
            RecordUndoEvent("Toggle TopOpt Mode");
            if (m_mode == Mode.MemberAdding)
                m_mode = Mode.None;
            else
                m_mode = Mode.MemberAdding;
            ExpireSolution(true);
        }

        protected override void ValuesChanged()
        {
            switch (m_mode)
            {
                case Mode.None:
                    this.Message = null;
                    break;
                case Mode.FullyConnected:
                    this.Message = "Fully-Connected";
                    break;
                case Mode.MemberAdding:
                    this.Message = "Member-Adding";
                    break;
            }
        }
    }
}