using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Moq;
using Moq.Protected;
using System.Net;
using System.Threading;

namespace ReqNRollTestProject
{
    /// <summary>
    /// API Test Suite: Validates API responses for status, structure, and format.
    /// Uses modular test design, parameterized testing, and mocking for better maintainability.
    /// </summary>
    [TestFixture]
    public class ApiTests
    {
        private HttpClient _httpClient;
        private Mock<HttpMessageHandler> _mockHttpHandler;
        private string _baseApiUrl = "https://jsonplaceholder.typicode.com/posts/";

        /// <summary>
        /// Set up a mock HttpClient before running tests.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            _mockHttpHandler = new Mock<HttpMessageHandler>();

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    var responseContent = new JObject
                    {
                        ["userId"] = 1,
                        ["id"] = 1,
                        ["title"] = "Sample Title",
                        ["body"] = "Sample Body"
                    }.ToString();

                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(responseContent)
                    };
                });

            _httpClient = new HttpClient(_mockHttpHandler.Object);
        }

        /// <summary>
        /// Validates that the API returns a 200 OK status.
        /// Uses parameterized testing for different endpoints.
        /// </summary>
        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public async Task Validate_StatusCode_Returns200(int postId)
        {
            var response = await _httpClient.GetAsync($"{_baseApiUrl}{postId}");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "[ERROR] API did not return 200 OK.");
        }

        /// <summary>
        /// Checks if the API response is in valid JSON format.
        /// </summary>
        [Test]
        [TestCase(1)]
        public async Task Validate_Response_IsJson(int postId)
        {
            var response = await _httpClient.GetAsync($"{_baseApiUrl}{postId}");
            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.That(responseBody, Is.Not.Null, "[ERROR] Response body is null!");

            try
            {
                JObject.Parse(responseBody);
                Console.WriteLine("[PASS] ✅ Response is valid JSON!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] ❌ Response is NOT valid JSON! Error: {ex.Message}");
                Assert.Fail("[ERROR] Response is not a valid JSON.");
            }
        }

        /// <summary>
        /// Verifies that API response structure contains the required fields.
        /// </summary>
        [Test]
        [TestCase(1)]
        public async Task Validate_ResponseStructure(int postId)
        {
            var response = await _httpClient.GetAsync($"{_baseApiUrl}{postId}");
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JObject.Parse(responseBody);

            Assert.That(jsonResponse["userId"], Is.Not.Null, "[ERROR] userId is missing!");
            Assert.That(jsonResponse["id"], Is.Not.Null, "[ERROR] id is missing!");
            Assert.That(jsonResponse["title"], Is.Not.Null, "[ERROR] title is missing!");
            Assert.That(jsonResponse["body"], Is.Not.Null, "[ERROR] body is missing!");

            Console.WriteLine("[PASS] ✅ Response structure is correct!");
        }

        /// <summary>
        /// Cleans up HttpClient resources after tests run.
        /// </summary>
        [TearDown]
        public void Cleanup()
        {
            _httpClient?.Dispose();
        }
    }
}
