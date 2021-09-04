using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace times.Functions.Entities
{
    public class TimeEntity : TableEntity
    {
        public DateTime Date { get; set; }

        public int EmployeId { get; set; }

        public int? Type { get; set; }

        public bool? IsConsolidated { get; set; }

    }
}
