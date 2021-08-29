using System.Collections.Generic;
using times.Functions.Entities;

namespace times.Common.Models
{
    internal class ConsolidatedTimes
    {
        public int id { get; set; }
        public List<TimeEntity> employeeTimes { get; set; }
    }
}
