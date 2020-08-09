using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms;

// Is used with colibri for the running function NOW not in use
// Can be used again, it is already changed from original, so maybe first go back to original

namespace Iterator
{
    class RunParam
    {

        //InputParams objects List
        private List<SourceParam> _inputParams;

        public List<SourceParam> InputParams
        {
            get { return _inputParams; }
            set { _inputParams = value; }
        }

        // kan wellicht eruyit, geen nut
        private List<string> _studiedFlyID;

        //List of each input param's all steps index
        private List<List<int>> _allPositions;
        private List<List<int>> _allSelectedPositions;
        private long _selectedCounts;
        private List<int> _iniPositions;
        public int SliderCount;


        //current each position
        public List<int> _currentPositionsIndex { get; set; } //dit is aantal totale inputs-> nmiet meer nodig

        //current counts
        public static int Count { get; private set; }

        // Total Iteration number int
        public Int64 _totalCounts { get; private set; }


        private string _watchFilePath { get; set; }
        private OverrideMode _overrideFolderMode = OverrideMode.AskEverytime;

        private List<List<int>> iterationsFlyList = new List<List<int>>();
        private List<SourceParam> _filteredSources;
        private List<SourceParam> sliders;
        private object _selections;// niet nodig
        private object _studyFolder;
        private object _mode;
        private object totalSlide;

        //constructor 
        public RunParam() { }

        //, IteratorSelection selections
        public RunParam(List<SourceParam> sourceParams, int SliderCount, OverrideMode overrideFolderMode)
        {
            this._inputParams = sourceParams;
            this._overrideFolderMode = overrideFolderMode;

            // counts gaat over aantal iteraties -> BOEIT GEEN FUK!
            //min max verkrijgen?
            Count = 0;


        }

        public RunParam(List<SourceParam> filteredSources, object totalSlide)
        {
            _filteredSources = filteredSources;
            this.totalSlide = totalSlide;
        }


        #region Methods

        //create a watch file  -> niet van belang als geen doc
        private void createWatchFile(string FolderPath)
        {
            if (!string.IsNullOrEmpty(FolderPath))
            {
                this._watchFilePath = FolderPath + "\\running.txt";
                try
                {
                    File.WriteAllText(this._watchFilePath, "running");
                }
                catch (Exception)
                {
                    throw;
                }

            }
        }

        //ndeze functie niet nodig want je wil huidige hebben, kan maar is een extra NIET NODIG VOOR FN
        private void FirstResetAll(bool isTest)
        {

            this._iniPositions = CalIniPositions();

            for (int i = 0; i < InputParams.Count; i++)
            {
                if (isTest)
                {
                    InputParams[i].Reset();
                }
                else
                {
                    InputParams[i].SetParamTo(this._iniPositions[i]);
                }

            }

        }

        private List<int> CalIniPositions()
        {
            var iniPositions = new List<int>();

            foreach (var item in _allSelectedPositions)
            {
                iniPositions.Add(item.First());
            }

            return iniPositions;

        }

        // run funtie aanpassen!!!
        public void Run(List<SourceParam> filteredSources, int slides)
        {
            List<SourceParam> sliders = new List<SourceParam>();
            var curVal = new List<decimal>();
            var newList = new List<decimal>();
            var maxList = new List<decimal>();
            var minList = new List<decimal>();
            _filteredSources = filteredSources;
            // maak matrix van de values om mee te geven als output

            // new list of only sliders
            foreach (var item in filteredSources)
            {
                if (item.GHType == InputType.Slider)
                {
                    sliders.Add(item);
                    curVal.Add(item.CurrentValueSlider());
                }
            }

            //loop over each slider to take one step and go back
            for (int i = 0; i < slides; i++)
            {
                var param = sliders[i].GHType;
                decimal value = sliders[i].CurrentValueSlider();
                curVal.Add(value);
                decimal max = sliders[i].GetMax();
                maxList.Add(max);
                decimal min = sliders[i].GetMin();
                minList.Add(min);
                decimal step = sliders[i].SizeSteps();

                // set slider to new value
                decimal newVal = value + step;
                newList.Add(newVal);

                sliders[i].SetToNext();
                sliders[i].SetBack();
            }

        }

            // doc van waardes die eruit komen
            private bool ListenToKeepRunning()
            {
                //watch the user cancel the process
                if (GH_Document.IsEscapeKeyDown())
                {
                    if (MessageBox.Show("Do you want to stop the process?\nSo far " + Count.ToString() +
                      " out of " + this._selectedCounts.ToString() + " iterations are done!", "Stop?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        // cancel the process by user input!
                        return false;
                    }
                }

                //watch the file to stop
                if (!string.IsNullOrEmpty(_watchFilePath))
                {
                    if (!File.Exists(_watchFilePath))
                    {
                        // watch file was deleted by user
                        return false;
                    }
                }

                return true;
            }

            //from Grasshopper's CrossReference
            private bool NextPosition(List<int> offsets, List<int> lengths, int index)
            {
                if (index >= offsets.Count)
                    return false;
                if (lengths[index] < 0)
                    return this.NextPosition(offsets, lengths, checked(index + 1));
                if (offsets[index] >= lengths[index])
                {
                    offsets[index] = 0;
                    return this.NextPosition(offsets, lengths, checked(index + 1));
                }
                List<int> intList1 = offsets;
                List<int> intList2 = intList1;
                int index1 = index;
                int index2 = index1;
                int num = checked(intList1[index1] + 1);
                intList2[index2] = num;
                return true;
            }

            private List<List<int>> FlyPositions(List<List<int>> inLists)
            {
                List<List<int>> inputLists = inLists;
                List<List<int>> crossReferencedList = new List<List<int>>();
                List<List<int>> positionsOfEachIterations = new List<List<int>>();
                List<int> lengths = new List<int>();
                List<int> offsets = new List<int>();
                List<int> intList = new List<int>();

                for (int i = 0; i < inputLists.Count; i++)
                {
                    if (inputLists[i].Count != 0)
                    {
                        intList.Add(i);
                        offsets.Add(0);
                        lengths.Add(checked(inputLists[i].Count - 1));
                        crossReferencedList.Add(new List<int>());
                    }
                }


                if (offsets.Count == 0)
                    return new List<List<int>>();

                offsets[0] = -1;

                while (this.NextPosition(offsets, lengths, 0))
                {
                    int num10 = 0;
                    int num11 = checked(inputLists.Count - 1);
                    int index8 = num10;
                    while (index8 <= num11)
                    {
                        int index3 = offsets[index8];

                        if (index3 <= lengths[index8])
                        {
                            crossReferencedList[index8].Add(inputLists[index8][index3]);
                        }
                        checked { ++index8; }
                    }
                }

                int iterationCount = crossReferencedList[0].Count;
                int paramCount = crossReferencedList.Count;

                for (int i = 0; i < iterationCount; i++)
                {
                    positionsOfEachIterations.Add(new List<int>());
                    for (int j = 0; j < paramCount; j++)
                    {
                        int setIndex = crossReferencedList[j][i];
                        positionsOfEachIterations.Last().Add(setIndex);
                    }
                }

                return positionsOfEachIterations;
            }

            private string getCurrentFlyID()
            {
                var inputParams = this._inputParams;
                string currentNickname = String.Empty;
                string currentValue = String.Empty;
                string flyID = String.Empty;

                var currentValues = new List<string>();
                for (int i = 0; i < inputParams.Count; i++)
                {
                    currentValue = inputParams[i].CurrentValue();
                    currentValues.Add(currentValue);
                }

                flyID = string.Join("_", currentValues);

                return flyID;
            }

            private List<string> getStudiedFlyID(string FolderPath)
            {
                var inputParams = this._inputParams;
                string csvFilePath = FolderPath + @"\data.csv";
                if (!File.Exists(csvFilePath)) return new List<string>();
                var stringLines = File.ReadAllLines(csvFilePath);
                var studiedFlyID = new List<string>();

                for (int i = 0; i < stringLines.Count(); i++)
                {
                    var itemValues = stringLines[i].Split(',').Take(inputParams.Count);
                    string oneID = string.Join("_", itemValues);
                    studiedFlyID.Add(oneID);
                }

                return studiedFlyID;

            }

            private bool checkIfStudiedFromCSV()
            {
                if (this._overrideFolderMode == OverrideMode.FinishTheRest)
                {
                    return this._studiedFlyID.Contains(getCurrentFlyID());
                }
                else
                {
                    return false;
                }

            }


            #endregion

    }
}
