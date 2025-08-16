using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;
using System.Net;
using System.Text.Json;


namespace StorySpoiler
{
    [TestFixture]
    public class StoryTests
    {
        private RestClient client;
        private static string lastCreatedFoodId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("userPlams33", "123456789");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string userName, string password)
        {
            var logClient = new RestClient(baseUrl);

            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { userName, password });

            var response = logClient.Execute(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to retrieve JWT token. Status: {response.StatusCode}, Content: {response.Content}");
            }

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString();
        }

        //tests

        [Test, Order(1)]

        public void CreateNewStory_ShouldReturnCreated()
        {
            var newStory = new
            {
                Title = "New story",
                Description = "New story description",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(newStory);

            var response = client.Execute(request);

            //Assert that the response status code is Created (201).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            //Assert that the StoryId is returned in the response
            Assert.That(response.Content, Does.Contain("storyId"));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            //Assert that the response message indicates the story was "Successfully created!".
             Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Successfully created!"));

            //Store the StoryId as a static member of the static member of the test class to maintain its value between test runs
            lastCreatedFoodId = json.GetProperty("storyId").GetString();
        }

        [Test, Order(2)]

        public void EditLastCreatedStory_ShouldReturnOK()
        {
            var editRequest = new StoryDTO
            {
                Title = "Edited the last created story",
                Description = "Edited story description",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{lastCreatedFoodId}", Method.Put);
            request.AddJsonBody(editRequest);

            var response = client.Execute(request);
            //Assert that the response status code is OK (200).
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            //Assert that the response message indicates the story was "Successfully edited".
            var editedResponce = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(editedResponce.GetProperty("msg").GetString(), Is.EqualTo("Successfully edited"));

        }

        [Test, Order(3)]

        public void GetAllStories_ShouldReturnList()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            //Assert that the response contains a non-empty array.
            var responceItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);
            Assert.That(responceItems, Is.Not.Null);
            Assert.That(responceItems, Is.Not.Empty);
            Assert.That(responceItems, Is.InstanceOf<List<ApiResponseDTO>>());
           
        }

        [Test, Order(4)]

        public void DeleteStoryByID_ShouldReturnOK()
        {
            var request = new RestRequest($"/api/Story/Delete/{lastCreatedFoodId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Deleted successfully!"));
        }

        [Test, Order(5)]

        public void TryCreateStory_WithoutRequiredField_ShouldReturnBadRequest()
        {
            var newWrondStorySpoiler = new
            {
                Title = "",
                Description = "Testing bad request",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(newWrondStorySpoiler);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Test, Order(6)]

        public void TryEditNonExistingStoryByID_ShouldReturnNotFound()
        {
          string nonExistingStoryId = "11111111"; 

            var editRequest = new StoryDTO
            {
                Title = "Non existing story",
                Description = "Edited non exising description",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{nonExistingStoryId}", Method.Put);
            request.AddJsonBody(editRequest);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("No spoilers..."));
        }

        [Test, Order(7)]

        public void TryDeleteSpoilerBy_NonExistingID_ShouldReturnBHadRequest()
        {
            string nonExistingStoryId = "2222222";

            var request = new RestRequest($"/api/Story/Delete/{nonExistingStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Unable to delete this story spoiler!"));
        }

        [OneTimeTearDown]
        public void CleanUp()
        { 
            client?.Dispose();
        }
    }
}