using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using WhatsAppCloud;

namespace WhatsAppCloud.IntegrationTests
{
    /// <summary>
    /// End-to-end tests against the real Meta WhatsApp Business Cloud API
    /// (https://graph.facebook.com). These tests send real messages and are
    /// entirely opt-in: every test skips gracefully unless ALL required
    /// environment variables are set:
    ///   WHATSAPP_ACCESS_TOKEN    - a permanent page / system-user token
    ///   WHATSAPP_PHONE_NUMBER_ID - the test phone number ID from the Meta console
    ///   WHATSAPP_TEST_NUMBER     - the recipient, in E.164 format (digits only)
    /// See INTEGRATION_TESTING.md in the repository root for setup details.
    /// </summary>
    [TestFixture]
    public class WhatsAppIntegrationTests
    {
        private WhatsAppClient CreateClient(out string testNumber)
        {
            var accessToken = Require("WHATSAPP_ACCESS_TOKEN");
            var phoneNumberId = Require("WHATSAPP_PHONE_NUMBER_ID");
            testNumber = Require("WHATSAPP_TEST_NUMBER");

            return new WhatsAppClient(new WhatsAppClientOptions
            {
                AccessToken = accessToken,
                PhoneNumberId = phoneNumberId
            });
        }

        private static string Require(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                Assert.Ignore($"Skipped: {name} environment variable is not set.");
            return value!;
        }

        private static string RequireMessageId(Models.SendMessageResult result, string what)
        {
            Assert.IsTrue(result.Successful, $"The {what} call did not return a message id.");
            Assert.IsFalse(string.IsNullOrEmpty(result.MessageId));
            return result.MessageId!;
        }

        [Test]
        public async Task SendTextMessage_ToTestNumber_ReturnsMessageId()
        {
            var client = CreateClient(out var testNumber);

            var result = await client.SendTextAsync(
                testNumber,
                $"Integration test {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC - WhatsAppCloud .NET SDK");

            RequireMessageId(result, "send");
            Assert.AreEqual(testNumber, result.To);
        }

        [Test]
        public async Task SendTemplateMessage_HelloWorld_ReturnsMessageId()
        {
            // hello_world (en_US) ships with every Meta test number, so template
            // sends work even outside an open 24h customer service window.
            var client = CreateClient(out var testNumber);

            var result = await client.SendTemplateAsync(testNumber, "hello_world", "en_US");

            RequireMessageId(result, "send");
        }

        [Test]
        public async Task SendImageMessage_ByPublicUrl_ReturnsMessageId()
        {
            var client = CreateClient(out var testNumber);

            var result = await client.SendImageAsync(
                testNumber,
                "https://placehold.co/600x400/png",
                "WhatsAppCloud integration test");

            RequireMessageId(result, "send");
        }

        [Test]
        public async Task SendLocationMessage_ReturnsMessageId()
        {
            var client = CreateClient(out var testNumber);

            var result = await client.SendLocationAsync(
                testNumber, 30.0444, 31.2357, "Cairo", "Integration test location");

            RequireMessageId(result, "send");
        }

        [Test]
        public async Task UploadMedia_ThenSendById_ThenDelete_WorksEndToEnd()
        {
            var client = CreateClient(out var testNumber);

            // A tiny valid PNG (1x1 pixel) written to a temp file.
            var pngBytes = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
            var tempPath = Path.Combine(Path.GetTempPath(), $"wacloud-test-{Guid.NewGuid():N}.png");
            await File.WriteAllBytesAsync(tempPath, pngBytes);
            try
            {
                var upload = await client.UploadMediaAsync(tempPath, "image/png");
                Assert.IsTrue(upload.Successful, "The media upload did not return a media id.");
                Assert.IsFalse(string.IsNullOrEmpty(upload.MediaId));
                var mediaId = upload.MediaId!;

                var info = await client.GetMediaInfoAsync(mediaId);
                Assert.IsNotNull(info);
                Assert.IsFalse(string.IsNullOrEmpty(info!.Url));

                var sent = await client.SendImageByIdAsync(
                    testNumber, mediaId, "Uploaded via WhatsAppCloud .NET SDK");
                RequireMessageId(sent, "send-by-id");

                Assert.IsTrue(await client.DeleteMediaAsync(mediaId), "Media delete failed.");
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Test]
        public async Task MarkAsRead_AfterSend_CompletesWithoutError()
        {
            var client = CreateClient(out var testNumber);

            var sent = await client.SendTextAsync(testNumber, "Mark-as-read integration test");
            var messageId = RequireMessageId(sent, "send");

            Assert.DoesNotThrowAsync(async () =>
                await client.MarkAsReadAsync(messageId));
        }
    }
}
