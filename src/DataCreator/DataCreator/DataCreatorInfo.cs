using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace DataCreator
{
    public class DataCreatorInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "DataCreator Info";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("9fd710ed-96bd-420b-af24-0bdc65a2c205");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
