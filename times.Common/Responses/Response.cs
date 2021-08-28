using System;
using System.Collections.Generic;
using System.Text;

namespace times.Common.Responses
{
    public class Response
    {
        public bool isSuccesss { get; set; }

        public string message { get; set; }

        public object result { get; set; }
    }
}
