using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace times.Functions.Entities
{
    public class ConsolidatedEntity : TableEntity
    {
        public DateTime Date { get; set; }

        public int EmployeId { get; set; }

        public double MinutesWorked { get; set; }
    }
}
