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
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            ILogger log)
        {
            log.LogInformation("Recieved a new time.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            Time time = JsonConvert.DeserializeObject<Time>(requestBody);

            if (time?.EmployeId == null || time?.Date == null || time?.Type == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "The request must have all the data."
                });
            }

            string replyMessage = null;

            Task<string> validate_Entry = validateEntry(timeTable, (int)time.EmployeId, time);

            if (validate_Entry.Result != null)
            {
                replyMessage = validate_Entry.Result;
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
                RowKey = Guid.NewGuid().ToString()
            };

            TableOperation addOperation = TableOperation.Insert(timeEntity);
            await timeTable.ExecuteAsync(addOperation);

            string message = "New time stored in table";
            log.LogInformation(message);
            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = timeEntity
            });
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

            string message = $"Time: {timeEntity.RowKey}, received.";
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
            [Table("time", "TIME", "{id}", Connection = "AzureWebJobsStorage")] TimeEntity time,
            [Table("time", Connection = "AzureWebJobsStorage")] CloudTable timeTable,
            string id,
            ILogger log)
        {
            log.LogInformation($"Delete time: {id} received.");

            if (time == null)
            {
                return new BadRequestObjectResult(new Response
                {
                    IsSuccess = false,
                    Message = "Time not found."
                });
            }

            string filter = TableQuery.GenerateFilterConditionForInt("EmployeId", QueryComparisons.Equal, time.EmployeId);
            TableQuery<TimeEntity> query = new TableQuery<TimeEntity>().Where(filter);
            TableQuerySegment<TimeEntity> existsId = await timeTable.ExecuteQuerySegmentedAsync(query, null, null, null);
            int size = existsId.Results.Count;
            string deletedMessage = null;
            if (size > 0)
            {
                deletedMessage = validateIfItCanBeDelete(existsId, size, time, timeTable).Result;
            }

            await timeTable.ExecuteAsync(TableOperation.Delete(time));
            string message = null;
            if (deletedMessage != null)
            {
                message = deletedMessage;
            }
            else
            {
                message = $"Time: {time.RowKey} deleted.";
            }

            log.LogInformation(message);

            return new OkObjectResult(new Response
            {
                IsSuccess = true,
                Message = message,
                Result = time
            });
        }

        private static async Task<string> validateEntry(CloudTable timeTable, int EmployeId, Time time)
        {
            string filter = TableQuery.GenerateFilterConditionForInt("EmployeId", QueryComparisons.Equal, EmployeId);
            TableQuery<TimeEntity> query = new TableQuery<TimeEntity>().Where(filter);
            TableQuerySegment<TimeEntity> existsId = await timeTable.ExecuteQuerySegmentedAsync(query, null, null, null);

            if (existsId.Results.Count > 0)
            {
                List<TimeEntity> timesList = existsId.OrderBy(x => x.Date).ToList();

                if (timesList.Last().Type == time.Type)
                {
                    string type = time.Type is 0 ? "entered" : "left";
                    return $"this employee has not {type}.";
                }
            }
            else
            {
                if (time.Type == 1)
                {
                    return $"this employee has not entered.";
                }
            }
            return null;
        }

        private static async Task<string> validateIfItCanBeDelete(TableQuerySegment<TimeEntity> existsId, int size, TimeEntity time, CloudTable timeTable)
        {
            string deleted = null;
            List<TimeEntity> timesList = existsId.OrderBy(x => x.Date).ToList();
            for (int i = 0; i < size; i++)
            {
                if (timesList[i].RowKey == time.RowKey)
                {
                    if (timesList[i].Type == 1)
                    {
                        if (timesList.Last().RowKey != timesList[i].RowKey)
                        {
                            await timeTable.ExecuteAsync(TableOperation.Delete(timesList[i - 1]));
                            return $"Time: {time.RowKey} and {timesList[i - 1].RowKey} deleted.";
                        }
                    }
                    else
                    {
                        if (timesList.Last().RowKey != timesList[i].RowKey)
                        {
                            await timeTable.ExecuteAsync(TableOperation.Delete(timesList[i + 1]));
                            return $"Time: {time.RowKey} and {timesList[i + 1].RowKey} deleted";
                        }
                    }
                }
            }
            return deleted;
        }
    }
}
