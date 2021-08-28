using System;
using System.Collections.Generic;
using System.Text;

namespace times.Common.Models
{
    public class Time
    {
        public DateTime Date { get; set;  }

        public int employeId { get; set; }

        public int type { get; set; }

        public bool isConsolidated { get; set; }
    }
}
