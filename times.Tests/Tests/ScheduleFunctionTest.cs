using Microsoft.AspNetCore.Mvc;
using System;
using times.Functions.Functions;
using times.Tests.Helpers;
using Xunit;

namespace times.Tests.Tests
{
    public class ScheduledFunctionTest
    {

        [Fact]
        public void ScheduledFunction_Should_Log_Message()
        {
            //Arrenge --preparate unitary test --We need request, table and http
            MockCloudTableTime mockTimes = new MockCloudTableTime(new Uri("http://127.0.0.1:10002/devstoreaccount1/reports"));
            ListLogger logger = (ListLogger)TestFactory.CreateLogger(LoggerTypes.List);

            //Act --Execute unitary test
            ScheduledFunction.Run(null, mockTimes, mockConsolidated, logger);
            string message = logger.Logs[0];

            //Assert --verification if the unitary test is correct
            Assert.Contains("Consolidated completed", message);

        }
    }
}
