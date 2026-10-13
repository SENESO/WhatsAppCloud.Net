using System;
using System.Collections.Generic;
using System.IO;
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

            Assert.AreEqual(131030, ex!.Code);
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

        [Test]
        public async Task SendVideoAsync_PostsVideoPayload()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            var result = await client.SendVideoAsync("201012345678", "https://example.com/v.mp4", "Watch this");

            Assert.IsTrue(result.Successful);
            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                Assert.AreEqual("video", doc.RootElement.GetProperty("type").GetString());
                Assert.AreEqual("https://example.com/v.mp4",
                    doc.RootElement.GetProperty("video").GetProperty("link").GetString());
            }
        }

        [Test]
        public async Task SendLocationAsync_PostsCoordinates()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            await client.SendLocationAsync("201012345678", 30.0444, 31.2357, "Cairo", "Egypt");

            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                var loc = doc.RootElement.GetProperty("location");
                Assert.AreEqual(30.0444, loc.GetProperty("latitude").GetDouble(), 0.0001);
                Assert.AreEqual(31.2357, loc.GetProperty("longitude").GetDouble(), 0.0001);
                Assert.AreEqual("Cairo", loc.GetProperty("name").GetString());
            }
        }

        [Test]
        public async Task SendButtonsAsync_PostsInteractiveButtons()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            await client.SendButtonsAsync("201012345678", "Pick one",
                new[] { new ReplyButton { Id = "yes", Title = "Yes" }, new ReplyButton { Id = "no", Title = "No" } },
                "footer here");

            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                var interactive = doc.RootElement.GetProperty("interactive");
                Assert.AreEqual("button", interactive.GetProperty("type").GetString());
                var buttons = interactive.GetProperty("action").GetProperty("buttons")
                    .EnumerateArray().ToList();
                Assert.AreEqual(2, buttons.Count);
                Assert.AreEqual("yes", buttons[0].GetProperty("reply").GetProperty("id").GetString());
                Assert.AreEqual("footer here", interactive.GetProperty("footer").GetProperty("text").GetString());
            }
        }

        [Test]
        public async Task SendListAsync_PostsInteractiveList()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            await client.SendListAsync("201012345678", "Choose", "Open",
                new[]
                {
                    new ListSection
                    {
                        Title = "Drinks",
                        Rows = new List<ListRow>
                        {
                            new ListRow { Id = "tea", Title = "Tea" },
                            new ListRow { Id = "coffee", Title = "Coffee", Description = "Hot" }
                        }
                    }
                });

            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                var interactive = doc.RootElement.GetProperty("interactive");
                Assert.AreEqual("list", interactive.GetProperty("type").GetString());
                var rows = interactive.GetProperty("action").GetProperty("sections")[0]
                    .GetProperty("rows").EnumerateArray().ToList();
                Assert.AreEqual(2, rows.Count);
                Assert.AreEqual("coffee", rows[1].GetProperty("id").GetString());
            }
        }

        [Test]
        public async Task MarkAsReadAsync_PostsReadStatus()
        {
            var handler = new FakeHandler(@"{ ""success"": true }");
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            await client.MarkAsReadAsync("wamid.abc");

            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                Assert.AreEqual("read", doc.RootElement.GetProperty("status").GetString());
                Assert.AreEqual("wamid.abc", doc.RootElement.GetProperty("message_id").GetString());
            }
        }

        [Test]
        public async Task ReplyToMessageId_AddsContext()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            await client.SendTextAsync("201012345678", "replying", replyToMessageId: "wamid.orig");

            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                Assert.AreEqual("wamid.orig",
                    doc.RootElement.GetProperty("context").GetProperty("message_id").GetString());
            }
        }

        // ---------- media upload & media IDs ----------

        [Test]
        public async Task UploadMediaAsync_Stream_PostsMultipartAndReturnsMediaId()
        {
            var handler = new FakeHandler(@"{ ""id"": ""mid.123"" }");
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake-png-bytes")))
            {
                var result = await client.UploadMediaAsync(stream, "pic.png", "image/png");

                Assert.IsTrue(result.Successful);
                Assert.AreEqual("mid.123", result.MediaId);
            }

            Assert.AreEqual("https://graph.facebook.com/v26.0/123456789/media", handler.RequestUrl);
            Assert.AreEqual("POST", handler.RequestMethod);
            Assert.AreEqual("Bearer", handler.AuthScheme);
            Assert.IsTrue(handler.RequestContentType.StartsWith(
                "multipart/form-data", StringComparison.OrdinalIgnoreCase));
            // The multipart body must carry Meta's three required fields.
            Assert.IsTrue(handler.RequestBody.Contains("messaging_product"));
            Assert.IsTrue(handler.RequestBody.Contains("pic.png"));
            Assert.IsTrue(handler.RequestBody.Contains("image/png"));
        }

        [Test]
        public async Task UploadMediaAsync_FilePath_UploadsFileContents()
        {
            var handler = new FakeHandler(@"{ ""id"": ""mid.456"" }");
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            var tempPath = Path.Combine(Path.GetTempPath(), $"wacloud-{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(tempPath, "hello-media");
            try
            {
                var result = await client.UploadMediaAsync(tempPath, "text/plain");
                Assert.AreEqual("mid.456", result.MediaId);
                Assert.IsTrue(handler.RequestBody.Contains("hello-media"));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Test]
        public void UploadMediaAsync_Error_ThrowsWhatsAppApiException()
        {
            var handler = new FakeHandler(@"{ ""error"": {
                ""message"": ""Invalid file type"", ""type"": ""OAuthException"", ""code"": 131052 } }");
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            using (var stream = new MemoryStream(new byte[] { 1, 2, 3 }))
            {
                var ex = Assert.ThrowsAsync<WhatsAppApiException>(async () =>
                    await client.UploadMediaAsync(stream, "x.bin", "application/octet-stream"));
                Assert.AreEqual(131052, ex!.Code);
            }
        }

        [Test]
        public void UploadMediaAsync_InvalidArguments_Throw()
        {
            var client = new WhatsAppClient(Options(), new HttpClient(new FakeHandler("{}")));

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await client.UploadMediaAsync((Stream)null!, "x.png", "image/png"));
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await client.UploadMediaAsync(" ", "image/png"));
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await client.UploadMediaAsync(new MemoryStream(), "x.png", " "));
        }

        [Test]
        public async Task SendImageByIdAsync_PostsIdInsteadOfLink()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            var result = await client.SendImageByIdAsync("201012345678", "mid.123", "Nice pic");

            Assert.IsTrue(result.Successful);
            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                Assert.AreEqual("image", doc.RootElement.GetProperty("type").GetString());
                var image = doc.RootElement.GetProperty("image");
                Assert.AreEqual("mid.123", image.GetProperty("id").GetString());
                Assert.AreEqual("Nice pic", image.GetProperty("caption").GetString());
                Assert.Throws<KeyNotFoundException>(() => image.GetProperty("link"));
            }
        }

        [Test]
        public async Task SendVideoByIdAsync_PostsIdPayload()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            await client.SendVideoByIdAsync("201012345678", "mid.vid");

            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                Assert.AreEqual("mid.vid",
                    doc.RootElement.GetProperty("video").GetProperty("id").GetString());
            }
        }

        [Test]
        public async Task SendAudioByIdAsync_PostsIdPayload()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            await client.SendAudioByIdAsync("201012345678", "mid.aud");

            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                Assert.AreEqual("mid.aud",
                    doc.RootElement.GetProperty("audio").GetProperty("id").GetString());
            }
        }

        [Test]
        public async Task SendDocumentByIdAsync_PostsIdAndFilename()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            await client.SendDocumentByIdAsync("201012345678", "mid.doc", "invoice.pdf");

            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                var document = doc.RootElement.GetProperty("document");
                Assert.AreEqual("mid.doc", document.GetProperty("id").GetString());
                Assert.AreEqual("invoice.pdf", document.GetProperty("filename").GetString());
            }
        }

        [Test]
        public async Task SendStickerByIdAsync_PostsIdPayload()
        {
            var handler = new FakeHandler(SuccessJson);
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            await client.SendStickerByIdAsync("201012345678", "mid.stk");

            using (var doc = JsonDocument.Parse(handler.RequestBody))
            {
                Assert.AreEqual("mid.stk",
                    doc.RootElement.GetProperty("sticker").GetProperty("id").GetString());
            }
        }

        [Test]
        public void SendById_EmptyMediaId_Throws()
        {
            var client = new WhatsAppClient(Options(), new HttpClient(new FakeHandler(SuccessJson)));

            Assert.ThrowsAsync<ArgumentException>(async () =>
                await client.SendImageByIdAsync("201012345678", " "));
        }

        [Test]
        public async Task GetMediaInfoAsync_ParsesMetadata()
        {
            var handler = new FakeHandler(@"{
                ""id"": ""mid.123"",
                ""url"": ""https://lookaside.fbsbx.com/x"",
                ""mime_type"": ""image/jpeg"",
                ""sha256"": ""c2hhMjU2"",
                ""file_size"": ""109982""
            }");
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            var info = await client.GetMediaInfoAsync("mid.123");

            Assert.AreEqual("https://graph.facebook.com/v26.0/mid.123", handler.RequestUrl);
            Assert.AreEqual("GET", handler.RequestMethod);
            Assert.IsNotNull(info);
            Assert.AreEqual("mid.123", info!.Id);
            Assert.AreEqual("https://lookaside.fbsbx.com/x", info.Url);
            Assert.AreEqual("image/jpeg", info.MimeType);
            Assert.AreEqual("c2hhMjU2", info.Sha256);
            Assert.AreEqual(109982, info.FileSizeBytes);
        }

        [Test]
        public async Task DeleteMediaAsync_ReturnsTrueOnSuccess()
        {
            var handler = new FakeHandler(@"{ ""success"": true }");
            var client = new WhatsAppClient(Options(), new HttpClient(handler));

            var deleted = await client.DeleteMediaAsync("mid.123");

            Assert.IsTrue(deleted);
            Assert.AreEqual("https://graph.facebook.com/v26.0/mid.123", handler.RequestUrl);
            Assert.AreEqual("DELETE", handler.RequestMethod);
            Assert.AreEqual("Bearer", handler.AuthScheme);
        }

        private class FakeHandler : HttpMessageHandler
        {
            private readonly string _responseJson;
            public string RequestUrl { get; private set; } = string.Empty;
            public string RequestMethod { get; private set; } = string.Empty;
            public string? AuthScheme { get; private set; }
            public string? AuthParameter { get; private set; }
            public string RequestBody { get; private set; } = string.Empty;
            public string RequestContentType { get; private set; } = string.Empty;

            public FakeHandler(string responseJson) => _responseJson = responseJson;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Capture everything while the request is still alive.
                RequestUrl = request.RequestUri?.ToString() ?? string.Empty;
                RequestMethod = request.Method.Method;
                AuthScheme = request.Headers.Authorization?.Scheme;
                AuthParameter = request.Headers.Authorization?.Parameter;
                RequestContentType = request.Content?.Headers.ContentType?.ToString() ?? string.Empty;
                RequestBody = request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync();

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
