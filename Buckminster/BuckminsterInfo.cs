using Grasshopper.Kernel;
using System;

namespace Buckminster
{
    public class BuckminsterInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Buckminster";
            }
        }
        public override string Description
        {
            get
            {
                return "Provides components for the generation of structural frames from surfaces using mesh operators and surface modelling techniques.";
            }
        }
        public override string AuthorName
        {
            get
            {
                return "Will Pearson";
            }
        }
        public override string AuthorContact
        {
            get
            {
                return "http://www.pearswj.co.uk";
            }
        }
        public override string Version
        {
            get
            {
                return "0.1.0";
            }
        }
        public override System.Guid Id
        {
            get
            {
                return new Guid("{980e3e9d-c890-49f4-9b56-dcb2a41143dc}");
            }
        }
        public override GH_LibraryLicense License
        {
            get
            {
                return GH_LibraryLicense.developer;
            }
        }

        //Override here any more methods you see fit.
        //Start typing public override..., select a property and push Enter.
    }
}