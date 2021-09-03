using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using times.Common.Models;
using times.Functions.Entities;
using times.Functions.Functions;
using times.Tests.Helpers;
using Xunit;

namespace times.Tests.Tests
{
    public class TimeApiTest
    {
        private readonly ILogger logger = TestFactory.CreateLogger();
        private readonly string Uri = "http://127.0.0.1:10002/devstoreaccount1/reports";

        [Fact]
        public async void CreateTime_Should_Return_200()
        {
            //Arrenge --preparate unitary test --We need request, table and http
            MockCloudTableTime mockTimes = new MockCloudTableTime(new Uri(Uri));
            Time timeRequest = TestFactory.GetTimeRequest();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(timeRequest);

            //Act --Execute unitary test
            IActionResult response = await TimeApi.CreateTime(request, mockTimes, logger);

            //Assert --verification if the unitary test is correct
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async void UpdateTime_Should_Return_200()
        {
            //Arrenge --preparate unitary test --We need request, table and http
            MockCloudTableTime mockTimes = new MockCloudTableTime(new Uri(Uri));
            Time timeRequest = TestFactory.GetTimeRequest();
            Guid timeId = Guid.NewGuid();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(timeId, timeRequest);

            //Act --Execute unitary test
            IActionResult response = await TimeApi.UpdateTime(request, mockTimes, timeId.ToString(), logger);

            //Assert --verification if the unitary test is correct
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async void GetAllTimes_Should_Return_200()
        {
            //Arrenge --preparate unitary test --We need request, table and http
            MockCloudTableTime mockTimes = new MockCloudTableTime(new Uri(Uri));
            DefaultHttpRequest request = TestFactory.CreateHttpRequest();

            //Act --Execute unitary test
            IActionResult response = await TimeApi.GetAllTimes(request, mockTimes, logger);

            //Assert --verification if the unitary test is correct
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async void GetAllConsolidatedByDate_Should_Return_200()
        {
            //Arrenge --preparate unitary test --We need request, table and http
            MockCloudTableConsolidated mockConsolidated = new MockCloudTableConsolidated(new Uri(Uri));
            DateTime consolidatedDate = DateTime.ParseExact(DateTime.UtcNow.ToString("yyyy-MM-dd"), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(consolidatedDate);

            //Act --Execute unitary test
            IActionResult response = await ConsolidatedApi.getAllConsolidatedByDate(request, mockConsolidated, consolidatedDate, logger);

            //Assert --verification if the unitary test is correct
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public void GetTimeById_Should_Return_200()
        {
            //Arrenge --preparate unitary test --We need request, table and http
            Guid timeId = Guid.NewGuid();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(timeId);
            TimeEntity timeEntity = TestFactory.GetTimeEntity();

            //Act --Execute unitary test
            IActionResult response = TimeApi.GetTimeById(request, timeEntity, timeId.ToString(), logger);

            //Assert --verification if the unitary test is correct
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        [Fact]
        public async void DeleteTime_Should_Return_200()
        {
            //Arrenge --preparate unitary test --We need request, table and http
            MockCloudTableTime mockTimes = new MockCloudTableTime(new Uri(Uri));
            Guid timeId = Guid.NewGuid();
            DefaultHttpRequest request = TestFactory.CreateHttpRequest(timeId);
            TimeEntity timeEntity = TestFactory.GetTimeEntity();

            //Act --Execute unitary test
            IActionResult response = await TimeApi.DeleteTime(request, timeEntity, mockTimes, timeId.ToString(), logger);

            //Assert --verification if the unitary test is correct
            OkObjectResult result = (OkObjectResult)response;
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }
    }

}
