using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using GH = Grasshopper;
using Grasshopper.Kernel.Types;
using Grasshopper.GUI;
using System.Windows.Forms;

namespace Iterator
{
    // Class for the input parameters
    // Here you can create functions/properties for the parameters 
    // TODO: check if some colibri(*) properties are needed
    public class SourceParam
    {
        //Properties
        public InputType GHType { get; set; }

        public string NickName
        {
            get
            {
                //in case source RawParam is updated by user
                if (_nickName != this.RawParam.NickName)
                {
                    _nickName = CheckNickname(this.RawParam);
                }
                return _nickName;
            }
            private set
            {
                _nickName = value;
                RawParam.NickName = _nickName;
                RawParam.Attributes.ExpireLayout();
            }
        }

        // Function of Colibri to get position of index(*)
        public int Position
        {
            get { return _position; }
            private set { _position = value; }
        }
        public int AtIteratorPosition { get; set; }
        public int TotalCount
        {
            get { return _totalCount; }
            set { _totalCount = value; }
        }
        public int IniPosition//(*)
        {
            get { return _iniPosition; }
            set { _iniPosition = value; }
        }

        public IGH_Param RawParam { get; private set; }

        public decimal curValSlid;
        private string _nickName;
        private int _position;
        private int _totalCount;
        private int _iniPosition;

        public int[] MyProperty { get; private set; }

        //Constructor
        public SourceParam()
        {

        }
        public SourceParam(IGH_Param RawParam, int atIteratorPosition)
        {
            this.RawParam = RawParam;
            AtIteratorPosition = atIteratorPosition;

            //check Type
            this.GHType = GetGHType(this.RawParam);
            

            this.NickName = CheckNickname(this.RawParam);

            RawParam.ObjectChanged += RawParam_ObjectChanged;

            CalIniPosition();

        }

        public delegate void nameChangedHandler(SourceParam sender);
        public event nameChangedHandler ObjectNicknameChanged;


        private void RawParam_ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            if (e.Type == GH_ObjectEventType.NickName)
            {
                _nickName = CheckNickname(this.RawParam);

                if (this.ObjectNicknameChanged != null)
                {
                    this.ObjectNicknameChanged(this);
                }


            }
        }

        //Methods (boolean/sliders)
        private InputType GetGHType(IGH_Param RawParam)
        {
            var rawParam = RawParam;
            //Check raw param if is null first
            if (rawParam == null)
            {
                return InputType.Unsupported;
            }
            else if (rawParam is GH_NumberSlider)
            {
                return InputType.Slider;

            }
            else if (rawParam is GH_BooleanToggle)
            {
                return InputType.Boolean;
            }
            else
            {
                return InputType.Unsupported;
            }
        }

        private string CheckNickname(IGH_Param RawParam)
        {
            //Check nicknames
            string nickName = RawParam.NickName;

            //check slider's Implied Nickname
            if (GHType == InputType.Slider)
            {
                var slider = RawParam as GH_NumberSlider;
                nickName = String.IsNullOrEmpty(slider.NickName) ? slider.ImpliedNickName : slider.NickName;
            }

            //check if is empty
            var isNicknameEmpty = String.IsNullOrEmpty(nickName) || nickName == "List" || nickName == "Input";
            if (isNicknameEmpty)
            {
                nickName = "RenamePlz";
            }
            else
            {
                //remove "." and ","
                nickName = nickName.Replace('.', ' ').Replace(',', ' ');
            }

            return nickName;
        }
        
        public string CurrentValue()
        {

            var rawParam = this.RawParam;
            string currentValue = string.Empty;

            if (GHType == InputType.Slider)
            {
                var slider = rawParam as GH_NumberSlider;
                currentValue = slider.CurrentValue.ToString();

            }
            else if (GHType == InputType.Boolean)
            {
                var bools = rawParam as GH_BooleanToggle;
                currentValue = bools.Value.ToString();
            }
            else
            {
                currentValue = "Please use Slider or Boolean!";
            }

            return currentValue;

        }

        // Value of the slider -> get value in decimal instead of string
        public decimal CurrentValueSlider()
        {

            var rawParam = this.RawParam;
            decimal curValSlid = 0;

            if (GHType == InputType.Slider)
            {
                var slider = rawParam as GH_NumberSlider;
                // nu de waarde niet de string valu (veranderen als ook bools)
                curValSlid = slider.CurrentValue;

            }

            return curValSlid;

        }

        // Get the maximum value of the slider
        public decimal GetMax()
        {

            var rawParam = this.RawParam;
            decimal max = 0;

            if (GHType == InputType.Slider)
            {
                var slider = rawParam as GH_Slider;
                // nu de waarde niet de string valu (veranderen als ook bools)
                max = slider.Maximum;

            }

            return max;

        }

        // Get the minimum value of the slider
        public decimal GetMin()
        {

            var rawParam = this.RawParam;
            decimal min = 0;

            if (GHType == InputType.Slider)
            {
                var slider = rawParam as GH_Slider;
                // nu de waarde niet de string valu (veranderen als ook bools)
                min = slider.Minimum;

            }

            return min;

        }

        // Get the size of the step if needed
        public decimal SizeSteps()
        {

            var param = this.RawParam;
            var step = 0;
            var count = 0;


            //Slider ->proberen minmax eruit te halen
            if (GHType == InputType.Slider)
            {
                var mySlider = param as GH_NumberSlider;
                count = mySlider.TickCount;
                step = 1 / TotalCount;
            }

            else
            {
                step = 0;
            }

            return step;
        }

        //(*)
        private void CalIniPosition()
        {

            var rawParam = this.RawParam;
            int position = this._position;


            if (GHType == InputType.Slider)
            {
                var slider = rawParam as GH_NumberSlider;
                position = slider.TickValue;
            }
            //else if (GHType == InputType.Boolean)
            //{
            //    var bools = rawParam as GH_Boolean;
            //    position = bools.Value.ToString();
            //}
            else
            {
                position = 0;
            }

            this._position = position;

        }

        // Set param to an value
        public void SetParamTo(int SetToStepIndex)
        {

            var param = this.RawParam;

            this._position = SetToStepIndex;

            if (GHType == InputType.Slider)
            {
                var slider = param as GH_NumberSlider;
                slider.TickValue = _position;

            }
            // if possible set value to boolean
            //else if (GHType == InputType.Boolean)
            //{
            //    //this.RawParam.ExpireSolution(false);
            //}
            this.RawParam.ExpireSolution(false);


        }

        // Set value of slider one step further
        public void SetToNext()
        {
            var param = this.RawParam;

            if (GHType == InputType.Slider)
            {
                var slider = param as GH_NumberSlider;
                slider.TickValue -= 1;
                //slider.ExpireSolution(true);

            }
        }

        // Set value sldier back to initial value
        public void SetBack()
        {
            var param = this.RawParam;

            if (GHType == InputType.Slider)
            {
                var slider = param as GH_NumberSlider;
                slider.TickValue -= 1;
                //slider.ExpireSolution(true);

            }
        }

        public void Reset()//(*)?
        {
            SetParamTo(0);
        }

        //this convert current step position to ValueList state string.
        private string indexToValueListState(int positionIndex)
        {
            int position = positionIndex < _totalCount ? positionIndex : 0;
            string state = new String('N', _totalCount - 1);
            state = state.Substring(0, position) + "Y" + state.Substring(position);
            return state;

        }



        // Override method
        public override string ToString()
        {
            string currentValue = CurrentValue();

            return currentValue;
        }

        public string ToString(bool withNames)
        {

            string currentValue = "[" + this.NickName + "," + CurrentValue() + "]";

            return currentValue;
        }

    }
}
