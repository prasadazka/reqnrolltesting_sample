using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ReqNRollTestProject
{
    [TestFixture]
    internal class PostsApiTests : BaseApiTest
    {
        private string reportFilePath = "CRUD_Test_Report.txt";

        [Test]
        public async Task Full_Post_CRUD_Test_With_Report()
        {
            try
            {
                Console.WriteLine("🚀 Starting Full Post CRUD Test...");
                using (StreamWriter writer = new StreamWriter(reportFilePath, false))
                {
                    writer.WriteLine("📌 **Full Post CRUD Test Report**");
                    writer.WriteLine($"Test Run Time: {DateTime.Now}");
                    writer.WriteLine("-------------------------------------------------");

                    // 1️ Create a new post
                    var newPost = new JObject
                    {
                        ["userId"] = 1,
                        ["title"] = "Automated Test Post",
                        ["body"] = "This post is created via test automation."
                    };

                    string createUrl = $"{_baseApiUrl}posts";
                    ValidateUrl(createUrl);
                    var createResponse = await CreateRequest("posts", newPost);
                    Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created), "[ERROR] Post creation failed!");

                    var createBody = await createResponse.Content.ReadAsStringAsync();
                    JObject createdPost = ValidateJsonStructure(createBody, "Created Post");

                    string postId = createdPost["id"]?.ToString() ?? string.Empty;
                    Assert.That(!string.IsNullOrEmpty(postId), "[ERROR] Post ID is missing or empty!");

                    Console.WriteLine($"✅ [Created] Post ID: {postId} | URL: {createUrl} | Status: Success | Status Code: {(int)createResponse.StatusCode}");
                    writer.WriteLine($"📝 **Create Operation:**");
                    writer.WriteLine($"- URL: {createUrl}");
                    writer.WriteLine($"- Status: Success");
                    writer.WriteLine($"- Status Code: {(int)createResponse.StatusCode}");
                    writer.WriteLine($"- JSON Body: {createdPost}");
                    writer.WriteLine("-------------------------------------------------");

                    // 2️⃣ Read a different existing post (e.g., ID 1)
                    string readUrl = $"{_baseApiUrl}posts/1";
                    ValidateUrl(readUrl);
                    var readResponse = await MakeApiCall("posts/1");
                    Assert.That(readResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "[ERROR] Failed to retrieve existing post!");

                    var readBody = await readResponse.Content.ReadAsStringAsync();
                    JObject readPost = ValidateJsonStructure(readBody, "Read Post");

                    Console.WriteLine($"✅ [Read] Post 1 retrieved successfully! | URL: {readUrl} | Status: Success | Status Code: {(int)readResponse.StatusCode}");
                    writer.WriteLine("📖 **Read Operation:**");
                    writer.WriteLine($"- URL: {readUrl}");
                    writer.WriteLine($"- Status: Success");
                    writer.WriteLine($"- Status Code: {(int)readResponse.StatusCode}");
                    writer.WriteLine($"- JSON Body: {readPost}");
                    writer.WriteLine("-------------------------------------------------");

                    // 3️⃣ Update the created post
                    var updatedPost = new JObject
                    {
                        ["userId"] = 1,
                        ["title"] = "Updated Test Post",
                        ["body"] = "This post has been updated via test automation."
                    };

                    string updateUrl = $"{_baseApiUrl}posts/{postId}";
                    ValidateUrl(updateUrl);
                    var updateResponse = await UpdateRequest($"posts/{postId}", updatedPost);
                    Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "[ERROR] Post update failed!");

                    var updateBody = await updateResponse.Content.ReadAsStringAsync();
                    JObject updatedPostResult = ValidateJsonStructure(updateBody, "Updated Post");

                    Assert.That(updatedPostResult["title"]?.ToString(), Is.EqualTo(updatedPost["title"]?.ToString()), "[ERROR] Title update mismatch!");
                    Assert.That(updatedPostResult["body"]?.ToString(), Is.EqualTo(updatedPost["body"]?.ToString()), "[ERROR] Body update mismatch!");

                    Console.WriteLine($"✅ [Updated] Post {postId} updated successfully! | URL: {updateUrl} | Status: Success | Status Code: {(int)updateResponse.StatusCode}");
                    writer.WriteLine("✏️ **Update Operation:**");
                    writer.WriteLine($"- URL: {updateUrl}");
                    writer.WriteLine($"- Status: Success");
                    writer.WriteLine($"- Status Code: {(int)updateResponse.StatusCode}");
                    writer.WriteLine($"- JSON Body: {updatedPostResult}");
                    writer.WriteLine("-------------------------------------------------");

                    // 4️⃣ Delete the created post
                    string deleteUrl = $"{_baseApiUrl}posts/{postId}";
                    ValidateUrl(deleteUrl);
                    var deleteResponse = await DeleteRequest($"posts/{postId}");
                    Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "[ERROR] Post deletion failed!");

                    Console.WriteLine($"✅ [Deleted] Post {postId} deleted successfully! | URL: {deleteUrl} | Status: Success | Status Code: {(int)deleteResponse.StatusCode}");
                    writer.WriteLine("🗑️ **Delete Operation:**");
                    writer.WriteLine($"- URL: {deleteUrl}");
                    writer.WriteLine($"- Status: Success");
                    writer.WriteLine($"- Status Code: {(int)deleteResponse.StatusCode}");
                    writer.WriteLine($"- Deleted Post ID: {postId}");
                    writer.WriteLine("-------------------------------------------------");

                    Console.WriteLine("🎉 Full Post CRUD Test Completed Successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [ERROR] Test execution failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates JSON structure and ensures correct data types.
        /// </summary>
        private JObject ValidateJsonStructure(string responseBody, string operation)
        {
            try
            {
                //  Ensure response is not empty
                Assert.That(!string.IsNullOrEmpty(responseBody), $"[ERROR] {operation} response body is empty!");

                //  Parse JSON safely
                JObject jsonResponse = JObject.Parse(responseBody);

                //  Required keys
                string[] requiredKeys = { "userId", "id", "title", "body" };
                foreach (var key in requiredKeys)
                {
                    Assert.That(jsonResponse.ContainsKey(key), $"[ERROR] {operation} is missing '{key}'!");
                }

                //  Convert `id` to integer if it's a string and not null
                if (jsonResponse["id"] != null && jsonResponse["id"]?.Type == JTokenType.String)
                {
                    string idString = jsonResponse["id"]?.ToString() ?? "";
                    if (int.TryParse(idString, out int intId))
                    {
                        jsonResponse["id"] = intId; //  Store as integer
                    }
                    else
                    {
                        Assert.Fail($"[ERROR] {operation} 'id' is not a valid integer: '{idString}'");
                    }
                }


                //  Validate Data Types
                Assert.That(jsonResponse["userId"]?.Type == JTokenType.Integer, $"[ERROR] {operation} 'userId' is not an integer or is null!");
                Assert.That(jsonResponse["id"]?.Type == JTokenType.Integer, $"[ERROR] {operation} 'id' is not an integer after conversion or is null!");
                Assert.That(jsonResponse["title"]?.Type == JTokenType.String, $"[ERROR] {operation} 'title' is not a string or is null!");
                Assert.That(jsonResponse["body"]?.Type == JTokenType.String, $"[ERROR] {operation} 'body' is not a string or is null!");

                Console.WriteLine($"✅ [Validation] {operation} contains valid JSON structure and correct data types.");
                return jsonResponse;
            }
            catch (Exception ex)
            {
                Assert.Fail($"[ERROR] {operation} response is not valid JSON! Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates that a URL is correctly formatted.
        /// </summary>
        private void ValidateUrl(string url)
        {
            try
            {
                //  Ensure URL is not null or empty
                Assert.That(!string.IsNullOrEmpty(url), "[ERROR] The API URL is null or empty!");

                bool isValidUrl = Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) &&
                                (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                Assert.That(isValidUrl, $"[ERROR] Invalid URL: {url}");
                Console.WriteLine($"✅ [URL Validation] Valid API URL: {url}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"[ERROR] URL validation failed! Error: {ex.Message}");
                throw;
            }
        }

    }
}
