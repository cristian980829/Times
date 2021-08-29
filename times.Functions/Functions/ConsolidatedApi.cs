using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using times.Common.Responses;
using times.Functions.Entities;

namespace times.Functions.Functions
{
    public static class ConsolidatedApi
    {

        [FunctionName(nameof(getAllConsolidatedByDate))]
        public static async Task<IActionResult> getAllConsolidatedByDate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "consolidated/{date}")] HttpRequest req,
            [Table("consolidated", "CONSOLIDATED", Connection = "AzureWebJobsStorage")] CloudTable consolidatedTable,
            DateTime date,
            ILogger log)
        {
            log.LogInformation($"Get consolidated by date: {date} received.");
            TableQuery<TimeEntity> query = new TableQuery<TimeEntity>();
            TableQuerySegment<TimeEntity> consolidated = await consolidatedTable.ExecuteQuerySegmentedAsync(query, null);
            List<TimeEntity> consolidatedList = new List<TimeEntity>();
            foreach (TimeEntity item in consolidated)
            {
                if (item.Date.Year == date.Year &&
                    item.Date.Month == date.Month &&
                    item.Date.Day == date.Day)
                {
                    consolidatedList.Add(item);
                }
            }

            string message = $"Consolidated: {date}, received.";

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = consolidatedList
            });
        }
    }
}
