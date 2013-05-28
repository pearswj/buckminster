using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

using System.Windows.Forms;

namespace Buckminster.Components
{
    public class ChebychevComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ChebychevComponent()
            : base("Buckminster's Chebychev Net", "Chebychev",
                "Constructs a quad mesh from a NURBS surface using the Chebychev net method.",
                "Buckminster", "Utils")
        {
            this.ValuesChanged();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Input surface", GH_ParamAccess.item);
            pManager.AddPointParameter("Point", "P", "Starting point (Optional: If none provided, a point in the middle of the parameter space will selected.)", GH_ParamAccess.item);
            pManager[1].Optional = true;
            pManager.AddNumberParameter("Length", "L", "Length of mesh edges", GH_ParamAccess.item, 3.0);
            pManager.AddNumberParameter("Rotation", "R", "Angle of rotation for grid (degrees)", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter("Steps", "B", "Maximum number of steps to walk out from the starting point", GH_ParamAccess.item, 1000);
            pManager.AddBooleanParameter("Extend", "E", "Extend the surface beyond its original boundaries", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Quad mesh (Chebychev net)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Surface S = null;
            if (!DA.GetData(0, ref S)) { return; }

            Point3d P = Point3d.Unset;
            if (!DA.GetData(1, ref P))
            {
                P = S.PointAt(S.Domain(0).Mid, S.Domain(1).Mid);
            }

            double R = Rhino.RhinoMath.UnsetValue;
            if (!DA.GetData(2, ref R)) { return; }

            double A = Rhino.RhinoMath.UnsetValue;
            if (!DA.GetData(3, ref A)) { return; }

            int max = 0;
            if (!DA.GetData(4, ref max)) { return; }

            Boolean extend = false;
            if (!DA.GetData(5, ref extend)) { return; }

            if (R <= 0) {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mesh edge length must be a positive, non-zero number.");
                return;
            }

            // Extend surface beyond boundaries to get a better coverage from the net
            if (extend)
            {
                S = S.Extend(IsoStatus.North, R, true);
                S = S.Extend(IsoStatus.East, R, true);
                S = S.Extend(IsoStatus.South, R, true);
                S = S.Extend(IsoStatus.West, R, true);
            }


            // starting point
            double u0, v0;
            S.ClosestPoint(P, out u0, out v0);
            // get two (four) orthogonal directions (in plane of surface at starting point)
            Plane plane = new Plane(S.PointAt(u0, v0), S.NormalAt(u0, v0));
            plane.Rotate(Rhino.RhinoMath.ToRadians(A), S.NormalAt(u0, v0));
            Vector3d[] dir = new Vector3d[]{
                plane.XAxis * R,
                plane.YAxis * R,
                plane.XAxis * -R,
                plane.YAxis * -R
                };


            // for each direction, walk out (and store list of points)
            double u, v;
            List<Point3d>[] axis = new List<Point3d>[4];
            for (int i = 0; i < 4; i++)
            {
                // set u and v to starting point
                u = u0;
                v = v0;
                List<Point3d> pts = new List<Point3d>();
                for (int j = 0; j < max + 1; j++)
                {
                    // get point and normal for uv
                    Point3d pt = S.PointAt(u, v);
                    Vector3d n = S.NormalAt(u, v);
                    n *= R;
                    // add point to list
                    pts.Add(pt);
                    // create forward facing arc and find intersection point with surface (as uv)
                    Arc arc = new Arc(pt + n, pt + dir[i], pt - n);
                    CurveIntersections isct =
                      Intersection.CurveSurface(arc.ToNurbsCurve(), S, 0.01, 0.01);
                    if (isct.Count > 0)
                        isct[0].SurfacePointParameter(out u, out v);
                    else
                        break;
                    // adjust direction vector (new position - old position)
                    dir[i] = S.PointAt(u, v) - pt;
                }
                axis[i] = pts;
            }


            // now that we have the axes, start to build up the mesh quads in between
            GH_PreviewUtil preview = new GH_PreviewUtil(GetValue("Animate", false));
            Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh(); // target mesh
            for (int k = 0; k < 4; k++)
            {
                int k0 = (k + 1) % 4;
                int padding = 10;
                Rhino.Geometry.Mesh qmesh = new Rhino.Geometry.Mesh(); // local mesh for quadrant
                Point3d[,] quad = new Point3d[axis[k].Count + padding, axis[k0].Count + padding]; // 2d array of points
                int[,] qindex = new int[axis[k].Count + padding, axis[k0].Count + padding]; // 2d array of points' indices in local mesh
                int count = 0;
                for (int i = 0; i < axis[k0].Count; i++)
                {
                    // add axis vertex to mesh and store point and index in corresponding 2d arrays
                    quad[0, i] = axis[k0][i];
                    qmesh.Vertices.Add(axis[k0][i]);
                    qindex[0, i] = count++;
                }

                for (int i = 1; i < quad.GetLength(0); i++)
                {
                    if (i < axis[k].Count)
                    {
                        // add axis vertex
                        quad[i, 0] = axis[k][i];
                        qmesh.Vertices.Add(axis[k][i]);
                        qindex[i, 0] = count++;
                    }
                    // for each column attempt to locate a new vertex in the grid
                    for (int j = 1; j < quad.GetLength(1); j++)
                    {
                        // if quad[i - 1, j] doesn't exist, try to add it and continue (or else break the current row)
                        if (quad[i - 1, j] == new Point3d())
                        {
                            if (j < 2) { break; }
                            CurveIntersections isct = this.ArcIntersect(S, quad[i - 1, j - 1], quad[i - 1, j - 2], R);
                            if (isct.Count > 0)
                            {
                                quad[i - 1, j] = isct[0].PointB;
                                qmesh.Vertices.Add(quad[i - 1, j]);
                                qindex[i - 1, j] = count++;
                            }
                            else
                                break;
                        }
                        // if quad[i, j - 1] doesn't exist, try to create quad[i, j] by projection and skip mesh face creation
                        if (quad[i, j - 1] == new Point3d())
                        {
                            if (i < 2) { break; }
                            CurveIntersections isct = this.ArcIntersect(S, quad[i - 1, j], quad[i - 2, j], R);
                            if (isct.Count > 0)
                            {
                                quad[i, j] = isct[0].PointB;
                                qmesh.Vertices.Add(quad[i, j]);
                                qindex[i, j] = count++;
                                continue;
                            }
                        }
                        // construct a sphere at each neighbouring vertex ([i,j-1] and [i-1,j]) and intersect
                        Sphere sph1 = new Sphere(quad[i, j - 1], R);
                        Sphere sph2 = new Sphere(quad[i - 1, j], R);
                        Circle cir;
                        if (Intersection.SphereSphere(sph1, sph2, out cir) == SphereSphereIntersection.Circle)
                        {
                            // intersect circle with surface
                            CurveIntersections cin =
                              Intersection.CurveSurface(NurbsCurve.CreateFromCircle(cir), S, 0.01, 0.01);
                            // attempt to find the new vertex (i.e not [i-1,j-1])
                            foreach(IntersectionEvent ie in cin)
                            {
                                if ((ie.PointA - quad[i - 1, j - 1]).Length > 0.2 * R) // compare with a tolerance, rather than exact comparison
                                {
                                    quad[i, j] = ie.PointA;
                                    qmesh.Vertices.Add(quad[i, j]);
                                    qindex[i, j] = count++;
                                    // create quad-face
                                    qmesh.Faces.AddFace(qindex[i, j], qindex[i - 1, j], qindex[i - 1, j - 1], qindex[i, j - 1]);
                                    break;
                                }
                            }
                            if (preview.Enabled)
                            {
                                preview.Clear();
                                preview.AddMesh(mesh);
                                preview.AddMesh(qmesh);
                                preview.Redraw();
                            }
                        }
                    }
                }
                // add local mesh to target
                mesh.Append(qmesh);
            }

            // weld mesh to remove duplicate vertices along axes
            mesh.Weld(Math.PI);
            mesh.Compact();
            mesh.Normals.ComputeNormals();


            DA.SetData(0, mesh);

            preview.Clear();
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
            get { return new Guid("{2cfe6201-8101-465c-8739-fefe111f66d2}"); }
        }

        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            //base.AppendAdditionalMenuItems(menu);
            ToolStripMenuItem toolStripMenuItem = Menu_AppendItem(menu, "Animate", new EventHandler(this.Menu_AnimateClicked), true, GetValue("Animate", false));
            toolStripMenuItem.ToolTipText = "Preview the mesh construction process.";
        }

        private void Menu_AnimateClicked(Object sender, EventArgs e)
        {
            RecordUndoEvent("AnimateChebychev");
            SetValue("Animate", !this.GetValue("Animate", false));
            ExpireSolution(true);
        }

        protected override void ValuesChanged()
        {
            if (this.GetValue("Animate", false))
                this.Message = "Animate";
            else
                this.Message = null;
        }

        /// <summary>
        /// A helper function to find the next point by walking out in a direction and pivoting down onto the surface.
        /// Similar approach to initial axis generation.
        /// </summary>
        private CurveIntersections ArcIntersect(Surface s, Point3d pt, Point3d pt_prev, double rad)
        {
            double u, v;
            // get uv
            s.ClosestPoint(pt, out u, out v);
            // get normal for uv
            Vector3d n = s.NormalAt(u, v);
            // scale normal by desired length
            n *= rad;
            // create forward facing arc and find intersection point with surface (as uv)
            // use pt_prev to get direction (middle parameter)
            Arc arc = new Arc(pt + n, pt + (pt - pt_prev), pt - n);
            // return intersection(s)
            return Rhino.Geometry.Intersect.Intersection.CurveSurface(arc.ToNurbsCurve(), s, 0.01, 0.01);
        }
    }
}