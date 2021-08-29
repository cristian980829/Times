using System.Collections.Generic;
using times.Functions.Entities;

namespace times.Common.Models
{
    internal class ConsolidatedTimes
    {
        public int Id { get; set; }
        public List<TimeEntity> EmployeeTimes { get; set; }
    }
}
