using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using times.Functions.Entities;

namespace times.Functions.Functions
{
    public static class ScheduledFunction
    {
        [FunctionName("ScheduledFunction")]
        public static async Task Run(
            //Create consolidation if 'isConsolidated' = true, every N minutes.
            [TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            [Table("consolidated", Connection = "AzureWebJobsStorage")] CloudTable consolidatedTable,
            ILogger log)
        {
            log.LogInformation($"Consolidated completed function executed at: {DateTime.Now}");

            string filter = TableQuery.GenerateFilterConditionForBool("IsConsolidated", QueryComparisons.Equal, false);
            TableQuery<TimeEntity> query = new TableQuery<TimeEntity>().Where(filter);
            TableQuerySegment<TimeEntity> unconsolidatedTimes = await timeTable.ExecuteQuerySegmentedAsync(query, null);

            List<TimeEntity> employee_times = unconsolidatedTimes.OrderBy(t => t.EmployeId).ThenBy(e => e.Date).ToList();

            DateTime employeDate = new DateTime();
            TimeSpan difference;
            string entryRowKey = "";
            double totalMinutes = 0;
            int id = -1;
            string RowKeyLastEmployee = employee_times.Last().RowKey;
            foreach (TimeEntity em in employee_times)
            {
                if (id == -1)
                {
                    id = em.EmployeId;
                }

                if (id != em.EmployeId && id != -1)
                {
                    CreateOrUpdateConsolidation(id, totalMinutes, consolidatedTable, employeDate);
                    id = em.EmployeId;
                    totalMinutes = 0;
                }

                if (em.Type == 0)
                {
                    employeDate = em.Date;
                    entryRowKey = em.RowKey;
                }
                else
                {
                    difference = em.Date - employeDate;
                    totalMinutes += difference.TotalMinutes;

                    UpdateIsConsolidatedState(entryRowKey, timeTable);
                    UpdateIsConsolidatedState(em.RowKey, timeTable);

                    employeDate = em.Date;
                    entryRowKey = "";
                    if (RowKeyLastEmployee == em.RowKey)
                    {
                        CreateOrUpdateConsolidation(id, totalMinutes, consolidatedTable, employeDate);
                    }
                }
            }
        }

        private static async void UpdateIsConsolidatedState(string rowkey, CloudTable timeTable)
        {
            //Update consolidation status to true
            TableOperation findOperation = TableOperation.Retrieve<TimeEntity>("TIME", rowkey);
            TableResult findResult = await timeTable.ExecuteAsync(findOperation);

            //Update
            TimeEntity time_Entity = (TimeEntity)findResult.Result;
            time_Entity.IsConsolidated = true;

            TableOperation add_Operation = TableOperation.Replace(time_Entity);
            await timeTable.ExecuteAsync(add_Operation);
        }

        private static async void UpdateIfExistsConsolidated(string rowkey, CloudTable consolidatedTable, double minutesWorked)
        {
            //Update consolidation status to true
            TableOperation findOperation = TableOperation.Retrieve<TimeEntity>("CONSOLIDATED", rowkey);
            TableResult findResult = await consolidatedTable.ExecuteAsync(findOperation);

            //Update
            TimeEntity time_Entity = (TimeEntity)findResult.Result;
            time_Entity.MinutesWorked += minutesWorked;

            TableOperation add_Operation = TableOperation.Replace(time_Entity);
            await consolidatedTable.ExecuteAsync(add_Operation);
        }

        private static async void CreateConsolidation(int id, double totalMinutes, CloudTable consolidatedTable)
        {
            TimeEntity timeEntity = new TimeEntity
            {
                EmployeId = id,
                Date = DateTime.UtcNow,
                MinutesWorked = totalMinutes,
                ETag = "*",
                PartitionKey = "CONSOLIDATED",
                RowKey = Guid.NewGuid().ToString(),
            };

            TableOperation addOperation = TableOperation.Insert(timeEntity);
            await consolidatedTable.ExecuteAsync(addOperation);
        }

        private static async void CreateOrUpdateConsolidation(int id, double totalMinutes, CloudTable consolidatedTable, DateTime employeDate)
        {
            string consolidated_filter = TableQuery.GenerateFilterConditionForInt("EmployeId", QueryComparisons.Equal, id);
            TableQuery<TimeEntity> consolidated_query = new TableQuery<TimeEntity>().Where(consolidated_filter);
            TableQuerySegment<TimeEntity> existsConsolidated = await consolidatedTable.ExecuteQuerySegmentedAsync(consolidated_query, null);

            if (existsConsolidated.Results.Count != 0)
            {
                foreach (TimeEntity it in existsConsolidated)
                {
                    if (it.Date.ToString("dd-MM-yyyy").Equals(employeDate.Date.ToString("dd-MM-yyyy")))
                    {
                        UpdateIfExistsConsolidated(it.RowKey, consolidatedTable, totalMinutes);
                    }
                    else
                    {
                        CreateConsolidation(id, totalMinutes, consolidatedTable);
                    }
                }
            }
            else
            {
                CreateConsolidation(id, totalMinutes, consolidatedTable);
            }
        }



    }
}
