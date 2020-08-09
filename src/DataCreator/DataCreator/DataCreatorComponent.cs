using System;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;

// Data creator is made to get the optimal boolean set for the optimisation
namespace DataComponent
{
    public class DataComponentComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public DataComponentComponent()
          : base("DataComponentComponent", "DataCreator",
            "Constructs a dataset to put into NM algorithm",
            "Maths", "Script")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// A matrix of the observed parameters (input)s settings and the correspondings outputs.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMatrixParameter("Output", "Output", "Outputs", GH_ParamAccess.item);
            pManager.AddMatrixParameter("Input", "Input", "Inputs", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// It gives the optimal boolean setting and corresponding the input and output to put in to the optimisation. 
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMatrixParameter("Bool", "Bool", "Bool settings", GH_ParamAccess.item);
            pManager.AddMatrixParameter("Y", "Y", "Y variable", GH_ParamAccess.item);
            pManager.AddMatrixParameter("X", "X", "X variable", GH_ParamAccess.item);
        }

        // Regression class to get beta's for final function to optimise
        public static Matrix Regression(Matrix x, Matrix y)
        {
            // make regression to get beta for each variable
            // first a base functions
            Matrix basic = new Matrix(y.ColumnCount, x.ColumnCount + 1);

            for (int i = 0; i < basic.RowCount; i++)
            {
                for (int j = 0; j < basic.ColumnCount; j++)
                {
                    if ((x != null) && (j < x.ColumnCount))
                    {
                        basic[i, j] = x[0, j];
                    }
                    else
                    {
                        basic[i, j] = y[0, i];
                    }
                }
            }

            // create return beta matrix
            Matrix beta = new Matrix(y.ColumnCount, x.ColumnCount);

            int numy = new int();
            numy = x.ColumnCount;

            // make temp matrices to compare to base and create beta's to put in matrix
            for (int row = 1; row < x.RowCount; row++)
            {
                Matrix temp = new Matrix(x.ColumnCount + 1, y.ColumnCount);
                Matrix diff = new Matrix(x.ColumnCount + 1, y.ColumnCount);
                for (int i = 0; i < temp.RowCount; i++)
                {
                    for (int j = 0; j < temp.ColumnCount; j++)
                    {
                        if ((x != null) && (j < x.ColumnCount))
                        {
                            temp[i, j] = x[row, j];
                        }
                        else
                        {
                            {
                                temp[i, j] = y[row, i];
                            }
                        }
                    }
                }

                for (int i = 0; i < temp.RowCount; i++)
                {
                    for (int j = 0; j < temp.ColumnCount; j++)
                    {
                        diff[i, j] = basic[i, j] - temp[i, j];
                    }
                }

                // create array for the right column in final beta matrix
                int param = new int();
                double[] xbet = new double[y.RowCount];
                for (int column = 0; column < x.ColumnCount; column++)
                {
                    if (diff[0, column] != 0)
                    {
                        param = column;
                    }

                }

                //CHECK OF DIT KLOPT!!
                for (int i = 0; i < y.ColumnCount; i++)
                {
                    xbet[i] = (diff[i, numy] / diff[i, param]);
                }

                // add to beta
                for (int i = 0; i < y.ColumnCount; i++)
                {
                    beta[i, param] = xbet[i];
                }

            }
            //matrix with beta's
            return beta;
        }

        // function to estimate outputs
        public static double FirstFunction(Matrix b, double[] x, double[] minmax)
        {
            //get function values
            double[] y = new double[b.RowCount];
            double[] tempy = new double[b.RowCount];
            for (int row = 0; row < y.Length; row++)
            {
                for (int column = 0; column < x.Length; column++)
                {
                    tempy[row] = b[row, column] * x[column];
                }
            }

            //change values so they all can be minimised
            for (int val = 0; val < y.Length; val++)
            {
                if (minmax[val] == 1)
                {
                    y[val] = -tempy[val];
                }
                else
                {
                    y[val] = tempy[val];
                }
            }

            // make totale score for the function to minimilise?
            double total = new double();
            total = 0;
            for (int array = 0; array < y.Length; array++)
            {
                total += y[array];
            }

            return total;
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Retrieve data and validate
            Matrix output = default(Matrix);
            Matrix input = default(Matrix);

            // Use the DA object to retrieve the data inside the first input parameter.
            // If the retieval fails (for example if there is no data) we need to abort.
            if (!DA.GetData(0, ref output)) { return; };
            if (!DA.GetData(1, ref input)) { return; };

            // Variables made from the input
            var totobs = input.RowCount;
            var totpar = input.ColumnCount;
            var outs = output.ColumnCount;
            //int obs = output.RowCount;
            int bools = new int();
            int parameters = new int();

            // Create min/max array (first row of output) to give to function
            double[] minmax = new double[outs];
            for (int space = 0; space < outs; space++)
            {
                minmax[space] = output[0, space];
            }

            // Make output without first row (max(1)/min(0)) to put in regression
            Matrix y = new Matrix(totobs, outs);
            for (int i = 1; i <= totobs; i++)
            {
                for (int j = 0; j < outs; j++)
                {
                    y[i - 1, j] = output[i, j];
                }
            }

            // Check number of booleans and parameeters
            for (int column = 0; column < totpar; column++)
            {
                if (input[0, column] == 0 || input[0, column] == 1)
                {
                    bools++;
                }
                else
                {
                    parameters++;
                }
            }

            // # of rows of the blocks that will be made from the matrices
            int block = parameters + 1;
            int obs = totobs / block;

            // Create matrix where the boolsetting with their function values get saved (befor loop!!)
            Matrix stat = new Matrix(obs, bools + 1);
            int row = 0;

            for (int set = 0; set < totobs; set += block)
            {
                // Create a block of the matching inputs and outputs
                Matrix tempx = new Matrix(block, parameters);
                Matrix tempy = new Matrix(block, outs);
                double[] basex = new double[parameters];
                double[] boolset = new double[bools];

                // Save boolsetting
                for (int j = 0; j < bools; j++)
                {
                    boolset[j] = input[set, j];
                }

                int bound = set + block;
                int rows = 0;

                // Create input/output blocks to put in regression and get beta
                for (int i = set; i < bound; i++)
                {
                    int col = 0;
                    for (int j = bools; j < totpar; j++)
                    {
                        if (i == set)
                        {
                            basex[col] = input[i, j];
                        }
                        tempx[rows, col] = input[i, j];
                        col++;
                    }
                    for (int l = 0; l < outs; l++)
                    {
                        tempy[rows, l] = y[i, l];
                    }
                    rows++;
                }

                // Get beta to put in fn
                Matrix tempb = new Matrix(tempy.ColumnCount, tempx.ColumnCount);
                tempb = Regression(tempx, tempy);

                // Put in regression to get score
                double fnvalue = FirstFunction(tempb, basex, minmax);

                // Put booleansetting with the fn value in matrix in order and find the highest value
                for (int j = 0; j < stat.ColumnCount; j++)
                {
                    if (j != bools)
                    {
                        stat[row, j] = boolset[j];
                    }
                    else
                    {
                        stat[row, j] = fnvalue;
                    }
                }

                row++;
            }

            // find highest value and create uitput parameters
            int best = 0;
            for (int i = 0; i < stat.RowCount; i++)
            {
                //function aanpassen om hoogste value te creeeren
                if (stat[i, bools] < stat[best, bools])
                {
                    best = i;
                }
            }

            Matrix BestBool = new Matrix(1, bools);
            for (int i = 0; i < BestBool.ColumnCount; i++)
            {
                BestBool[0, i] = stat[best, i];
            }

            Matrix bestx = new Matrix(block, parameters);
            Matrix besty = new Matrix(block + 1, outs);
            int start = best * block;
            int rowstart = 0;
            for (int i = start; i < block + start; i++)
            {
                int col = 0;
                for (int j = parameters - 1; j < totpar; j++)
                {
                    bestx[rowstart, col] = input[i, j];
                    col++;
                }
                rowstart++;
            }

            int restart = 0;
            for (int i = start; i <= block + start; i++)
            {
                for (int l = 0; l < outs; l++)
                {
                    if (i == start)
                    {
                        besty[restart, l] = minmax[l];
                    }
                    else
                    {
                        besty[restart, l] = y[i - 1, l];
                    }
                }
                restart++;
            }

            // Return stand vd booleans + matrix of corresponding parameters and ouputs;
            DA.SetData(0, BestBool);
            DA.SetData(1, besty);
            DA.SetData(2, bestx);

        }

        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7a566b63-25e8-4180-9cd1-eeae09cbcc5a"); }
        }
    }
}
