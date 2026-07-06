using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;

namespace DriverTripScheduler.Tests.FunctionalTests
{
    [TestClass]
    public class AuthTests
    {
        private static readonly HttpClient client = new HttpClient();

        [TestMethod]
        public async Task RegisterAndLogin_ShouldReturnToken_WhenCredentialsAreCorrect()
        {
            var registerUrl = "http://localhost:5038/api/Auth/register";
            var loginUrl = "http://localhost:5038/api/Auth/login";

            var uniqueUsername = $"testuser_{Guid.NewGuid().ToString("N").Substring(0, 6)}";

            // Register
            var registerPayload = new
            {
                Username = uniqueUsername,
                Password = "test1234",
                Role = "Driver"
            };

            var registerContent = new StringContent(JsonConvert.SerializeObject(registerPayload), Encoding.UTF8, "application/json");
            var registerResponse = await client.PostAsync(registerUrl, registerContent);
            var registerResponseString = await registerResponse.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode, $"Register Failed: {registerResponseString}");

            // Optional wait (only needed if DB is slow to persist)
            await Task.Delay(200);

            // Login
            var loginPayload = new
            {
                Username = uniqueUsername,
                Password = "test1234"
            };

            var loginContent = new StringContent(JsonConvert.SerializeObject(loginPayload), Encoding.UTF8, "application/json");
            var loginResponse = await client.PostAsync(loginUrl, loginContent);
            var loginResponseString = await loginResponse.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode, $"Login Failed: {loginResponseString}");

            var data = JsonConvert.DeserializeObject<LoginResponse>(loginResponseString);
            Assert.IsFalse(string.IsNullOrEmpty(data.Token), "Token was null or empty.");
        }

        private class LoginResponse
        {
            public string Username { get; set; }
            public string Role { get; set; }
            public string Token { get; set; }
        }

        [TestMethod]
        public async Task Register_ShouldFail_WithExistingUsername()
        {
            var registerUrl = "http://localhost:5038/api/Auth/register";
            var username = $"duplicate_user_{Guid.NewGuid().ToString("N").Substring(0, 6)}";

            var payload = new
            {
                Username = username,
                Password = "test1234",
                Role = "Driver"
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            // First registration should succeed
            var firstResponse = await client.PostAsync(registerUrl, content);
            Assert.AreEqual(HttpStatusCode.OK, firstResponse.StatusCode);

            // Second registration with same username should fail
            var secondResponse = await client.PostAsync(registerUrl, content);
            Assert.IsTrue(
                secondResponse.StatusCode == HttpStatusCode.BadRequest || secondResponse.StatusCode == HttpStatusCode.Conflict,
                $"Expected failure status code, got: {secondResponse.StatusCode}"
            );
        }
        [TestMethod]
        public async Task Login_ShouldFail_WithInvalidCredentials()
        {
            var loginUrl = "http://localhost:5038/api/Auth/login";

            var payload = new
            {
                Username = "nonexistentuser",
                Password = "wrongpassword"
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(loginUrl, content);
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Login with invalid credentials should fail.");
        }


        // in this 2 methods concuurency problem comming means at a time
        // registration success and login fails (when new data)           at same time of registraion login fails because db is not updated at that current time
        // registratoin fails  and login success (when existing data passed)                


        //[TestMethod]
        //public async Task Register_ShouldSucceed_WithNewUser()
        //{
        //    var url = "http://localhost:5038/api/Auth/register"; // Adjust if your controller route differs

        //    var payload = new
        //    {
        //        Username = "testuser1234",
        //        Password = "test1234",
        //        Role = "Driver"
        //    };

        //    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        //    var response = await client.PostAsync(url, content);
        //    var responseString = await response.Content.ReadAsStringAsync();

        //    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Failed: {responseString}");
        //}


        //[TestMethod]
        //public async Task Login_ShouldReturnToken_WhenCredentialsAreCorrect()
        //{
        //    var url = "http://localhost:5038/api/Auth/login";

        //    var payload = new
        //    {
        //        Username = "testuser1234",
        //        Password = "test1234"
        //    };

        //    var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

        //    var response = await client.PostAsync(url, content);
        //    var responseString = await response.Content.ReadAsStringAsync();

        //    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Failed: {responseString}");

        //    var data = JsonConvert.DeserializeObject<LoginResponse>(responseString);
        //    Assert.IsFalse(string.IsNullOrEmpty(data.Token));
        //}

        //private class LoginResponse
        //{
        //    public string Username { get; set; }
        //    public string Role { get; set; }
        //    public string Token { get; set; }
        //}
    }
}
