using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Rhino.Geometry;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Buckminster
{
    [Obsolete("Consider using SharpFE instead.")]
    public class Analysis
    {
        public static bool StiffnessMethod(
            IList<Point3d> nodes,
            IList<int> bars_s, // indices of start nodes
            IList<int> bars_e, // indices of end nodes
            IList<double> bars_mat, // csa, for now
            IList<Vector3d> loads,
            IList<Vector3d> supports_t, // translational fixities
            //IList<Vector3d> supports_r, // rotational fixities (not yet implemented)
            out Vector3d[] deflections,
            out double[] forces
            )
        {
            // TODO: check that loads array isn't being altered

            var num_bars = bars_s.Count;
            var num_nodes = nodes.Count;

            if (num_nodes != loads.Count |
                num_nodes != supports_t.Count |
                num_bars != bars_e.Count |
                num_bars != bars_mat.Count)
            {
                deflections = new Vector3d[num_nodes];
                forces = new double[num_bars];
                return false;
            }

            var YoungsModulus = 205000.0;

            //var matrix = new DenseMatrix(n * 2, n * 2);
            var stiffness_matrix = new double[num_nodes * 3, num_nodes * 3];

            // build stiffness matrix

            var vectors = Enumerable.Range(0, num_bars).Select(i => nodes[bars_e[i]] - nodes[bars_s[i]]).ToArray();

            for (int i = 0; i < num_bars; i++)
            {
                //var vector = nodes[bars_e[i]] - nodes[bars_s[i]];
                var vector = vectors[i];
                var EA_over_Lcubed = YoungsModulus * bars_mat[i] / Math.Pow(vector.Length, 3); // bar_mat is csa
                var xx_stiffness = EA_over_Lcubed * Math.Pow(vector.X, 2);
                var yy_stiffness = EA_over_Lcubed * Math.Pow(vector.Y, 2);
                var zz_stiffness = EA_over_Lcubed * Math.Pow(vector.Z, 2);
                var xy_stiffness = EA_over_Lcubed * vector.X * vector.Y;
                var yz_stiffness = EA_over_Lcubed * vector.Y * vector.Z;
                var zx_stiffness = EA_over_Lcubed * vector.Z * vector.X;

                var start_dof = 3 * bars_s[i];
                var end_dof = 3 * bars_e[i];

                // xx
                stiffness_matrix[start_dof, start_dof] += xx_stiffness;
                stiffness_matrix[end_dof, end_dof] += xx_stiffness;
                stiffness_matrix[start_dof, end_dof] -= xx_stiffness;
                stiffness_matrix[end_dof, start_dof] -= xx_stiffness;

                // yy
                stiffness_matrix[start_dof + 1, start_dof + 1] += yy_stiffness;
                stiffness_matrix[end_dof + 1, end_dof + 1] += yy_stiffness;
                stiffness_matrix[start_dof + 1, end_dof + 1] -= yy_stiffness;
                stiffness_matrix[end_dof + 1, start_dof + 1] -= yy_stiffness;

                // zz
                stiffness_matrix[start_dof + 2, start_dof + 2] += zz_stiffness;
                stiffness_matrix[end_dof + 2, end_dof + 2] += zz_stiffness;
                stiffness_matrix[start_dof + 2, end_dof + 2] -= zz_stiffness;
                stiffness_matrix[end_dof + 2, start_dof + 2] -= zz_stiffness;

                // xy
                stiffness_matrix[start_dof + 1, start_dof] += xy_stiffness;
                stiffness_matrix[end_dof + 1, end_dof] += xy_stiffness;
                stiffness_matrix[start_dof + 1, end_dof] -= xy_stiffness;
                stiffness_matrix[end_dof + 1, start_dof] -= xy_stiffness;

                stiffness_matrix[start_dof, start_dof + 1] += xy_stiffness;
                stiffness_matrix[end_dof, end_dof + 1] += xy_stiffness;
                stiffness_matrix[start_dof, end_dof + 1] -= xy_stiffness;
                stiffness_matrix[end_dof, start_dof + 1] -= xy_stiffness;

                // yz
                stiffness_matrix[start_dof + 2, start_dof + 1] += yz_stiffness;
                stiffness_matrix[end_dof + 2, end_dof + 1] += yz_stiffness;
                stiffness_matrix[start_dof + 2, end_dof + 1] -= yz_stiffness;
                stiffness_matrix[end_dof + 2, start_dof + 1] -= yz_stiffness;

                stiffness_matrix[start_dof + 1, start_dof + 2] += yz_stiffness;
                stiffness_matrix[end_dof + 1, end_dof + 2] += yz_stiffness;
                stiffness_matrix[start_dof + 1, end_dof + 2] -= yz_stiffness;
                stiffness_matrix[end_dof + 1, start_dof + 2] -= yz_stiffness;

                // zx
                stiffness_matrix[start_dof, start_dof + 2] += zx_stiffness;
                stiffness_matrix[end_dof, end_dof + 2] += zx_stiffness;
                stiffness_matrix[start_dof, end_dof + 2] -= zx_stiffness;
                stiffness_matrix[end_dof, start_dof + 2] -= zx_stiffness;

                stiffness_matrix[start_dof + 2, start_dof] += zx_stiffness;
                stiffness_matrix[end_dof + 2, end_dof] += zx_stiffness;
                stiffness_matrix[start_dof + 2, end_dof] -= zx_stiffness;
                stiffness_matrix[end_dof + 2, start_dof] -= zx_stiffness;
            }

            // apply boundary conditions

            for (int i = 0; i < num_nodes; i++)
            {
                var x_dof = 3 * i;
                var y_dof = x_dof + 1;
                var z_dof = x_dof + 2;

                if (supports_t[i].X == 1) // x-fixed
                {
                    for (int dof_count = 0; dof_count < num_nodes * 3; dof_count++)
                    {
                        stiffness_matrix[x_dof, dof_count] = 0.0;
                        stiffness_matrix[dof_count, x_dof] = 0.0;
                    }
                    stiffness_matrix[x_dof, x_dof] = 1.0;

                    // set x-load to zero
                    loads[i] = new Vector3d(0.0, loads[i].Y, loads[i].Z);
                }

                if (supports_t[i].Y == 1) // y-fixed
                {
                    for (int dof_count = 0; dof_count < num_nodes * 3; dof_count++)
                    {
                        stiffness_matrix[y_dof, dof_count] = 0.0;
                        stiffness_matrix[dof_count, y_dof] = 0.0;
                    }
                    stiffness_matrix[y_dof, y_dof] = 1.0;

                    // set y-load to zero
                    loads[i] = new Vector3d(loads[i].X, 0.0, loads[i].Z);
                }

                if (supports_t[i].Z == 1) // z-fixed
                {
                    for (int dof_count = 0; dof_count < num_nodes * 3; dof_count++)
                    {
                        stiffness_matrix[z_dof, dof_count] = 0.0;
                        stiffness_matrix[dof_count, z_dof] = 0.0;
                    }
                    stiffness_matrix[z_dof, z_dof] = 1.0;

                    // set z-load to zero
                    loads[i] = new Vector3d(loads[i].X, loads[i].Y, 0.0);
                }
            }

            var inverse_stiffness_matrix = SparseMatrix.OfArray(stiffness_matrix).Inverse();

            var force_vector_storage = new double[num_nodes * 3];
            var force_vector = new DenseVector(force_vector_storage);
            for (int i = 0; i < num_nodes; i++)
            {
                force_vector_storage[3 * i] = loads[i].X;
                force_vector_storage[3 * i + 1] = loads[i].Y;
                force_vector_storage[3 * i + 2] = loads[i].Z;
            }

            var deflections_vector = inverse_stiffness_matrix.Multiply(force_vector);

            var deflections_array = deflections_vector.ToArray();

            deflections = new Vector3d[num_nodes];
            for (int i = 0; i < num_nodes; i++)
            {
                deflections[i] = new Vector3d(
                    deflections_vector[3 * i],     // x
                    deflections_vector[3 * i + 1], // y
                    deflections_vector[3 * i + 2]  // z
                    );
            }

            forces = new double[num_bars];
            for (int i = 0; i < num_bars; i++)
            {
                forces[i] = YoungsModulus * bars_mat[i] / (Math.Pow(vectors[i].X, 2) + Math.Pow(vectors[i].Y, 2) + Math.Pow(vectors[i].Z, 2));
                forces[i] *= vectors[i].X * (deflections[bars_e[i]].X - deflections[bars_s[i]].X) +
                    vectors[i].Y * (deflections[bars_e[i]].Y - deflections[bars_s[i]].Y) +
                    vectors[i].Z * (deflections[bars_e[i]].Z - deflections[bars_s[i]].Z);
            }

            return true;
        }
    }
}
