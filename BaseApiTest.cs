using System;
using System.Collections.Generic;
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
    public class BaseApiTest
    {
        protected HttpClient _httpClient;
        protected Mock<HttpMessageHandler> _mockHttpHandler;
        protected string _baseApiUrl = "https://jsonplaceholder.typicode.com/";
        private Dictionary<string, JObject> _mockPostStorage = new Dictionary<string, JObject>(); //  Stores created posts

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
                .Returns(async (HttpRequestMessage request, CancellationToken token) =>
                {
                    string endpoint = request.RequestUri?.AbsolutePath ?? "";
                    string responseContent = "{}"; // Default empty response
                    HttpStatusCode statusCode = HttpStatusCode.OK;

                    //  Handle POST requests (Creating a resource)
                    if (request.Method == HttpMethod.Post && endpoint.Contains("posts"))
                    {
                        statusCode = HttpStatusCode.Created;
                        string newPostId = (_mockPostStorage.Count + 101).ToString(); // Generate unique ID
                        JObject newPost = new JObject
                        {
                            ["userId"] = 1,
                            ["id"] = newPostId,
                            ["title"] = "New Post Title",
                            ["body"] = "This is a new post body"
                        };

                        _mockPostStorage[newPostId] = newPost; //  Store the post
                        responseContent = newPost.ToString();
                    }
                    
                    //  Handle GET requests (Fetching real data for IDs 1-100)
                    else if (request.Method == HttpMethod.Get && endpoint.Contains("posts/"))
                    {
                        string postId = endpoint.Split('/')[2]; // Extract post ID

                        if (int.TryParse(postId, out int id) && id >= 1 && id <= 100)
                        {
                            using (var realHttpClient = new HttpClient())
                            {
                                var realResponse = await realHttpClient.GetAsync($"{_baseApiUrl}posts/{postId}");
                                statusCode = realResponse.StatusCode;
                                responseContent = await realResponse.Content.ReadAsStringAsync();
                            }
                        }
                        else if (_mockPostStorage.ContainsKey(postId))
                        {
                            responseContent = _mockPostStorage[postId].ToString();
                        }
                        else
                        {
                            statusCode = HttpStatusCode.NotFound;
                            responseContent = "{ \"error\": \"Post not found\" }";
                        }
                    }
                    //  Handle PUT requests (Updating a resource)
                        else if (request.Method == HttpMethod.Put && endpoint.Contains("posts/"))
                        {
                            string postId = endpoint.Split('/')[2];

                            if (_mockPostStorage.ContainsKey(postId))
                            {
                                //  Fetch existing post and apply updates
                                JObject updatedPost = new JObject
                                {
                                    ["userId"] = _mockPostStorage[postId]["userId"], // Keep original userId
                                    ["id"] = postId,
                                    ["title"] = request.Content != null ? JObject.Parse(await request.Content.ReadAsStringAsync())["title"]?.ToString() ?? "Updated Title" : "Updated Title",
                                    ["body"] = request.Content != null ? JObject.Parse(await request.Content.ReadAsStringAsync())["body"]?.ToString() ?? "Updated Body" : "Updated Body"
                                };

                                _mockPostStorage[postId] = updatedPost; //  Update post in storage
                                responseContent = updatedPost.ToString();
                            }
                            else
                            {
                                statusCode = HttpStatusCode.NotFound;
                                responseContent = "{ \"error\": \"Post not found\" }";
                            }
                        }

                    //  Handle DELETE requests (Deleting a resource)
                    else if (request.Method == HttpMethod.Delete && endpoint.Contains("posts/"))
                    {
                        string postId = endpoint.Split('/')[2];
                        if (_mockPostStorage.ContainsKey(postId))
                        {
                            _mockPostStorage.Remove(postId); //  Remove post from storage
                            statusCode = HttpStatusCode.OK;
                            responseContent = "{ \"message\": \"Post deleted successfully.\" }";
                        }
                        else
                        {
                            statusCode = HttpStatusCode.NotFound;
                            responseContent = "{ \"error\": \"Post not found\" }";
                        }
                    }

                    return new HttpResponseMessage
                    {
                        StatusCode = statusCode,
                        Content = new StringContent(responseContent)
                    };
                });

            _httpClient = new HttpClient(_mockHttpHandler.Object);
        }

        /// <summary>
        /// General API GET request (fetches real data)
        /// </summary>
        protected async Task<HttpResponseMessage> MakeApiCall(string endpoint)
        {
            return await _httpClient.GetAsync($"{_baseApiUrl}{endpoint}");
        }

        /// <summary>
        /// General API POST request
        /// </summary>
        protected async Task<HttpResponseMessage> CreateRequest(string endpoint, JObject postData)
        {
            var content = new StringContent(postData.ToString(), System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync($"{_baseApiUrl}{endpoint}", content);
        }

        /// <summary>
        /// General API PUT request (update)
        /// </summary>
        protected async Task<HttpResponseMessage> UpdateRequest(string endpoint, JObject updatedData)
        {
            var content = new StringContent(updatedData.ToString(), System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PutAsync($"{_baseApiUrl}{endpoint}", content);
        }

        /// <summary>
        /// General API DELETE request
        /// </summary>
        protected async Task<HttpResponseMessage> DeleteRequest(string endpoint)
        {
            return await _httpClient.DeleteAsync($"{_baseApiUrl}{endpoint}");
        }

        [TearDown]
        public void Cleanup()
        {
            _httpClient?.Dispose();
        }
    }
}
