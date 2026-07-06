using DriverTripSchedulerBackend.DTO.Trips;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DriverTripScheduler.Tests.Functional_Testing.UserFunctionalTest
{
    [TestClass]
    public class TripFunctionalTests
    {
        private static readonly HttpClient client = new HttpClient();
        private string baseUrl = "http://localhost:5038";
        private string managerToken;

        [TestInitialize]
        public async Task Setup()
        {
            // Register and login as Manager
            var uniqueUsername = $"manager_{Guid.NewGuid().ToString("N").Substring(0, 6)}";

            var registerPayload = new
            {
                Username = uniqueUsername,
                Password = "admin123",
                Role = "Manager"
            };

            var registerContent = new StringContent(JsonConvert.SerializeObject(registerPayload), Encoding.UTF8, "application/json");
            var registerResponse = await client.PostAsync($"{baseUrl}/api/Auth/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();

            var loginPayload = new
            {
                Username = uniqueUsername,
                Password = "admin123"
            };

            var loginContent = new StringContent(JsonConvert.SerializeObject(loginPayload), Encoding.UTF8, "application/json");
            var loginResponse = await client.PostAsync($"{baseUrl}/api/Auth/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
            var loginData = JsonConvert.DeserializeObject<LoginResponse>(loginResponseString);
            managerToken = loginData.Token;
        }

        [TestMethod]
        public async Task AddTrip_ShouldSucceed_WhenDataIsValid()

        {
            // Dynamically generate trip times
            DateTime startTime = DateTime.UtcNow.AddHours(10); // UTC time + 10 hours
            DateTime endTime = startTime.AddHours(1);         // Start + 1 hour

            string STime = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
            string ETime = endTime.ToString("yyyy-MM-ddTHH:mm:ss");
            // Arrange
            var tripPayload = new
            {
                originCityId = 2,
                originAreaId = 7,
                destinationCityId = 2,
                destinationAreaId = 8,
                driverId = 8,
                vehicleId = 10,
                tripStartTime = STime,
                tripEndTime = ETime
            };

            var tripContent = new StringContent(JsonConvert.SerializeObject(tripPayload), Encoding.UTF8, "application/json");
            tripContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

            // Act
            var response = await client.PostAsync($"{baseUrl}/api/Trip", tripContent);
            var responseString = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, $"Failed: {responseString}");
            Assert.IsTrue(responseString.Contains("\"tripId\":"), "Response should contain tripId");

            var trip = JsonConvert.DeserializeObject<TripResponseDTO>(responseString);
            int tripId = trip.TripId;

            // Delete the created trip to clean up
            var deleteResponse = await client.DeleteAsync($"{baseUrl}/api/Trip/{tripId}");
            deleteResponse.EnsureSuccessStatusCode();

        }

        [TestMethod]
        public async Task AddTrip_ShouldFail_WhenTripOverlapsWithExisting()
        {
            // Arrange
            var url = $"{baseUrl}/api/Trip";
            DateTime startTime = DateTime.UtcNow.AddHours(10); // UTC time + 10 hours
            DateTime endTime = startTime.AddHours(1);         // Start + 1 hour
            DateTime overlapTime= DateTime.UtcNow.AddHours(9);

            string STime = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
            string ETime = endTime.ToString("yyyy-MM-ddTHH:mm:ss");

            // Step 1: Create initial valid trip
            var baseTrip = new
            {
                originCityId = 1,
                originAreaId = 3,
                destinationCityId = 2,
                destinationAreaId = 8,
                driverId = 11,
                vehicleId = 9,
                tripStartTime = STime,
                tripEndTime = ETime
            };

            var baseContent = new StringContent(JsonConvert.SerializeObject(baseTrip), Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

            var baseResponse = await client.PostAsync(url, baseContent);
            var baseResponseString = await baseResponse.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.Created, baseResponse.StatusCode, $"Base trip creation failed: {baseResponseString}");

            // Extract created tripId from response
            var createdTrip = JsonConvert.DeserializeObject<TripResponseDTO>(baseResponseString);
            int createdTripId = createdTrip.TripId;

            try
            {
                // Step 2: Attempt to create overlapping trip
                var overlappingTrip = new
                {
                    originCityId = 1,
                    originAreaId = 3,
                    destinationCityId = 2,
                    destinationAreaId = 8,
                    driverId = 11,
                    vehicleId = 9,
                    tripStartTime = overlapTime,// Overlaps with previous
                    tripEndTime = ETime
                };

                var overlapContent = new StringContent(JsonConvert.SerializeObject(overlappingTrip), Encoding.UTF8, "application/json");
                var overlapResponse = await client.PostAsync(url, overlapContent);
                var overlapResponseString = await overlapResponse.Content.ReadAsStringAsync();

                // Step 3: Assert overlapping failure
                Assert.AreEqual(HttpStatusCode.Conflict, overlapResponse.StatusCode, $"Expected Conflict, got: {overlapResponse.StatusCode}. Response: {overlapResponseString}");

                string[] expectedMessages = {
                    "Driver has an active or upcoming trip.",
                    "Trip time overlaps with another trip for the driver.",
                    "Trip time overlaps with another trip for the vehicle."
        };

                Assert.IsTrue(expectedMessages.Any(msg => overlapResponseString.Contains(msg)), $"Expected overlap message but got: {overlapResponseString}");
            }
            finally
            {
                // Step 4: Cleanup - delete the created base trip
                var deleteResponse = await client.DeleteAsync($"{url}/{createdTripId}");
                Assert.IsTrue(deleteResponse.IsSuccessStatusCode, "Cleanup failed: Could not delete created trip.");
            }
        }


        [TestMethod]
        public async Task UpdateTrip_ShouldSucceed_WhenDataIsValid()
        {
            // Dynamically generate trip times
            DateTime startTime = DateTime.UtcNow.AddHours(10); // UTC time + 10 hours
            DateTime endTime = startTime.AddHours(1);         // Start + 1 hour

            string STime = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
            string ETime = endTime.ToString("yyyy-MM-ddTHH:mm:ss");

            // STEP 1: Create a trip first
            var createPayload = new
            {
                originCityId = 2,
                originAreaId = 6,
                destinationCityId = 2,
                destinationAreaId = 7,
                driverId = 10,
                vehicleId = 8,
                tripStartTime = STime,
                tripEndTime = ETime
            };

            var createContent = new StringContent(JsonConvert.SerializeObject(createPayload), Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
            var createResponse = await client.PostAsync($"{baseUrl}/api/Trip", createContent);
            var createResponseString = await createResponse.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode, $"Create failed: {createResponseString}");

            dynamic createdTrip = JsonConvert.DeserializeObject(createResponseString);
            int CtripId = createdTrip.tripId;

            // STEP 2: Now update that trip
            var updatePayload = new
            {
                tripId = CtripId,
                originCityId = 2,
                originAreaId = 6,
                destinationCityId = 2,
                destinationAreaId = 8, //  Change destination area
                driverId = 10,
                vehicleId = 8,
                tripStartTime = STime,
                tripEndTime = ETime
            };

            var updateContent = new StringContent(JsonConvert.SerializeObject(updatePayload), Encoding.UTF8, "application/json");
            var updateResponse = await client.PutAsync($"{baseUrl}/api/Trip/{CtripId}", updateContent);
            var updateResponseString = await updateResponse.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode, $"Update failed: {updateResponseString}");
            Assert.IsTrue(updateResponseString.Contains("\"destinationAreaId\":8"), "Destination area should be updated to 8");

            // STEP 3 (Optional): Delete trip after test
            var deleteResponse = await client.DeleteAsync($"{baseUrl}/api/Trip/{CtripId}");
            Assert.AreEqual(HttpStatusCode.OK, deleteResponse.StatusCode, "Cleanup failed: Trip not deleted");
        }

        [TestMethod]
        public async Task DeleteTrip_ShouldSucceed_WhenTripExists()
        {
            // Set Authorization Header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
            // Dynamically generate trip times
            DateTime startTime = DateTime.UtcNow.AddHours(10); // UTC time + 10 hours
            DateTime endTime = startTime.AddHours(1);         // Start + 1 hour

            string STime = startTime.ToString("yyyy-MM-ddTHH:mm:ss");
            string ETime = endTime.ToString("yyyy-MM-ddTHH:mm:ss");

            // Step 1: Create a trip to be deleted
            var tripPayload = new
            {
                originCityId = 2,
                originAreaId = 7,
                destinationCityId = 2,
                destinationAreaId = 8,
                driverId = 8,
                vehicleId = 5,
                tripStartTime =STime,
                tripEndTime = ETime
            };

            var content = new StringContent(JsonConvert.SerializeObject(tripPayload), Encoding.UTF8, "application/json");
            var createResponse = await client.PostAsync($"{baseUrl}/api/Trip", content);
            var createResponseString = await createResponse.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode, $"Create failed: {createResponseString}");

            // Extract tripId from response
            var tripObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(createResponseString);
            Assert.IsTrue(tripObject.ContainsKey("tripId"), "Response did not contain tripId");

            int tripId = Convert.ToInt32(tripObject["tripId"]);

            // Step 2: Delete the created trip
            var deleteResponse = await client.DeleteAsync($"{baseUrl}/api/Trip/{tripId}");
            var deleteResponseString = await deleteResponse.Content.ReadAsStringAsync();

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, deleteResponse.StatusCode, $"Delete failed: {deleteResponseString}");
            Assert.IsTrue(deleteResponseString.Contains("Trip deleted successfully"), "Unexpected delete response message");
        }



        private class LoginResponse
        {
            public string Username { get; set; }
            public string Role { get; set; }
            public string Token { get; set; }
        }
    }
}
