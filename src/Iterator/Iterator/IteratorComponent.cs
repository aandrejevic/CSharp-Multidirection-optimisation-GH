using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using GH = Grasshopper;
using System.Linq;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Data;

using Grasshopper.Kernel.Types;
using GH_IO.Serialization;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Security.Permissions;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Iterator
{
    public class IteratorComponent : GH_Component, IGH_VariableParameterComponent
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public IteratorComponent()
          : base("Iterator", "Iterator",
              "Iterates sliders en bools to get the correct data for the optimisation.",
              "Maths", "Script")
        {
        }
       
        // Boolean to start running iteration
        private bool _run = false;

        // ints to keep track of number of booleans, slides and in total
        private int TotalBool = 0;
        private int TotalSlid = 0;
        private int Total = 0;

        // Matrix for the output of the min and max values of the sliders
        private Matrix minmax;
        
        // To make the input parameter in the class 
        private List<SourceParam> _filteredSources;


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Sliders", "Sliders", "Please connect a Sliders as a variable input", GH_ParamAccess.list);
            pManager[0].Optional = true;
            pManager[0].MutableNickName = false;

            // When its clear how to SET value top boolean then boolean can also be an input
            //pManager.AddBooleanParameter("Booleans", "Bools", "Please connect a Boolean as a variable input.", GH_ParamAccess.item);
            //pManager[1].Optional = true;
            //pManager[1].MutableNickName = false;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Min/max value of sldier output
            pManager.AddMatrixParameter("MinMax", "MinMax", "Give back the minimal and maximum values of the sliders", GH_ParamAccess.item);
        }

        // Funstion to check number of sldiers and bools (can maybe be put in to the Source class?)
        private void NumSlidBool(List<SourceParam> _filteredSources)
        {
            foreach (var item in _filteredSources)
            {
                this.Total++;
                if (item.GHType == InputType.Boolean)
                {
                    this.TotalBool++;
                }
                if (item.GHType == InputType.Slider)
                {
                    this.TotalSlid++;
                }
            }
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Get data info or set a recorder on the variables
            //decimal[] CurrentValue = new decimal[TotalSlid];
            //int i = 0;

            //foreach (var item in _filteredSources)
            //{ 
            //    if (item.GHType == InputType.Slider)
            //   {
            //        CurrentValue[i] = item.CurrentValueSlider();
            //        i++;
            //    }
            //    
            //}

            // if running set each slider one step forward and then back, then next slider
            // if possible to set value to boolean -> do the sliders per boolean setting
            if (_run)
            {
                //NumSlidBool(_filteredSources); -> necessary when bool inputs

                for (int i = 0; i < _filteredSources.Count; i++)
                {
                    var source = _filteredSources[i];
                    var curval = source.CurrentValueSlider();
                    var step = source.SizeSteps();

                    // get minmax of source 
                    var min = source.GetMin();
                    var max = source.GetMax();
                    minmax[i, 0] = Convert.ToDouble(min);
                    minmax[i, 1] = Convert.ToDouble(max);

                    source.SetToNext();
                    source.SetBack();
                }
            }

            DA.SetData(0, minmax);
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
            get { return new Guid("37c25ef2-bc71-4d33-9a37-c132137ecfe4"); }
        }

        // Create Button
        public override void CreateAttributes()
        {
            var newButtonAttribute = new IteratorAttributes(this) { ButtonText = "Execute" };
            //windows forms
            newButtonAttribute.mouseDownEvent += OnMouseDownEvent;
            m_attributes = newButtonAttribute;

        }

        // TODO: automatically extra input place instead of clicking on the plus sign
        #region Methods of IGH_VariableParameterComponent interface
        public bool CanInsertParameter(GH_ParameterSide side, int index)
        {
            bool isInputSide = side == GH_ParameterSide.Input;

            bool isTheOnlyInput = index == 0;
            
            bool isSetting = false;
            //We only let input parameters to be added 
            if (isInputSide && !isSetting && !isTheOnlyInput)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public bool CanRemoveParameter(GH_ParameterSide side, int index)
        {
            bool isInputSide = side == GH_ParameterSide.Input;
            //bool isTheFlyButton = index == this.Params.Input.Count-1;
            bool isTheOnlyInput = (index == 0) && (this.Params.Input.Count <= 2);
            
            bool isSetting = index == this.Params.Input.Count;

            //can only remove from the input and non Fly? or the first Slider
            if (isInputSide && !isSetting && !isTheOnlyInput)
            {
                return true;
            }
            else
            {
                return false;
            }

        }


        public IGH_Param CreateParameter(GH_ParameterSide side, int index)
        {
            // add normal params
            var inParam = new Param_GenericObject();
            inParam.NickName = String.Empty;
            return inParam;
        }

        public bool DestroyParameter(GH_ParameterSide side, int index)
        {
             return true;
        }
        
        public void VariableParameterMaintenance()
        {
            int inputParamCount = this.Params.Input.Count;

            for (int i = 0; i < inputParamCount; i++)
            {
                // create inputs
                var inParam = this.Params.Input[i];
                if (inParam.NickName == String.Empty)
                {
                    inParam.NickName = "Input[N]";
                }
                inParam.Name = "Input";
                inParam.Description = "Please connect a Slider or Boolean";
                inParam.Access = GH_ParamAccess.list;
                inParam.Optional = true;
                inParam.MutableNickName = false;
                inParam.WireDisplay = GH_ParamWireDisplay.faint;
            }

        }


        #endregion

        //This is for if any source name changed, NOTE: cannot deteck the edited
        private void OnSourceNicknameChanged(SourceParam sender)
        {
            //bool isExist = _filteredSources.Exists(_ => _.RawParam.Equals(sender));
            bool isExist = _filteredSources.Contains(sender);

            if (isExist)
            {
                //todo: this can be finished inside SourceParam
                CheckInputParamNickname(sender);
            }
            else
            {
                sender = null;
            }

        }

        /// <summary>
        /// convert current selected source to SourceParam
        /// </summary>   
        private SourceParam ConvertToParam(IGH_Param SelectedSource, int AtIteratorPosition)
        {
            //var component = SelectedSource; //list of things connected on this input
            SourceParam SourceParam = new SourceParam(SelectedSource, AtIteratorPosition);

            return SourceParam;
        }

        /// <summary>
        /// Change Iterator's input and output NickName
        /// </summary>   
        private void CheckInputParamNickname(SourceParam ValidSourceParam)
        {

            if (ValidSourceParam != null)
            {
                var SourceParam = ValidSourceParam;

                int atPosition = SourceParam.AtIteratorPosition;

                var inputParam = this.Params.Input[atPosition];

                string newNickname = SourceParam.NickName;
                inputParam.NickName = newNickname;
                this.Attributes.ExpireLayout();
            }

        }

        /// <summary>
        /// check and iterator's input and output params' nicknames
        /// </summary>   
        private void checkAllInputParamNames(List<SourceParam> validColibriParams)
        {
            //all items in the list are unnull. which is checked in gatherSources();

            if (validColibriParams.Count == 0) return;

            foreach (var item in validColibriParams)
            {
                //checkSourceParamNickname(item);
                CheckInputParamNickname(item);
            }
        }

        /// <summary>
        /// check input source if is slider or boolean, if not, remove it
        /// </summary>   
        private List<SourceParam> GatherSources()
        {
            var filtedSources = new List<SourceParam>();
            
            // exclude the last input which is "Selection" 
            for (int i = 0; i < this.Params.Input.Count; i++)
            {
                //Check if it is fly or empty source param
                //bool isFly = i == this.Params.Input.Count - 1 ? true : false;
                var source = this.Params.Input[i].Sources;

                if (source.Any() && source.Count == 1)
                {
                    //if something's connected,and get the last connected
                    var SourceParam = ConvertToParam(source.Last(), i);

                    //null  added if input is unsupported type IN INPUTCLASS
                    if (SourceParam.GHType != InputType.Unsupported)
                    {
                        SourceParam.ObjectNicknameChanged += OnSourceNicknameChanged;
                        filtedSources.Add(SourceParam);

                    }
                    else
                    {
                        //throw new ArgumentException("Unsupported component!\nPlease use Slider, ValueList, or Panel!");
                        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unsupported component!\nPlease use Slider or Boolean!");
                        break;
                        //return null;
                    }

                }
                else if (source.Count > 1)
                {
                    //throw new ArgumentException("Please connect one component per grip!");
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please connect one component per grip!");
                    break;
                    //return null;
                }
            }

            return filtedSources;
        }

        #region Button Event on Iterator

        // TODO: the current fault is in here
        // -> when clicked on the button the input parameters change name etc, instead of running!!!
        // response to Button event
        private void OnMouseDownEvent(object sender)
        {
            // remote run fn 
            // TODO: check if this is the right place for the functions or put in solveinstance or somewhere else!
            VariableParameterMaintenance();
            this.Params.OnParametersChanged();
            this.ExpireSolution(true);

            if (this.RuntimeMessageLevel == GH_RuntimeMessageLevel.Error) return;

            //recollect all params 
            this._filteredSources = GatherSources();

            this._filteredSources.RemoveAll(item => item == null); // fn in input class als goed is
            this._filteredSources.RemoveAll(item => item.GHType == InputType.Unsupported);
            
            //check if any vaild input source connected to Iteratior
            if (this._filteredSources.Count() == 0)
            {
                // message box kan ook weggelaten worden als niet werkt, is niet van belang!!!! (alleen extra)
                MessageBox.Show("No Slider or Boolean!");
                return;
            }

            // Start running of the SolveInstance
            if (sender is true)
            {
                _run = true; 
                
            }
            else
            {
                _run = false;
            }


        }
        
        #endregion
    }
}
