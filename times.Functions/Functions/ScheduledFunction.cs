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
            //Obtain all times where IsConsolidated=false 
            string filter = TableQuery.GenerateFilterConditionForBool("IsConsolidated", QueryComparisons.Equal, false);
            TableQuery<TimeEntity> query = new TableQuery<TimeEntity>().Where(filter);
            TableQuerySegment<TimeEntity> unconsolidatedTimes = await timeTable.ExecuteQuerySegmentedAsync(query, null);
            //Check for data
            if (unconsolidatedTimes.Results.Count != 0)
            {
                //If there is data, it orders them by id and date
                List<TimeEntity> employee_times = unconsolidatedTimes.OrderBy(t => t.EmployeId).ThenBy(e => e.Date).ToList();

                DateTime employeDate = new DateTime();
                TimeSpan difference;
                string entryRowKey = "";
                double totalMinutes = 0;
                int id = -1;
                string RowKeyLastEmployee = employee_times.Last().RowKey;
                bool finish = false;

                foreach (TimeEntity em in employee_times)
                {
                    //If the employee has times on different dates, separate consolidation is created
                    if (!employeDate.ToString("dd-MM-yyyy").Equals(em.Date.ToString("dd-MM-yyyy")) && id == em.EmployeId)
                    {
                        CreateOrUpdateConsolidation(id, totalMinutes, consolidatedTable, employeDate);
                        totalMinutes = 0;
                    }

                    if (id == -1)
                    {
                        id = em.EmployeId;
                    }

                    //Ends the process with a time in state 0. This doesn't consolidate
                    if (RowKeyLastEmployee == em.RowKey && em.Type == 0)
                    {
                        CreateOrUpdateConsolidation(id, totalMinutes, consolidatedTable, employeDate);
                        finish = true;
                    }

                    //If it finishes going through all the data of an employee the consolidation is saved.
                    if (id != em.EmployeId && id != -1 && !finish)
                    {
                        //If the employee already has a consolidation, a new consolidation will be created
                        //if the current date is different from the date of the already registered consolidation.
                        //else the consolidation will be updated in your field minutes worked
                        CreateOrUpdateConsolidation(id, totalMinutes, consolidatedTable, employeDate);
                        id = em.EmployeId;
                        totalMinutes = 0;
                    }

                    if (em.Type == 0 && !finish)
                    {
                        employeDate = em.Date;
                        entryRowKey = em.RowKey;
                    }
                    //Consolidation will only be created if the employee has checked out
                    else if(!finish)
                    {
                        difference = em.Date - employeDate;
                        totalMinutes += difference.TotalMinutes;
                        //The IsConsolidated field is updated at the employee's entry and exit
                        UpdateIsConsolidatedState(entryRowKey, timeTable);
                        UpdateIsConsolidatedState(em.RowKey, timeTable);

                        if (RowKeyLastEmployee == em.RowKey)
                        {
                            CreateOrUpdateConsolidation(id, totalMinutes, consolidatedTable, employeDate);
                        }
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
            TableOperation findOperation = TableOperation.Retrieve<ConsolidatedEntity>("CONSOLIDATED", rowkey);
            TableResult findResult = await consolidatedTable.ExecuteAsync(findOperation);

            //Update
            ConsolidatedEntity consolidated_Entity = (ConsolidatedEntity)findResult.Result;
            consolidated_Entity.MinutesWorked += minutesWorked;

            TableOperation add_Operation = TableOperation.Replace(consolidated_Entity);
            await consolidatedTable.ExecuteAsync(add_Operation);
        }

        private static async void CreateConsolidation(int id, double totalMinutes, CloudTable consolidatedTable, DateTime employeDate)
        {
            ConsolidatedEntity consolidatedEntity = new ConsolidatedEntity
            {
                EmployeId = id,
                Date = new DateTime(employeDate.Year, employeDate.Month, employeDate.Day, 00, 00, 0),
                MinutesWorked = totalMinutes,
                ETag = "*",
                PartitionKey = "CONSOLIDATED",
                RowKey = Guid.NewGuid().ToString(),
            };

            if (consolidatedEntity.Date.Year != 1)
            {
                TableOperation addOperation = TableOperation.Insert(consolidatedEntity);
                await consolidatedTable.ExecuteAsync(addOperation);
            }
        }

        private static async void CreateOrUpdateConsolidation(int id, double totalMinutes, CloudTable consolidatedTable, DateTime employeDate)
        {
            string consolidated_filter = TableQuery.GenerateFilterConditionForInt("EmployeId", QueryComparisons.Equal, id);
            TableQuery<ConsolidatedEntity> consolidated_query = new TableQuery<ConsolidatedEntity>().Where(consolidated_filter);
            TableQuerySegment<ConsolidatedEntity> existsConsolidated = await consolidatedTable.ExecuteQuerySegmentedAsync(consolidated_query, null);

            if (existsConsolidated.Results.Count != 0)
            {
                foreach (ConsolidatedEntity it in existsConsolidated)
                {
                    if (it.Date.ToString("dd-MM-yyyy").Equals(employeDate.Date.ToString("dd-MM-yyyy")))
                    {
                        UpdateIfExistsConsolidated(it.RowKey, consolidatedTable, totalMinutes);
                    }
                    else
                    {
                        CreateConsolidation(id, totalMinutes, consolidatedTable, employeDate);
                    }
                }
            }
            else
            {
                CreateConsolidation(id, totalMinutes, consolidatedTable, employeDate);
            }
        }



    }
}
