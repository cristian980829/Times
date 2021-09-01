using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using times.Common.Models;
using times.Common.Responses;
using times.Functions.Entities;

namespace times.Functions.Functions
{
    public static class TimeApi
    {
        [FunctionName(nameof(CreateTime))]
        public static async Task<IActionResult> CreateTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "time")] HttpRequest req,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            Time time = JsonConvert.DeserializeObject<Time>(requestBody);

            if (time?.EmployeId == null ||
                time?.Date == null ||
                time?.Type == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have all the data."
                });
            }

            string filter = TableQuery.GenerateFilterConditionForInt("EmployeId", QueryComparisons.Equal, (int)time.EmployeId);
            TableQuery<TimeEntity> query = new TableQuery<TimeEntity>().Where(filter);
            TableQuerySegment<TimeEntity> existsId = await timeTable.ExecuteQuerySegmentedAsync(query, null);
            
            string replyMessage = null;

            if (existsId.Results.Count != 0)
            {
                List<TimeEntity> timesList = existsId.OrderBy(x => x.Date).ToList();

                if (timesList.Last().Type == time.Type)
                {
                    replyMessage = "This employee has alreay ";
                    replyMessage += time.Type is 0 ? "entered" : "left";
                }
            }
            else
            {
                if (time.Type == 1)
                {
                    replyMessage = $"this employee has not entered.";
                }
            }

            if (replyMessage != null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = replyMessage
                });
            }

            TimeEntity timeEntity = new TimeEntity
            {
                EmployeId = (int)time.EmployeId,
                Date = (DateTime)time.Date,
                Type = time.Type,
                IsConsolidated = false,
                ETag = "*",
                PartitionKey = "TIME",
                RowKey = Guid.NewGuid().ToString(),
            };

            TableOperation addOperation = TableOperation.Insert(timeEntity);
            await timeTable.ExecuteAsync(addOperation);

            string message = "New time stored in table";
            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeEntity
            });
        }

        private static void List<T>()
        {
            throw new NotImplementedException();
        }

        [FunctionName(nameof(UpdateTime))]
        public static async Task<IActionResult> UpdateTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "time/{id}")] HttpRequest req,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            string id,
            ILogger log)
        {
            log.LogInformation($"Update for time: {id}. received.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            Time time = JsonConvert.DeserializeObject<Time>(requestBody);

            if (time?.EmployeId == null ||
                time?.Date == null ||
                time?.Type == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have all the data."
                });
            }

            // Validate time id
            TableOperation findOperation = TableOperation.Retrieve<TimeEntity>("TIME", id);
            TableResult findResult = await timeTable.ExecuteAsync(findOperation);

            if (findResult.Result == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Time not found."
                });
            }

            //Update time
            TimeEntity timeEntity = (TimeEntity)findResult.Result;
            timeEntity.Date = (DateTime)time.Date;

            TableOperation addOperation = TableOperation.Replace(timeEntity);
            await timeTable.ExecuteAsync(addOperation);

            string message = $"Time: {id}, updated in table. ";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeEntity
            });
        }

        [FunctionName(nameof(GetAllTimes))]
        public static async Task<IActionResult> GetAllTimes(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "time")] HttpRequest req,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            ILogger log)
        {
            log.LogInformation("Get all times received.");

            TableQuery<TimeEntity> query = new TableQuery<TimeEntity>();
            TableQuerySegment<TimeEntity> times = await timeTable.ExecuteQuerySegmentedAsync(query, null);

            if (times.Results.Count == 0)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Without times."
                });
            }

            string message = "Retrieved all times.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = times
            });
        }

        [FunctionName(nameof(GetTimeById))]
        public static IActionResult GetTimeById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "time/{id}")] HttpRequest req,
            [Table("time", "TIME", "{id}", Connection = "AzureWebJobsStorage")] TimeEntity timeEntity,
            string id,
            ILogger log)
        {
            log.LogInformation($"Get time by id: {id} received.");

            if (timeEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Time not found."
                });
            }

            string message = $"Todo: {timeEntity.RowKey}, received.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeEntity
            });
        }

        [FunctionName(nameof(DeleteTime))]
        public static async Task<IActionResult> DeleteTime(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "time/{id}")] HttpRequest req,
            [Table("time", "TIME", "{id}", Connection = "AzureWebJobsStorage")] TimeEntity timeEntity,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            string id,
            ILogger log)
        {
            log.LogInformation($"Delete time: {id} received.");

            if (timeEntity == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Time not found."
                });
            }

            await timeTable.ExecuteAsync(TableOperation.Delete(timeEntity));
            string message = $"Time: {timeEntity.RowKey}, deleted.";
            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeEntity
            });
        }
    }
}
