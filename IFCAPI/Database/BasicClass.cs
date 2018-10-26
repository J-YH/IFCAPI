using System;
using System.Collections.Generic;
using System.Text;

namespace IFCAPI.Database
{
    internal class IFCPair
    {
        public string ifcKey;
        public string ifcValue;
        public IFCPair(string _ifcKey, string _ifcValue)
        {
            ifcKey = _ifcKey;
            ifcValue = _ifcValue;
        }
    };

    internal class IFCData
    {
        private string _ifcP21ID;
        public string ifcName;
        public string entityName
        {
            get { return ifcName; }
            set { ifcName = value; }
        }
        public string ifcP21ID
        {
            get { return _ifcP21ID; }
            set
            {
                _ifcP21ID = value;
            }
        }

        public string ifcContent;
        public IFCData(string _ifcP21ID, string _ifcEntityName, string _ifcContent)
        {
            ifcP21ID = _ifcP21ID;
            ifcName = _ifcEntityName;
            ifcContent = _ifcContent;
        }
        public IFCData()
        {
            ifcP21ID = "";
            ifcName = "";
            ifcContent = "";
        }
        public string[] getMemberArray()
        {
            return ifcContent.Split(',');
        }

    };


}
