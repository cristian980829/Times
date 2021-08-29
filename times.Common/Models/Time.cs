using System;
using System.Collections.Generic;
using System.Text;

namespace times.Common.Models
{
    public class Time
    {
        public DateTime Date { get; set;  }

        public int EmployeId { get; set; }

        public int Type { get; set; }

        public bool IsConsolidated { get; set; }

        public float MinutesWorked { get; set; }
    }
}
