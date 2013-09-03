using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Buckminster.Components
{
    public class DecomposeMolecularComponent : GH_Component
    {
        Line[] m_lines;
        System.Drawing.Color[] m_colours;
        int[] m_thicknesses;

        /// <summary>
        /// Initializes a new instance of the DecomposeMolecularComponent class.
        /// </summary>
        public DecomposeMolecularComponent()
            : base("Buckminster's Decompose Molecular Component", "DecompMol",
                "Extract bar/node attributes from Molecular data type.",
                "Buckminster", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            var p = new MolecularParam();
            p.Name = "Molecular";
            p.NickName = "M";
            p.Access = GH_ParamAccess.item;
            pManager.AddParameter(p);
            pManager.AddBooleanParameter("Filter", "F", "Filter out unstressed bars", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Lines", "L", "Bars as lines", GH_ParamAccess.list);
            pManager.AddColourParameter("Colours", "C",
                "Bar colours (blue = tension, red = compression, grey = unstressed)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Thicknesses", "T", "Bar thicknesses (scaled, absolute stress values)", GH_ParamAccess.list);
            pManager.AddVectorParameter("Displacements", "D", "Nodal displacements", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Collect inputs
            SharpSLO.Types.Molecular molecular = null;
            bool filter = true;

            if (!DA.GetData(0, ref molecular))
            {
                m_lines = null;
                m_colours = null;
                m_thicknesses = null;
                this.ExpirePreview(true);
                return;
            }
            DA.GetData<bool>(1, ref filter);

            // Filter out unstressed bars
            IEnumerable<int> subset;
            if (filter)
            {
                subset = molecular.Bars.Select((bar, index) => new { Bar = bar, Index = index })
                                       .Where(b => Math.Abs(b.Bar.Stress) > 1E-6)
                                       .Select(b => b.Index);
            }
            else
            {
                subset = Enumerable.Range(0, molecular.Bars.Count);
            }

            m_lines = subset.Select(i => molecular.ToLine(i)).ToArray();
            m_colours = subset.Select(i => molecular.Bars[i].Colour).ToArray();
            m_thicknesses = subset.Select(i => (int)Math.Floor(Math.Abs(molecular.Bars[i].Stress * 5)) + 1).ToArray();

            DA.SetDataList(0, m_lines);
            DA.SetDataList(1, m_colours);
            DA.SetDataList(2, subset.Select(i => Math.Abs(molecular.Bars[i].Stress * 5)).ToArray());
            DA.SetDataList(3, molecular.Nodes.Select(n => (n.Displacement ?? new SharpSLO.Geometry.Vector()).ToVector3d()));
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
            get { return new Guid("{8a71be98-1eb4-442c-9c54-2bae059d1887}"); }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (Hidden) { return; }
            if (Locked) { return; }
            if (m_lines == null || m_colours == null || m_thicknesses == null) { return; }

            // Change colours if selected
            System.Drawing.Color[] colours;
            if (this.Attributes.Selected)
            {
                colours = new System.Drawing.Color[m_lines.Length];
                for (int i = 0; i < m_lines.Length; i++) { colours[i] = args.WireColour_Selected; }
            }
            else { colours = m_colours; }

            // Draw lines
            for (int i = 0; i < m_lines.Length; i++)
            {
                args.Display.DrawLine(m_lines[i], colours[i], m_thicknesses[i]);
            }
        }
    }
}