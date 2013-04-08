using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

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
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "Input surface", GH_ParamAccess.item);
            pManager.AddNumberParameter("Length", "L", "Length of mesh edges", GH_ParamAccess.item, 3.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new MeshParam(), "Mesh", "M", "Quad mesh (Chebychev net)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Surface S = null;
            if (!DA.GetData(0, ref S)) { return; }

            double R = Rhino.RhinoMath.UnsetValue;
            if (!DA.GetData(1, ref R)) { return; }

            if (R <= 0) {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mesh edge length must be a positive, non-zero number.");
                return;
            }

            // Starting point
            Point3d c = S.PointAt(S.Domain(0).Mid, S.Domain(1).Mid);

            // Two curves (isoparametric curves, in this case)
            Curve x = S.IsoCurve(0, S.Domain(1).Mid);
            Curve y = S.IsoCurve(1, S.Domain(0).Mid);

            // Initial sphere (at starting point)
            Sphere sph = new Sphere(c, R);
            Surface srf = NurbsSurface.CreateFromSphere(sph);

            // Intersect sphere with the two curves
            Rhino.Geometry.Intersect.CurveIntersections cix =
              Rhino.Geometry.Intersect.Intersection.CurveSurface(x, srf, 0.01, 0.01);
            Rhino.Geometry.Intersect.CurveIntersections ciy =
              Rhino.Geometry.Intersect.Intersection.CurveSurface(y, srf, 0.01, 0.01);

            Point3d[] four = new Point3d[4];
            if (cix.Count != 2 && ciy.Count != 2) { return; }
            for (int i = 0; i < 2; i++)
            {
                if (cix[i].IsPoint && ciy[i].IsPoint)
                {
                    four[2 * i] = cix[i].PointA;
                    four[2 * i + 1] = ciy[i].PointA;
                }
                else
                {
                    return;
                }
            }




            List<Point3d>[] axis = new List<Point3d>[4];
            Curve[] xy = new Curve[] { x, y };
            for (int i = 0; i < 4; i++)
            {
                List<Point3d> pts = new List<Point3d>();
                //
                Point3d pt = four[i]; // set marker at initial point
                pts.Add(c); // add center point
                Boolean exit = false;
                for (int j = 0; j < 5000; j++) // using a hard limit to prevent infinite looping if something goes wrong
                {
                    pts.Add(pt);
                    Surface sphr = NurbsSurface.CreateFromSphere(new Sphere(pt, R));
                    Rhino.Geometry.Intersect.CurveIntersections isct =
                      Rhino.Geometry.Intersect.Intersection.CurveSurface(xy[i % 2], sphr, 0.01, 0.01);
                    if (isct.Count > 1)
                        pt = isct[(i - (i % 2)) / 2].PointA; // select the correct intersection point (depending on the relative direction of the curve)
                    else
                    {
                        exit = true;
                        break;
                    }
                }
                if (exit == false) // fail fast if the hard limit was hit and alert user
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mesh resolution too high; Try increasing the edge length.");
                    return;
                }
                //
                axis[i] = pts;
            }

            // for each quadrant, build up a matrix of points which can be used to construct a mesh
            List<Point3d> quadrants = new List<Point3d>();
            Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();
            for (int k = 0; k < 4; k++)
            {

                List<List<Point3d>> quart = new List<List<Point3d>>();
                quart.Add(axis[(k + 1) % 4]);
                for (int i = 1; i < axis[k].Count; i++)
                {
                    List<Point3d> pts = new List<Point3d>();
                    Point3d point = axis[k][i];
                    pts.Add(point);
                    for (int j = 1; j < quart[i - 1].Count; j++)
                    {
                        Circle cir;
                        Rhino.Geometry.Intersect.Intersection.SphereSphere(new Sphere(point, R), new Sphere(quart[i - 1][j], R), out cir);
                        if (cir.IsValid)
                        {
                            Rhino.Geometry.Intersect.CurveIntersections cin =
                              Rhino.Geometry.Intersect.Intersection.CurveSurface(NurbsCurve.CreateFromCircle(cir), S, 0.01, 0.01);
                            if (cin.Count > 1)
                            {
                                if ((cin[0].PointA - quart[i - 1][j - 1]).Length < 0.01 * R) // compare with a tolerance, rather than exact comparison
                                    point = cin[1].PointA;
                                else
                                    point = cin[0].PointA;
                                pts.Add(point);
                                quadrants.Add(point);
                            }
                        }
                    }
                    quart.Add(pts);
                }
                // use QUART to mesh this quadrant (join later)
                Rhino.Geometry.Mesh qmesh = new Rhino.Geometry.Mesh();
                int[][] vindex = new int[quart.Count][];
                int count = 0;
                for (int i = 0; i < quart.Count; i++)
                {
                    vindex[i] = new int[quart[i].Count];
                    for (int j = 0; j < quart[i].Count; j++)
                    {
                        qmesh.Vertices.Add(quart[i][j]);
                        vindex[i][j] = count;
                        if (i > 0 && j > 0)
                            qmesh.Faces.AddFace(
                              vindex[i][j],
                              vindex[i - 1][j],
                              vindex[i - 1][j - 1],
                              vindex[i][j - 1]);
                        count++;
                    }
                }
                mesh.Append(qmesh);
            }

            // mesh housekeeping
            mesh.Weld(Math.PI);
            mesh.Compact();
            mesh.Normals.ComputeNormals();

            /*
            // flatten axis pts
            List<Point3d> a = new List<Point3d>();
            for (int i = 0; i < 4; i++)
            {
                a.AddRange(axis[i]);
            }
            a.AddRange(quadrants);
            */

            DA.SetData(0, mesh);
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
    }
}