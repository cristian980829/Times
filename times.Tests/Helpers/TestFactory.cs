using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using times.Common.Models;
using times.Functions.Entities;

namespace times.Tests.Helpers
{
    public class TestFactory
    {
        public static TimeEntity GetTimeEntity()
        {
            return new TimeEntity
            {
                ETag = "*",
                PartitionKey = "TIME",
                RowKey = Guid.NewGuid().ToString(),
                Date = DateTime.UtcNow,
                IsConsolidated = false,
                EmployeId = 133,
                Type = 0
            };
        }

        public static List<TimeEntity> GetAllTimeEntity()
        {
            List<TimeEntity> lista = new List<TimeEntity>();
            TimeEntity time = new TimeEntity
            {
                ETag = "*",
                PartitionKey = "TIME",
                RowKey = Guid.NewGuid().ToString(),
                Date = DateTime.UtcNow,
                IsConsolidated = false,
                EmployeId = 2008,
                Type = 0
            };

            lista.Add(time);
            return lista;
        }

        public static List<TimeEntity> GetTimeEntityList()
        {
            List<TimeEntity> lista = new List<TimeEntity>();
            TimeEntity time = new TimeEntity
            {
                ETag = "*",
                PartitionKey = "TIME",
                RowKey = Guid.NewGuid().ToString(),
                Date = DateTime.UtcNow,
                IsConsolidated = false,
                EmployeId = 2008,
                Type = 0
            };

            if (time.EmployeId != GetTimeRequest().EmployeId)
            {
                return lista;
            }

            lista.Add(time);
            return lista;
        }

        public static List<TimeEntity> GetConsolidatedList()
        {
            List<TimeEntity> lista = new List<TimeEntity>();
            TimeEntity time = new TimeEntity
            {
                ETag = "*",
                PartitionKey = "CONSOLIDATED",
                RowKey = Guid.NewGuid().ToString(),
                Date = DateTime.UtcNow,
                EmployeId = 1,
                MinutesWorked = 1808
            };
            lista.Add(time);
            return lista;
        }

        public static DefaultHttpRequest CreateHttpRequest(Guid timeId, Time timeRequest)
        {
            string request = JsonConvert.SerializeObject(timeRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request),
                Path = $"/{timeId}"
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Guid timeId)
        {
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Path = $"/{timeId}"
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(DateTime consolidatedDate)
        {
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Path = $"/{consolidatedDate}"
            };
        }

        public static DefaultHttpRequest CreateHttpRequest(Time timeRequest)
        {
            string request = JsonConvert.SerializeObject(timeRequest);
            return new DefaultHttpRequest(new DefaultHttpContext())
            {
                Body = GenerateStreamFromString(request)
            };
        }

        public static DefaultHttpRequest CreateHttpRequest()
        {
            return new DefaultHttpRequest(new DefaultHttpContext());
        }

        public static Time GetTimeRequest()
        {
            return new Time
            {
                Date = DateTime.UtcNow,
                IsConsolidated = false,
                EmployeId = 134,
                Type = 0,
            };
        }

        public static Stream GenerateStreamFromString(string stringToConvert)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(stringToConvert);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static ILogger CreateLogger(LoggerTypes type = LoggerTypes.Null)
        {
            ILogger logger;
            if (type == LoggerTypes.List)
            {
                logger = new ListLogger();
            }
            else
            {
                logger = NullLoggerFactory.Instance.CreateLogger("Null Logger");
            }
            return logger;
        }
    }
}
