using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using WhatsAppCloud;
using WhatsAppCloud.Models;

namespace WhatsAppCloud.Tests
{
    public class WhatsAppClientTests
    {
        private const string PhoneNumberId = "123456789";
        private const string AccessToken = "test-token";

        private static WhatsAppClientOptions Options() => new WhatsAppClientOptions
        {
            PhoneNumberId = PhoneNumberId,
            AccessToken = AccessToken
        };

        private const string SuccessJson = @"{
            ""messaging_product"": ""whatsapp"",
            ""contacts"": [{ ""input"": ""201012345678"", ""wa_id"": ""201012345678"" }],
            ""messages"": [{ ""id"": ""wamid.test123"" }]
        }";

        [Test]
        public async Task SendTextAsync_PostsCorrectPayload()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            var result = await client.SendTextAsync("201012345678", "Hello!");

            Assert.IsTrue(result.Successful);
            Assert.AreEqual("wamid.test123", result.MessageId);
            Assert.AreEqual("201012345678", result.To);

            Assert.AreEqual("https://graph.facebook.com/v26.0/123456789/messages", handler.RequestUrl);
            Assert.AreEqual("Bearer", handler.AuthScheme);
            Assert.AreEqual(AccessToken, handler.AuthParameter);

            var body = handler.RequestBody;
            using (var doc = JsonDocument.Parse(body))
            {
                var root = doc.RootElement;
                Assert.AreEqual("whatsapp", root.GetProperty("messaging_product").GetString());
                Assert.AreEqual("201012345678", root.GetProperty("to").GetString());
                Assert.AreEqual("text", root.GetProperty("type").GetString());
                Assert.AreEqual("Hello!", root.GetProperty("text").GetProperty("body").GetString());
            }
        }

        [Test]
        public async Task SendTemplateAsync_IncludesBodyParameters()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            var result = await client.SendTemplateAsync(
                "201012345678", "order_update", "en_US", new[] { "Eslam", "12345" });

            Assert.IsTrue(result.Successful);

            var body = handler.RequestBody;
            using (var doc = JsonDocument.Parse(body))
            {
                var template = doc.RootElement.GetProperty("template");
                Assert.AreEqual("order_update", template.GetProperty("name").GetString());
                Assert.AreEqual("en_US", template.GetProperty("language").GetProperty("code").GetString());
                var parameters = template.GetProperty("components")[0]
                    .GetProperty("parameters").EnumerateArray().ToList();
                Assert.AreEqual(2, parameters.Count);
                Assert.AreEqual("Eslam", parameters[0].GetProperty("text").GetString());
                Assert.AreEqual("12345", parameters[1].GetProperty("text").GetString());
            }
        }

        [Test]
        public async Task SendImageAsync_PostsImagePayload()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            var result = await client.SendImageAsync(
                "201012345678", "https://example.com/pic.jpg", "Nice pic");

            Assert.IsTrue(result.Successful);

            var body = handler.RequestBody;
            using (var doc = JsonDocument.Parse(body))
            {
                Assert.AreEqual("image", doc.RootElement.GetProperty("type").GetString());
                var image = doc.RootElement.GetProperty("image");
                Assert.AreEqual("https://example.com/pic.jpg", image.GetProperty("link").GetString());
                Assert.AreEqual("Nice pic", image.GetProperty("caption").GetString());
            }
        }

        [Test]
        public void ApiError_ThrowsWhatsAppApiExceptionWithCode()
        {
            const string errorJson = @"{ ""error"": {
                ""message"": ""Invalid parameter"", ""type"": ""OAuthException"", ""code"": 131030 } }";

            var ex = Assert.Throws<WhatsAppApiException>(() =>
                WhatsAppClient.ParseSendResponse(errorJson));

            Assert.AreEqual(131030, ex.Code);
            Assert.AreEqual("OAuthException", ex.ErrorType);
            Assert.IsTrue(ex.Message.Contains("Invalid parameter"));
        }

        [Test]
        public void MissingOptions_Throw()
        {
            Assert.Throws<ArgumentException>(() =>
                new WhatsAppClient(new WhatsAppClientOptions { AccessToken = "x" }));
            Assert.Throws<ArgumentException>(() =>
                new WhatsAppClient(new WhatsAppClientOptions { PhoneNumberId = "x" }));
        }

        private class FakeHandler : HttpMessageHandler
        {
            private readonly string _responseJson;
            public string RequestUrl { get; private set; }
            public string AuthScheme { get; private set; }
            public string AuthParameter { get; private set; }
            public string RequestBody { get; private set; }

            public FakeHandler(string responseJson) => _responseJson = responseJson;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Capture everything while the request is still alive.
                RequestUrl = request.RequestUri.ToString();
                AuthScheme = request.Headers.Authorization?.Scheme;
                AuthParameter = request.Headers.Authorization?.Parameter;
                RequestBody = await request.Content.ReadAsStringAsync();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
