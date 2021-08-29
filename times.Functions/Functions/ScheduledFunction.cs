using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using times.Common.Models;
using times.Functions.Entities;

namespace times.Functions.Functions
{
    public static class ScheduledFunction
    {
        [FunctionName("ScheduledFunction")]
        public static async Task Run(
            //Created consolidation if 'isConsolidated' = true, every N minutes
            [TimerTrigger("0 */1 * * * *")] TimerInfo myTimer,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            [Table("consolidated", Connection = "AzureWebJobsStorage")] CloudTable consolidatedTable,
            ILogger log)
        {
            log.LogInformation($"Consolidated completed function executed at: {DateTime.Now}");

            string filter = TableQuery.GenerateFilterConditionForBool("isConsolidated", QueryComparisons.Equal, false);
            TableQuery<TimeEntity> query = new TableQuery<TimeEntity>().Where(filter);
            TableQuerySegment<TimeEntity> completedTimes = await timeTable.ExecuteQuerySegmentedAsync(query, null);


            List<ConsolidatedTimes> times = new List<ConsolidatedTimes>();
            List<TimeEntity> employee_times = null;

            foreach (IGrouping<int, TimeEntity> grouping in completedTimes.GroupBy(t => t.employeId).Where(t => t.Count() != 1))
            {
                int dato = int.Parse(string.Format("{0}", grouping.Key, grouping.Count()));
                ConsolidatedTimes time = new ConsolidatedTimes();
                employee_times = new List<TimeEntity>();
                time.id = dato;
                foreach (TimeEntity item in completedTimes)
                {
                    if (dato == item.employeId)
                    {
                        employee_times.Add(item);
                    }
                }
                //Order by date
                employee_times.Sort((x, y) => DateTime.Compare(x.date, y.date));
                time.employeeTimes = employee_times;
                times.Add(time);
            }

            foreach (ConsolidatedTimes item in times)
            {
                int hours = 0, min = 0, totalMinutes = 0, cont = 0;
                DateTime employeDate = new DateTime();
                string entryRowKey = "";
                foreach (TimeEntity em in item.employeeTimes)
                {
                    cont += 1;
                    if ((cont % 2) != 0)
                    {
                        hours += em.date.Hour;
                        min += em.date.Minute;
                        entryRowKey = em.RowKey;
                    }
                    else
                    {
                        hours = em.date.Hour - hours;
                        if (min > 0)
                        {
                            totalMinutes += ((hours * 60) + em.date.Minute) - min;
                        }
                        else
                        {
                            totalMinutes += (hours * 60) + em.date.Minute;
                        }

                        updateIsConsolidatedState(entryRowKey, timeTable);
                        updateIsConsolidatedState(em.RowKey, timeTable);

                        employeDate = em.date;
                        hours = 0;
                        min = 0;
                        entryRowKey = "";
                    }
                }

                string consolidated_filter = TableQuery.GenerateFilterConditionForInt("employeId", QueryComparisons.Equal, item.id);
                TableQuery<TimeEntity> consolidated_query = new TableQuery<TimeEntity>().Where(consolidated_filter);
                TableQuerySegment<TimeEntity> existsConsolidated = await consolidatedTable.ExecuteQuerySegmentedAsync(consolidated_query, null);

                if (existsConsolidated.Results.Count != 0)
                {
                    foreach (TimeEntity it in existsConsolidated)
                    {
                        if ((it.date.Year == employeDate.Year) &&
                            (it.date.Month == employeDate.Month) &&
                            (it.date.Day == employeDate.Day))
                        {
                            updateIfExistsConsolidated(it.RowKey, consolidatedTable, totalMinutes);
                        }
                        else
                        {
                            createConsolidation(item.id, totalMinutes, consolidatedTable);
                        }
                    }
                }
                else
                {
                    createConsolidation(item.id, totalMinutes, consolidatedTable);
                }

                totalMinutes = 0;
                cont = 0;
            }

        }

        private static async void updateIsConsolidatedState(string rowkey, CloudTable timeTable)
        {
            //Update consolidation status to true
            TableOperation findOperation = TableOperation.Retrieve<TimeEntity>("TIME", rowkey);
            TableResult findResult = await timeTable.ExecuteAsync(findOperation);

            //Update
            TimeEntity time_Entity = (TimeEntity)findResult.Result;
            time_Entity.isConsolidated = true;

            TableOperation add_Operation = TableOperation.Replace(time_Entity);
            await timeTable.ExecuteAsync(add_Operation);
        }

        private static async void updateIfExistsConsolidated(string rowkey, CloudTable consolidatedTable, int minutesWorked)
        {
            //Update consolidation status to true
            TableOperation findOperation = TableOperation.Retrieve<TimeEntity>("CONSOLIDATED", rowkey);
            TableResult findResult = await consolidatedTable.ExecuteAsync(findOperation);

            //Update
            TimeEntity time_Entity = (TimeEntity)findResult.Result;
            time_Entity.minutesWorked = time_Entity.minutesWorked + minutesWorked;

            TableOperation add_Operation = TableOperation.Replace(time_Entity);
            await consolidatedTable.ExecuteAsync(add_Operation);
        }

        private static async void createConsolidation(int id, int totalMinutes, CloudTable consolidatedTable)
        {
            TimeEntity timeEntity = new TimeEntity
            {
                employeId = id,
                date = DateTime.UtcNow,
                minutesWorked = totalMinutes,
                ETag = "*",
                PartitionKey = "CONSOLIDATED",
                RowKey = Guid.NewGuid().ToString(),
            };

            TableOperation addOperation = TableOperation.Insert(timeEntity);
            await consolidatedTable.ExecuteAsync(addOperation);
        }

    }
}
