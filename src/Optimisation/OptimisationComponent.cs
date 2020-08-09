using System; using System.Collections.Generic; using System.Linq; using System.Text; using Grasshopper; using Grasshopper.Kernel; using Rhino.Geometry;

namespace Optimisation
{
    public class OptimisationComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public OptimisationComponent()
          : base("OptimisationComponent", "Opt",
            "Optimises the parameters to get a good estimation of the global min or max",
            "Maths", "Script")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Input variables (all matrix) observed iputs (X) and outputs(Y, with first row min/max) and interval of X 
            pManager.AddMatrixParameter("Y", "Y", "Y variable", GH_ParamAccess.item);             pManager.AddMatrixParameter("X", "X", "X variable", GH_ParamAccess.item);             pManager.AddMatrixParameter("Interval", "Interval", "Interval of x variables", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Output is a matrix with the optimal values of X
            pManager.AddMatrixParameter("Optimal X", "OX", "The optimal value of parameters after the algorithm", GH_ParamAccess.item);
        }

        // Regression class to get beta's for final function to optimise
        public static Matrix Regression(Matrix x, Matrix y)         {             //make regression to get beta for each variable             // first a base functions             Matrix basic = new Matrix(y.ColumnCount, x.ColumnCount + 1);              for (int i = 0; i < basic.RowCount; i++)             {                 for (int j = 0; j < basic.ColumnCount; j++)                 {                     if ((x != null) && (j < x.ColumnCount))                     {                         basic[i,j] = x[0,j];                     }                     else                     {                         basic[i,j] = y[0, i];                     }                 }             }              // create return beta matrix             Matrix beta = new Matrix(y.ColumnCount, x.ColumnCount);              int numy = x.ColumnCount;              // make temp matrices to compare to base and create beta's to put in matrix             for (int row = 1; row < x.RowCount; row++)             {                 Matrix temp = new Matrix(y.ColumnCount, x.ColumnCount + 1);                 Matrix diff = new Matrix(y.ColumnCount,x.ColumnCount + 1);                 for (int i = 0; i < temp.RowCount; i++)                 {                     for (int j = 0; j < temp.ColumnCount; j++)                     {                         if ((x != null) && (j < x.ColumnCount))                         {                             temp[i,j] = x[row,j];                         }                         else                         {                             temp[i,j] = y[row, i];                         }                     }                 }                   for (int i = 0; i < temp.RowCount; i++)                 {                     for (int j = 0; j < temp.ColumnCount; j++)                     {                         diff[i, j] = basic[i, j] - temp[i, j];                     }                 }                  // create array for the right column in final beta matrix                 int param = new int();                 double[] xbet = new double[y.RowCount];                 for (int column = 0; column < x.ColumnCount; column++)                 {                     if (diff[0, column] != 0)                     {                         param = column;                     }                  }                  for (int i = 0; i < y.ColumnCount; i++)                 {                     xbet[i] = (diff[i, numy]/diff[i, param]);                 }
                 // add to beta                 for (int i = 0; i < y.ColumnCount; i++)                 {                     beta[i, param] = xbet[i];
                }
             }
            //matrix with beta's
            return beta;         }          public static double FirstFunction(Matrix b, double[] x, double[] minmax)         {             //get function values
            double[] y = new double[b.RowCount];             double[] tempy = new double[b.RowCount];             for (int row = 0; row < y.Length; row++)             {                 for (int column = 0; column < x.Length; column++)                 {                     tempy[row] = b[row,column] * x[column];                 }             }

            //change values so they all can be minimised
            for (int val = 0; val < y.Length; val++)             {                 if (minmax[val] == 1)                 {                     y[val] = -tempy[val];                 }                 else                 {                     y[val] = tempy[val];                 }             }              // make totale score for the function to minimilise?             double total = new double();             total = 0;             for (int array = 0; array < y.Length; array++)
            {
                total += y[array];
            }              return total;         }

        /// <summary>         /// This is the method that actually does the work.         /// </summary>         /// <param name="DA">The DA object can be used to retrieve data from input parameters and          /// to store data in output parameters.</param>         protected override void SolveInstance(IGH_DataAccess DA)         {
            // Retrieve data and validate
            Matrix output = default(Matrix);             Matrix x = default(Matrix);             Matrix intval = default(Matrix);              // Use the DA object to retrieve the data inside the first input parameter.             // If the retrieve fails (for example if there is no data) we need to abort.             if (!DA.GetData(0, ref output)) { return; }             if (!DA.GetData(1, ref x)) { return; }             if (!DA.GetData(2, ref intval)) { return; }              // create standard variables             var parameters = x.ColumnCount;             var outs = output.ColumnCount;             var obs = x.RowCount;             var N = parameters;              // boolean to get out of iteration loop             bool notdone = true;              // create min/max array (first row of output) to give to function             double[] minmax = new double[outs];             for (int space = 0; space < outs; space++)             {                 minmax[space] = output[0, space];             }              // make output without first row (max(1)/min(0)) to put in regression             Matrix y = new Matrix(obs, outs);             for (int i = 1; i < obs; i++)
            {
                for (int j = 0; j < outs; j++)
                {
                    y[i - 1, j] = output[i,j];
                }
            }

            // base value of the variables to put in simplex
            double[] xbase = new double[N];             for (int i = 0; i < N; i++)
            {
                xbase[i] = x[0, i];
            }

            // create beta matrix for the function
            Matrix beta = new Matrix(outs, N);             beta = Regression(x, y);             // base value y with first x values             double baseY = FirstFunction(beta, xbase, minmax);
            
            // Create step value array to now the size steps of slider
            double[] steps = new double[N];             for (int i = 0; i < N; i++)
            {
                double step = 0;                 double[] diff = new double[N];                 for (int j = 0; j < N; j++)
                {
                    diff[j] = x[i + 1, j] - xbase[j];                     if (diff[j] != 0)
                    {
                        step = diff[j];
                    }
                }                 steps[i] = step;
            } 
            // Create "simplex" [min, base, max]
            Matrix simplex = new Matrix(N, 3);             for (int i = 0; i < N; i++)             {                 simplex[i, 0] = intval[i, 0];                 simplex[i, 1] = xbase[i];                 simplex[i, 2] = intval[i, 1];
            }
             // array to decide if the parameter is going up or down in value              double[] updown = new double[N];             for(int i = 0; i < N; i++)
            {
                for (int j = 0; j < outs; j++)
                {                     // When beta pos + y max or beta neg and y is min => value to max
                    if (beta[j, i] > 0 && minmax[j] == 1)
                    {
                        updown[i] += 1;
                        continue;
                    }                     if (beta[j, i] < 0 && minmax[j] == 0)                     {                         updown[i] += 1;                         continue;                     }
                    // When the beta sign is 'against' the type of opt => value to min
                    if (beta[j, i] < 0 && minmax[j] == 1)
                    {
                        updown[i] -= 1;
                        continue;
                    }
                    if (beta[j, i] > 0 && minmax[j] == 0)
                    {
                        updown[i] -= 1;
                        continue;
                    }
                }
            }              // create final matrix with values for the X for the output             Matrix final = new Matrix(parameters, 1);              // max value first to max or min for in the loop (go to extreme values)
            double[] maxx = new double[parameters];             for(int i = 0; i < parameters; i++)             {                 // check if the valkue is not already one of the max or min                 if(updown[i] > 0 && xbase[i] != simplex[i,0] && xbase[i] != simplex[i, 2])
                {
                    maxx[i] = simplex[i, 2];
                }                 if(updown[i] < 0 && xbase[i] != simplex[i, 0] && xbase[i] != simplex[i, 2])                 {                     maxx[i] = simplex[i, 0];                 }                 if(updown[i] == 0)                 {                     // TODO: what if zero?                     maxx[i] = simplex[i, 1];                 }
                // if value is already min/max go to other end interval
                if (xbase[i] == simplex[i, 0])
                {
                    maxx[i] = simplex[i, 2];
                }                 if (xbase[i] == simplex[i, 2])
                {
                    maxx[i] = simplex[i, 0];
                }             }

            // calculate y value
            double maxY = FirstFunction(beta, maxx, minmax);

            // create boundary of lowest value of Y
            double bound = 0;
            if (maxY < baseY)
            {
                bound = maxY;
            }
            else
            {
                bound = baseY;
            }

            //start loop until iteration is don/optimal is found
            int iter = 0;
            while (notdone)
            {
                iter++;

                // temp value first to max or min and then check
                double[] tempx = new double[parameters];
                for (int i = 0; i < parameters; i++)
                {
                    tempx[i] = (xbase[i] + maxx[i]) / 2;
                }
                double tempY = FirstFunction(beta, tempx, minmax);

                // base interval if needed
                double interbase = baseY;

                //if (new output < ols output) -> notdone == false -> stop iteration give values in final
                if (tempY < bound)
                {
                    notdone = false;
                    for (int i = 0; i < N; i++)
                    {
                        final[i, 0] = tempx[i];
                    }
                }

                // if lower than base but max values is lower set interval from temp to max values
                if (tempY < interbase && maxY < baseY)
                {
                    for (int i = 0; i < N; i++)
                    {
                        xbase[i] = tempx[i];
                    }
                    baseY = FirstFunction(beta, xbase, minmax);
                }
                else
                {
                    for(int i = 0; i < N; i++)
                    {
                        maxx[i] = tempx[i];
                    }
                    maxY = FirstFunction(beta, maxx, minmax);
                }

                // if iteration is more than 10 make it stop and estimated x of optimasation
                if (iter > 10 && notdone == true)
                {
                    notdone = false;
                    for (int i = 0; i < N; i++)
                    {
                        final[i, 0] = tempx[i];
                    }
                }

                continue;

            }              // Set optimal parameter values as output             DA.SetData(0, final);         } 

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
            get { return new Guid("11b15057-60f8-4c72-983c-ad62ced935c9"); }
        }
    }
}
