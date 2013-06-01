using System;

using Grasshopper.Kernel;
using Buckminster.Types;

namespace Buckminster.Components
{
    public class LaceDualComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the LaceDualComponent class.
        /// </summary>
        public LaceDualComponent()
            : base("Buckminster's Lace Dual", "Lace",
                "Constructs a space truss by lacing between a mesh and its dual.",
                "Buckminster", "Lace")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Original Mesh", "M", "The original mesh", GH_ParamAccess.item);
            pManager.AddParameter(new MeshParam(), "Dual Mesh", "D",
                "This should be the dual of the original mesh and offset from the orginal using Buckminster's Offset component", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddLineParameter("Output Truss", "T", "The space truss (as a list of lines)", GH_ParamAccess.list);
            pManager.AddParameter(new MeshParam(), "Truss Mesh", "T", "A combination of the two input meshes with naked edges connecting corresponding vertices", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            Mesh dual = null;
            if (!DA.GetData<Mesh>(0, ref mesh)) { return; }
            if (!DA.GetData<Mesh>(1, ref dual)) { return; }

            if (!mesh.IsValid) { return; }
            if (!dual.IsValid) { return; }

            // no. faces on original mesh should match no. vertices on dual
            if (mesh.Faces.Count != dual.Vertices.Count)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Face/vertex mismatch. Please connect the original mesh and its dual.");
                return;
            }

            Mesh target = mesh.Duplicate();
            target.Append(dual);

            int n = mesh.Vertices.Count;

            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                foreach (Vertex fv in target.Faces[i].GetVertices())
                {
                    //target.Halfedges.Add(fv, fv.Halfedge.Next, target.Vertices[n + i].Halfedge, null);
                    // Adds a halfedge pair between the vertex on the dual and each corresponding face-vertex
                    // The pair link to each other, forwards and backwards.
                    Halfedge e1 = new Halfedge(fv);
                    Halfedge e2 = new Halfedge(target.Vertices[n + i], e1, e1, null, e1);
                    e1.Next = e2;
                    e1.Prev = e2;
                    e1.Pair = e2;
                    target.Halfedges.Add(e1);
                    target.Halfedges.Add(e2);
                }
            }


            DA.SetData(0, target);
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
            get { return new Guid("{1cfa5569-8d27-40ef-9413-be1fb76e6c32}"); }
        }
    }
}