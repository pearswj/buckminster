using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using World = Buckminster.Types.Molecular;
using Vertex = Buckminster.Types.Molecular.Node;
using Edge = Buckminster.Types.Molecular.Bar;
using Color = System.Drawing.Color;

using Google.OrTools.LinearSolver;  // Van Omme, N., Perron, L. and Furnon, V., "or-tools user’s manual", Google, 2013.
//using mosek;

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
        static int[,] PCL = null;
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
        /// Set up the problem. (aWorld is not copied.)
        /// </summary>
        public static bool SetProblem(World aWorld, double limit_t, double limit_c, double j_cost)
        {
            RunTime = 0; // Reset the clock
            limit_tension = limit_t;
            limit_compression = limit_c;
            joint_cost = j_cost;

            bool was_null = (theWorld == null);
            theWorld = aWorld;
            return was_null;
        }

        public static bool SetProblem(World aWorld, World pcl, double limit_t, double limit_c, double j_cost)
        {
            bool retval = SetProblem(aWorld, limit_t, limit_c, j_cost);

            if (pcl == null) PCL = null;
            else // Use pcl structure to populate PCL (we have to assume that they are similar!)
            {
                PCL = new int[pcl.listEdges.Count, 2];
                for (int i = 0; i < pcl.listEdges.Count; i++)
                {
                    PCL[i, 0] = pcl.listEdges[i].StartVertex.Index;
                    PCL[i, 1] = pcl.listEdges[i].EndVertex.Index;
                }
            }

            return retval;
        }

        /// <summary>
        /// Linear Programming Solver of Ax=B
        /// </summary>
        /// <returns>True if optimal solution found.</returns>
        static public bool SolveProblemGoogle()
        {
            string msg; // trash this
            return SolveProblemGoogle(out msg);
        }

        /// <summary>
        /// Linear Programming Solver of Ax=B 
        /// </summary>
        /// <param name="message">Gives details about the state of the solver.</param>
        /// <returns>True if optimal solution found.</returns>
        static public bool SolveProblemGoogle(out string message)
        {
            Solver theSolver = new Solver("TopOpt Test", Google.OrTools.LinearSolver.Solver.CLP_LINEAR_PROGRAMMING);
            var listCompressions = new List<Variable>(theWorld.listEdges.Count);
            var listTensions     = new List<Variable>(theWorld.listEdges.Count);
            var listXConstraints = new List<Constraint>(theWorld.listVertexes.Count);
            var listYConstraints = new List<Constraint>(theWorld.listVertexes.Count);
            var listZConstraints = new List<Constraint>(theWorld.listVertexes.Count);
            Variable aVariable;
            Constraint xConstraint = null;
            Constraint yConstraint = null;
            Constraint zConstraint = null;
            Vector3d aVector;
            var listConstraintExists = new List<int>(theWorld.listVertexes.Count);

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
                        //aVector.Unitize();
                        aVector = aVector / aVector.Length;
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
                        //aVector.Unitize();
                        aVector = aVector / aVector.Length;
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
                // Get volume
                Volume = theSolver.ObjectiveValue();
                RunTime += 0.001*theSolver.WallTime();
                //System.Windows.Forms.MessageBox.Show(String.Format("Solved in {0} milliseconds", theSolver.WallTime()));
                //System.Diagnostics.Debug.WriteLine(String.Format("Solved in {0:0.000} seconds", 0.001*theSolver.WallTime()));
                System.Diagnostics.Debug.WriteLine(String.Format("Total Volume = {0:F6}", Volume));

                // Get bar stresses
                double stress;
                foreach (Edge aEdge in theWorld.listEdges)
                {
                    stress = listTensions[aEdge.Index].SolutionValue();
                    if (Math.Abs(stress)<0.0000001) stress = -listCompressions[aEdge.Index].SolutionValue();
                    if (Math.Abs(stress) < 0.0000001) // Unstressed
                        aEdge.Colour = Color.FromArgb(100, Color.DarkGray);
                    else if (stress < 0.0) // Compression
                        aEdge.Colour = Color.FromArgb(100, Color.Red);
                    else // Tension
                        aEdge.Colour = Color.FromArgb(100, Color.Blue);
                    aEdge.Radius = Math.Abs(stress) * 0.01;  //  Store Stress in Edge Radius
                    //System.Diagnostics.Debug.WriteLine(String.Format("Stress in Edge {0:D4} = {1:F6}", aEdge.Number, aEdge.Radius));
                }

                // Get dual displacements
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
                message = "Optimal solution found";
                return true;
            }
            else if (result == Solver.INFEASIBLE)
                message = "Infeasible problem definition";
            else if (result == Solver.UNBOUNDED)
                message = "Unbounded Problem Definition";
            else if (result == Solver.FEASIBLE)
                message = "Feasible Problem Stopped by Limit";
            else
                message = "Abnormal Problem - Some Kind of Error";

            return false;
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

            if (PCL == null) // Create fully-connected potential connections list
            {
                int n = theWorld.listVertexes.Count;
                PCL = new int[n * (n - 1) / 2, 2];
                for (int start = 0, count = 0; start < n; start++)
                {
                    for (int end = start + 1; end < n; end++, count++)
                    {
                        PCL[count, 0] = start;
                        PCL[count, 1] = end;
                    }
                }
            }

            //  Use virtual displacements Length(EndVertex.Velocity-StartVertex.Velocity) to calc potential strain in each possible member and add in 10% most strained ones
            for (int i = 0; i < PCL.GetLength(0); i++)
            {
                int start = PCL[i, 0];
                int end = PCL[i, 1];

                sVertex = theWorld.listVertexes[start];
                eVertex = theWorld.listVertexes[end];

                //  Calculate violation
                aDeltaVector = eVertex.Coord - sVertex.Coord;
                length = aDeltaVector.Length;
                violation = ((eVertex.Velocity - sVertex.Velocity) * aDeltaVector) / (length + joint_cost);
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

        /// <summary>
        /// Linear Programming Solver of Ax=B (Mosek).
        /// </summary>
        /// <returns>True if optimal solution found.</returns>
        static public bool SolveProblemMosek()
        {
            string msg;
            return SolveProblemMosek(out msg);
        }

        /// <summary>
        /// Linear Programming Solver of Ax=B (Mosek).
        /// </summary>
        /// <param name="message">Gives details about the state of the solver.</param>
        /// <returns>True if optimal solution found.</returns>
        static public bool SolveProblemMosek(out string message)
        {
            int numcon = 0; // We'll add up the DOFs as we go...
            int numvar = theWorld.listEdges.Count * 2; // Tension & compression

            // Coefficients
            var c = new double[numvar];
            for (int i = 0; i < theWorld.listEdges.Count; i++)
            {
                var aEdge = theWorld.listEdges[i];
                c[i] = 2 * joint_cost + aEdge.Length / limit_tension;
                c[i + theWorld.listEdges.Count] = 2 * joint_cost + aEdge.Length / limit_compression;
            }

            // Create sparse matrix and initialise
            var asub = new List<int>[numvar];
            var aval = new List<double>[numvar];
            for (int i = 0; i < numvar; i++)
            {
                asub[i] = new List<int>();
                aval[i] = new List<double>();
            }

            // create list of forces (a.k.a. boundary constraints)
            var bc = new List<double>();

            for (int i = 0; i < theWorld.listVertexes.Count; i++) // iterate through vertices
            {
                var aVertex = theWorld.listVertexes[i];

                // Degrees of freedom for vertex #i
                aVertex.Fixity = aVertex.Fixity ?? theWorld.NewConstraint(false, false, false); // Just incase its empty...
                if (aVertex.Fixity.X && aVertex.Fixity.Y && aVertex.Fixity.Z) continue; // ignore completely
                if (!aVertex.Fixity.X) bc.Add(aVertex.Force.X);
                if (!aVertex.Fixity.Y) bc.Add(aVertex.Force.Y);
                if (!aVertex.Fixity.Z) bc.Add(aVertex.Force.Z);

                var aFixityArray = new bool[] { aVertex.Fixity.X, aVertex.Fixity.Y, aVertex.Fixity.Z };
                //var dofs = aFixityArray.Select(fix => fix ? 0 : 1).Sum();

                foreach (var aEdge in aVertex.listEdgesStarting)
                {
                    int j = aEdge.Index;
                    var aVector = aEdge.StartVertex.Coord - aEdge.EndVertex.Coord;
                    aVector = aVector / aVector.Length; // Unitize() won't work in unit tests
                    var aComponent = new double[] { aVector.X, aVector.Y, aVector.Z };
                    //int dof = bc.Count - dofs;
                    int dof = numcon;
                    for (int k = 0; k < 3; k++)
                    {
                        if (aFixityArray[k]) continue;
                        asub[j].Add(dof);
                        aval[j].Add(-aComponent[k]);
                        asub[j + theWorld.listEdges.Count].Add(dof);
                        aval[j + theWorld.listEdges.Count].Add(aComponent[k]);
                        dof++;
                    }
                }
                foreach (var aEdge in aVertex.listEdgesEnding)
                {
                    int j = aEdge.Index;
                    var aVector = aEdge.EndVertex.Coord - aEdge.StartVertex.Coord;
                    aVector = aVector / aVector.Length; // Unitize() won't work in unit tests
                    var aComponent = new double[] { aVector.X, aVector.Y, aVector.Z };
                    //int dof = bc.Count - dofs;
                    int dof = numcon;
                    for (int k = 0; k < 3; k++)
                    {
                        if (aFixityArray[k]) continue;
                        asub[j].Add(dof);
                        aval[j].Add(-aComponent[k]);
                        asub[j + theWorld.listEdges.Count].Add(dof);
                        aval[j + theWorld.listEdges.Count].Add(aComponent[k]);
                        dof++;
                    }
                }
                numcon = bc.Count;
            }

            //int numcon = bc.Count;


            // Make mosek environment.
            using (mosek.Env env = new mosek.Env())
            {
                // Create a task object.
                using (mosek.Task task = new mosek.Task(env, 0, 0))
                {
                    // Directs the log task stream to the user specified
                    // method msgclass.streamCB
                    //task.set_Stream(mosek.streamtype.log, new msgclass(""));

                    // Append 'numcon' empty constraints.
                    // The constraints will initially have no bounds.
                    task.appendcons(numcon);

                    // Append 'numvar' variables.
                    // The variables will initially be fixed at zero (x=0).
                    task.appendvars(numvar);

                    for (int j = 0; j < numvar; ++j)
                    {
                        // Set the linear term c_j in the objective.
                        task.putcj(j, c[j]);

                        // Set the bounds on variable j.
                        // blx[j] <= x_j <= bux[j]
                        //task.putvarbound(j, bkx[j], blx[j], bux[j]);
                        task.putvarbound(j, mosek.boundkey.lo, 0, 0);

                        // Input column j of A   
                        task.putacol(j,                     /* Variable (column) index.*/
                                     asub[j].ToArray(),               /* Row index of non-zeros in column j.*/
                                     aval[j].ToArray());              /* Non-zero Values of column j. */
                    }

                    // Set the bounds on constraints.
                    // blc[i] <= constraint_i <= buc[i]
                    for (int i = 0; i < numcon; ++i)
                        //task.putconbound(i, bkc[i], blc[i], buc[i]);
                        task.putconbound(i, mosek.boundkey.fx, bc[i], bc[i]);

                    System.Diagnostics.Debug.WriteLine("Number of Variables {0} : Number of Constraints {1}", task.getnumvar(), task.getnumcon());

                    // Input the objective sense (minimize/maximize)
                    task.putobjsense(mosek.objsense.minimize);

                    var stopwatch = new System.Diagnostics.Stopwatch();
                    stopwatch.Start();

                    // Solve the problem
                    task.optimize();

                    stopwatch.Stop();
                    RunTime += stopwatch.Elapsed.TotalSeconds; // Add to total running time

                    // Print a summary containing information
                    // about the solution for debugging purposes
                    task.solutionsummary(mosek.streamtype.msg);

                    // Get status information about the solution        
                    mosek.solsta solsta;

                    task.getsolsta(mosek.soltype.itr, out solsta);

                    switch (solsta)
                    {
                        case mosek.solsta.optimal:
                        case mosek.solsta.near_optimal:
                            // Get Volume
                            Volume = task.getprimalobj(mosek.soltype.bas);
                            System.Diagnostics.Debug.WriteLine(String.Format("Total Volume = {0:F6}", Volume));

                            // Get bar stresses
                            double[] xx = new double[numvar];
                            task.getxx(mosek.soltype.bas, xx);
                            foreach (var aEdge in theWorld.listEdges)
                            { // Get bar stress and store as colour and radius
                                var stress = Math.Abs(xx[aEdge.Index]) < 1E-6 ? -xx[aEdge.Index + theWorld.listEdges.Count] : xx[aEdge.Index];
                                if (Math.Abs(stress) < 1E-6) // Unstressed
                                    aEdge.Colour = Color.FromArgb(100, Color.DarkGray);
                                else if (stress < 0.0) // Compression
                                    aEdge.Colour = Color.FromArgb(100, Color.Blue);
                                else // Tension
                                    aEdge.Colour = Color.FromArgb(100, Color.Red);
                                aEdge.Radius = Math.Abs(stress) * 0.01;  //  Store Stress in Edge Radius
                                //System.Diagnostics.Debug.WriteLine(String.Format("Stress in Edge {0:D4} = {1:F6}", aEdge.Number, aEdge.Radius));
                            }

                            // Get dual displacements
                            double[] y = new double[numcon];
                            task.gety(mosek.soltype.bas, y);
                            double x_dual, y_dual, z_dual;
                            int dof = 0;
                            foreach (var aVertex in theWorld.listVertexes)
                            {
                                x_dual = !aVertex.Fixity.X ? y[dof++] : 0.0;
                                y_dual = !aVertex.Fixity.Y ? y[dof++] : 0.0;
                                z_dual = !aVertex.Fixity.Z ? y[dof++] : 0.0;
                                aVertex.Velocity = new Vector3d(x_dual, y_dual, z_dual);  //  Store Virtual Displacements in Vertex Velocity
                                System.Diagnostics.Debug.WriteLine(String.Format("Dual Displacement in Vertex#{0} = ({1:F6},{2:F6},{3:F6})", aVertex.Number, aVertex.Velocity.X, aVertex.Velocity.Y, aVertex.Velocity.Z));
                            }

                            message = "Optimal primal solution";
                            return true;
                        case mosek.solsta.dual_infeas_cer:
                        case mosek.solsta.prim_infeas_cer:
                        case mosek.solsta.near_dual_infeas_cer:
                        case mosek.solsta.near_prim_infeas_cer:
                            message = "Primal or dual infeasibility certificate found.";
                            break;
                        case mosek.solsta.unknown:
                            message = "Unknown solution status.";
                            break;
                        default:
                            message = "Other solution status";
                            break;
                    }
                }
            }
            return false;
        }
    }
}