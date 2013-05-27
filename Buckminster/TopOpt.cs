using System;
using System.Collections.Generic;
using System.Text;

using World = Buckminster.Types.Molecular;
using Vertex = Buckminster.Types.Molecular.Node;
using Edge = Buckminster.Types.Molecular.Bar;
using Color = System.Drawing.Color;

using Google.OrTools.LinearSolver;  // Van Omme, N., Perron, L. and Furnon, V., "or-tools user’s manual", Google, 2013.

#if (!TEST)
using Rhino.Geometry;
#else
using System.Windows.Media.Media3D;
using Vector3d = System.Windows.Media.Media3D.Vector3D;
using Point3d = System.Windows.Media.Media3D.Point3D;
#endif

namespace Buckminster
{
    /// <summary>
    /// Layout Optimisation Code
    /// </summary>
    public class TopOpt
    {
        static World theWorld = null;
        static double limit_tension = 1.0;
        static double limit_compression = 1.0;
        static double joint_cost = 0.0;

        /// <summary>
        /// Cumulative sum of WallTimes
        /// </summary>
        public static double RunTime;
        /// <summary>
        /// Current Value of Objective Function
        /// </summary>
        public static double Volume;
        /// <summary>
        /// Number of Members Added
        /// </summary>
        public static int MembersAdded;
        /// <summary>
        /// Number of Members Removed
        /// </summary>
        public static int MembersRemoved;

        /// <summary>
        /// Make demo cantilever for TopOpt test
        /// </summary>
        public static bool SetWorld(World aWorld, double limit_t, double limit_c, double j_cost)
        {
            limit_tension = limit_t;
            limit_compression = limit_c;
            joint_cost = j_cost;

            bool was_null = (theWorld == null);
            theWorld = aWorld;
            return was_null;
        }

        /// <summary>
        /// Linear Programming Solver of Ax=B
        /// </summary>
        static public bool SolveProblem()
        {
            Solver theSolver = new Solver("TopOpt Test", Google.OrTools.LinearSolver.Solver.CLP_LINEAR_PROGRAMMING);
            List<Variable> listCompressions = new List<Variable>(theWorld.listEdges.Count);
            List<Variable> listTensions = new List<Variable>(theWorld.listEdges.Count);
            List<Google.OrTools.LinearSolver.Constraint> listXConstraints = new List<Google.OrTools.LinearSolver.Constraint>(theWorld.listVertexes.Count);
            List<Google.OrTools.LinearSolver.Constraint> listYConstraints = new List<Google.OrTools.LinearSolver.Constraint>(theWorld.listVertexes.Count);
            List<Google.OrTools.LinearSolver.Constraint> listZConstraints = new List<Google.OrTools.LinearSolver.Constraint>(theWorld.listVertexes.Count);
            Variable aVariable;
            Google.OrTools.LinearSolver.Constraint xConstraint = null;
            Google.OrTools.LinearSolver.Constraint yConstraint = null;
            Google.OrTools.LinearSolver.Constraint zConstraint = null;
            Vector3d aVector;
            List<int> listConstraintExists = new List<int>(theWorld.listVertexes.Count);

            foreach (Edge aEdge in theWorld.listEdges)
            {
                //  x values allowed between -tension and +compression stresses
                aVariable = theSolver.MakeNumVar(0.0, limit_tension, String.Format("TensionsInElement#{0:D4}", aEdge.Number));
                listTensions.Add(aVariable);
                theSolver.SetObjectiveCoefficient(aVariable, 2*joint_cost + aEdge.Length/limit_tension);

                aVariable = theSolver.MakeNumVar(0.0, limit_compression, String.Format("CompressionInElement#{0:D4}", aEdge.Number));
                listCompressions.Add(aVariable);
                theSolver.SetObjectiveCoefficient(aVariable, 2*joint_cost + aEdge.Length/limit_compression);
            }

            foreach (Vertex aVertex in theWorld.listVertexes)
            {
                if (aVertex.Fixity==null)
                {
                    if (aVertex.Force == null)
                    {
                        xConstraint = theSolver.MakeConstraint(0.0, 0.0);
                        yConstraint = theSolver.MakeConstraint(0.0, 0.0);
                        zConstraint = theSolver.MakeConstraint(0.0, 0.0);
                    }
                    else
                    {
                        xConstraint = theSolver.MakeConstraint(aVertex.Force.X, aVertex.Force.X);
                        yConstraint = theSolver.MakeConstraint(aVertex.Force.Y, aVertex.Force.Y);
                        zConstraint = theSolver.MakeConstraint(aVertex.Force.Z, aVertex.Force.Z);
                    }
                }
                else
                {
                    xConstraint = null;
                    yConstraint = null;
                    zConstraint = null;
                    if (aVertex.Force == null)
                    {
                        if (!aVertex.Fixity.X) xConstraint = theSolver.MakeConstraint(0.0, 0.0);
                        if (!aVertex.Fixity.Y) yConstraint = theSolver.MakeConstraint(0.0, 0.0);
                        if (!aVertex.Fixity.Z) zConstraint = theSolver.MakeConstraint(0.0, 0.0);
                    }
                    else
                    {
                        if (!aVertex.Fixity.X) xConstraint = theSolver.MakeConstraint(aVertex.Force.X, aVertex.Force.X);
                        if (!aVertex.Fixity.Y) yConstraint = theSolver.MakeConstraint(aVertex.Force.Y, aVertex.Force.Y);
                        if (!aVertex.Fixity.Z) zConstraint = theSolver.MakeConstraint(aVertex.Force.Z, aVertex.Force.Z);
                    }
                }
                listConstraintExists.Add(0);
                if (xConstraint != null) listConstraintExists[aVertex.Index] += 1;
                if (yConstraint != null) listConstraintExists[aVertex.Index] += 2;
                if (zConstraint != null) listConstraintExists[aVertex.Index] += 4;

                if ((xConstraint != null) || (yConstraint != null) || (zConstraint != null))  //  If fully Fixed then don't bother
                {
                    foreach (Edge aEdge in aVertex.listEdgesStarting)
                    {
                        aVector = aEdge.StartVertex.Coord-aEdge.EndVertex.Coord;
                        aVector.Unitize();
                        aVariable = listCompressions[aEdge.Index];
                        if (xConstraint != null) xConstraint.SetCoefficient(aVariable, -aVector.X);
                        if (yConstraint != null) yConstraint.SetCoefficient(aVariable, -aVector.Y);
                        if (zConstraint != null) zConstraint.SetCoefficient(aVariable, -aVector.Z);
                        aVariable = listTensions[aEdge.Index];
                        if (xConstraint != null) xConstraint.SetCoefficient(aVariable, aVector.X);
                        if (yConstraint != null) yConstraint.SetCoefficient(aVariable, aVector.Y);
                        if (zConstraint != null) zConstraint.SetCoefficient(aVariable, aVector.Z);
                    }
                    foreach (Edge aEdge in aVertex.listEdgesEnding)
                    {
                        aVector = aEdge.EndVertex.Coord-aEdge.StartVertex.Coord;
                        aVector.Unitize();
                        aVariable = listCompressions[aEdge.Index];
                        if (xConstraint != null) xConstraint.SetCoefficient(aVariable, -aVector.X);
                        if (yConstraint != null) yConstraint.SetCoefficient(aVariable, -aVector.Y);
                        if (zConstraint != null) zConstraint.SetCoefficient(aVariable, -aVector.Z);
                        aVariable = listTensions[aEdge.Index];
                        if (xConstraint != null) xConstraint.SetCoefficient(aVariable, aVector.X);
                        if (yConstraint != null) yConstraint.SetCoefficient(aVariable, aVector.Y);
                        if (zConstraint != null) zConstraint.SetCoefficient(aVariable, aVector.Z);
                    }
                    if (xConstraint != null) listXConstraints.Add(xConstraint);
                    if (yConstraint != null) listYConstraints.Add(yConstraint);
                    if (zConstraint != null) listZConstraints.Add(zConstraint);
                }
            }
            System.Diagnostics.Debug.WriteLine(String.Format("Number of Variables {0} : Number of Constraints {1}", theSolver.NumVariables(), theSolver.NumConstraints()));
            theSolver.SetMinimization();
            int result = theSolver.Solve();
            if (result == Solver.OPTIMAL)
            {
                Volume = theSolver.ObjectiveValue();
                RunTime += 0.001*theSolver.WallTime();
                //System.Windows.Forms.MessageBox.Show(String.Format("Solved in {0} milliseconds", theSolver.WallTime()));
                //System.Diagnostics.Debug.WriteLine(String.Format("Solved in {0:0.000} seconds", 0.001*theSolver.WallTime()));
                //System.Diagnostics.Debug.WriteLine(String.Format("Total Volume = {0:F6}", Volume));

                double stress;
                foreach (Edge aEdge in theWorld.listEdges)
                {
                    stress = listTensions[aEdge.Index].SolutionValue();
                    if (Math.Abs(stress)<0.0000001) stress = -listCompressions[aEdge.Index].SolutionValue();
                    if (Math.Abs(stress) < 0.0000001)
                    {
                        //aEdge.Colour.FromColor(System.Drawing.Color.DarkGray);
                        aEdge.Colour = Color.FromArgb(100, Color.DarkGray);
                    }
                    else
                    {
                        if (stress < 0.0)
                        {
                            //aEdge.Colour.FromColor(System.Drawing.Color.Red);
                            aEdge.Colour = Color.FromArgb(100, Color.Red);
                        }
                        else
                        {
                            //aEdge.Colour.FromColor(System.Drawing.Color.Blue);
                            aEdge.Colour = Color.FromArgb(100, Color.Blue);
                        }
                    }
                    aEdge.Radius = Math.Abs(stress) * 0.01;  //  Store Stress in Edge Radius
                    //System.Diagnostics.Debug.WriteLine(String.Format("Stress in Edge {0:D4} = {1:F6}", aEdge.Number, aEdge.Radius));
                }

                double x_dual, y_dual, z_dual;
                int x_index, y_index, z_index;
                x_index = y_index = z_index = 0;
                foreach (Vertex aVertex in theWorld.listVertexes)
                {
                    x_dual = y_dual = z_dual = 0.0;
                    if ((listConstraintExists[aVertex.Index] & 1) > 0) x_dual = listXConstraints[x_index++].DualValue();
                    if ((listConstraintExists[aVertex.Index] & 2) > 0) y_dual = listYConstraints[y_index++].DualValue();
                    if ((listConstraintExists[aVertex.Index] & 4) > 0) z_dual = listZConstraints[z_index++].DualValue();
                    aVertex.Velocity = new Vector3d(x_dual, y_dual, z_dual);  //  Store Virtual Displacements in Vertex Velocity
                    System.Diagnostics.Debug.WriteLine(String.Format("Dual Displacement in Vertex#{0} = ({1:F6},{2:F6},{3:F6})", aVertex.Number, aVertex.Velocity.X, aVertex.Velocity.Y, aVertex.Velocity.Z));
                }
                //World.Make_VelocityArrowsArray = true;
            }
            else
            {
                if (result == Solver.INFEASIBLE)
                {
                    System.Windows.Forms.MessageBox.Show(String.Format("Infeasible Problem Definition"));
                    //System.Diagnostics.Debug.WriteLine(String.Format("    {0}", theSolver.ComputeExactConditionNumber()));
                }
                else
                {
                    if (result == Solver.UNBOUNDED)
                    {
                        System.Windows.Forms.MessageBox.Show(String.Format("Unbounded Problem Definition"));
                    }
                    else
                    {
                        if (result == Solver.FEASIBLE)
                        {
                            System.Windows.Forms.MessageBox.Show(String.Format("Feasible Problem Stopped by Limit"));
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show(String.Format("Abnormal Problem - Some Kind of Error"));
                        }
                    }
                }
            }

            return (result == Solver.OPTIMAL);
        }

        /// <summary>
        /// Add Edges to TopOpt Problem
        /// </summary>
        static public void AddEdges(double add_proportion, double remove_proportion)
        {
            List<double> listViolationAdd = new List<double>();
            List<int> listStartIndex = new List<int>();
            List<int> listEndIndex = new List<int>();
            List<double> listViolationRemove = new List<double>();
            List<Edge> listEdgesToRemove = new List<Edge>();
            Edge aEdge;
            Vertex sVertex, eVertex;
            Vector3d aDeltaVector;
            double length, violation;
            int index;

            //  Use virtual displacements Length(EndVertex.Velocity-StartVertex.Velocity) to calc potential strain in each possible member and add in 10% most strained ones
            for (int start = 0; start < theWorld.listVertexes.Count; start++)
            {
                for (int end = start + 1; end < theWorld.listVertexes.Count; end++)
                {
                    sVertex = theWorld.listVertexes[start];
                    eVertex = theWorld.listVertexes[end];

                    //  Calculate violation
                    aDeltaVector = eVertex.Coord - sVertex.Coord;
                    length = aDeltaVector.Length;
                    violation = ((eVertex.Velocity - sVertex.Velocity) * aDeltaVector)/(length + joint_cost);
                    violation /= length;
                    violation = Math.Max(violation * limit_tension, -violation * limit_compression);

                    aEdge = theWorld.FindEdge(sVertex, eVertex); //, Test_Duplicates.All, null);
                    if (aEdge == null)
                    {
                        //  Edge doesn't already exist
                        if (violation >= 1.001)
                        {
                            for (index = 0; index < listViolationAdd.Count; index++)
                            {
                                if (listViolationAdd[index] < violation) break;
                            }
                            listStartIndex.Insert(index, start);
                            listEndIndex.Insert(index, end);
                            listViolationAdd.Insert(index, violation);
                        }
                    }
                    else
                    {
                        for (index = 0; index < listViolationRemove.Count; index++)
                        {
                            if (listViolationRemove[index] < violation) break;
                        }
                        listEdgesToRemove.Insert(index, aEdge);
                        listViolationRemove.Insert(index, violation);
                    }
                }
            }
            //  Remove Existing
            listEdgesToRemove.RemoveRange(0, Convert.ToInt32((1.0 - remove_proportion) * listViolationRemove.Count));
            MembersRemoved = listEdgesToRemove.Count;
            theWorld.DeleteElements(listEdgesToRemove);
            //System.Diagnostics.Debug.WriteLine("TopOpt : Removed {0} Members", MembersRemoved);

            // Add New
            MembersAdded = Convert.ToInt32(Math.Ceiling(add_proportion * listViolationAdd.Count));
            for (index = 0; index < MembersAdded; index++)
            {
                sVertex = theWorld.listVertexes[listStartIndex[index]];
                eVertex = theWorld.listVertexes[listEndIndex[index]];
                theWorld.NewEdge(sVertex, eVertex).Radius = 0.0;
            }
        }

        /// <summary>
        /// Remove Least Stressed Edges from TopOpt Problem
        /// </summary>
        static public void RemoveUnstressed(double tolerance)
        {
            List<Edge> listEs = new List<Edge>(theWorld.listEdges.Count);
            foreach (Edge aEdge in theWorld.listEdges)
            {
                if (Math.Abs(aEdge.Radius)<tolerance) listEs.Add(aEdge);
            }
            theWorld.DeleteElements(listEs);
        }
    }
}
