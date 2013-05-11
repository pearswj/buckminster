using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using Rhino.Geometry;
using System.Windows.Media.Media3D;
using Vector3d = System.Windows.Media.Media3D.Vector3D;
using Point3d = System.Windows.Media.Media3D.Point3D;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Buckminster
{
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
            out Vector3d[] deflections
            )
        {
            var num_bars = bars_s.Count;
            var num_nodes = nodes.Count;

            if (num_nodes != loads.Count |
                num_nodes != supports_t.Count |
                num_bars != bars_e.Count |
                num_bars != bars_mat.Count)
            {
                deflections = new Vector3d[num_nodes];
                return false;
            }

            var YoungsModulus = 205000.0;

            //var matrix = new DenseMatrix(n * 2, n * 2);
            var stiffness_matrix = new double[num_nodes * 2, num_nodes * 2];

            // build stiffness matrix

            for (int i = 0; i < num_bars; i++)
            {
                var vector = nodes[bars_e[i]] - nodes[bars_s[i]];
                var EA_over_Lcubed = YoungsModulus * bars_mat[i] / Math.Pow(vector.Length, 3); // bar_mat is csa
                var xx_stiffness = EA_over_Lcubed * Math.Pow(vector.X, 2);
                var yy_stiffness = EA_over_Lcubed * Math.Pow(vector.Y, 2);
                var xy_stiffness = EA_over_Lcubed * vector.X * vector.Y;

                var start_dof = 2 * bars_s[i];
                var end_dof = 2 * bars_e[i];

                stiffness_matrix[start_dof, start_dof] += xx_stiffness;
                stiffness_matrix[end_dof, end_dof] += xx_stiffness;
                stiffness_matrix[start_dof, end_dof] -= xx_stiffness;
                stiffness_matrix[end_dof, start_dof] -= xx_stiffness;

                stiffness_matrix[start_dof + 1, start_dof + 1] += yy_stiffness;
                stiffness_matrix[end_dof + 1, end_dof + 1] += yy_stiffness;
                stiffness_matrix[start_dof + 1, end_dof + 1] -= yy_stiffness;
                stiffness_matrix[end_dof + 1, start_dof + 1] -= yy_stiffness;

                stiffness_matrix[start_dof + 1, start_dof] += xy_stiffness;
                stiffness_matrix[end_dof + 1, end_dof] += xy_stiffness;
                stiffness_matrix[start_dof + 1, end_dof] -= xy_stiffness;
                stiffness_matrix[end_dof + 1, start_dof] -= xy_stiffness;

                stiffness_matrix[start_dof, start_dof + 1] += xy_stiffness;
                stiffness_matrix[end_dof, end_dof + 1] += xy_stiffness;
                stiffness_matrix[start_dof, end_dof + 1] -= xy_stiffness;
                stiffness_matrix[end_dof, start_dof + 1] -= xy_stiffness;
            }

            // apply boundary conditions

            for (int i = 0; i < num_nodes; i++)
            {
                var x_dof = 2 * i;
                var y_dof = x_dof + 1;

                if (supports_t[i].X == 1) // x-fixed
                {
                    for (int dof_count = 0; dof_count < num_nodes * 2; dof_count++)
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
                    for (int dof_count = 0; dof_count < num_nodes * 2; dof_count++)
                    {
                        stiffness_matrix[y_dof, dof_count] = 0.0;
                        stiffness_matrix[dof_count, y_dof] = 0.0;
                    }
                    stiffness_matrix[y_dof, y_dof] = 1.0;

                    // set y-load to zero
                    loads[i] = new Vector3d(loads[i].X, 0.0, loads[i].Z);
                }
            }

            var inverse_stiffness_matrix = DenseMatrix.OfArray(stiffness_matrix).Inverse();

            var force_vector_storage = new double[num_nodes * 2];
            var force_vector = new DenseVector(force_vector_storage);
            for (int i = 0; i < num_nodes; i++)
            {
                force_vector_storage[2 * i] = loads[i].X;
                force_vector_storage[2 * i + 1] = loads[i].Y;
            }

            var deflections_vector = inverse_stiffness_matrix.Multiply(force_vector);

            var deflections_array = deflections_vector.ToArray();

            deflections = new Vector3d[num_nodes];
            for (int i = 0; i < num_nodes; i++)
            {
                deflections[i] = new Vector3d(deflections_vector[2 * i], deflections_vector[2 * i + 1], 0.0);
            }

            return true;
        }
    }
}
