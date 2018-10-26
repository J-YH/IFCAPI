using System;
using System.Collections.Generic;
using System.Text;

namespace IFCAPI.IFCObj
{
    public class P21id
    {
        public int _P21id { get; set; }

        public P21id(int id)
        {
            _P21id = id;
        }
        public P21id(string ifcValue)
        {
            ifcValue = ifcValue.Replace("#", "");
            _P21id = Convert.ToInt32(ifcValue);
        }
    }
}
