using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace times.Functions.Entities
{
    public class TimeEntity : TableEntity
    {
        public DateTime date { get; set; }

        public int employeId { get; set; }

        public int? type { get; set; }

        public bool? isConsolidated { get; set; }

        public int? minutesWorked { get; set; }
    }
}
