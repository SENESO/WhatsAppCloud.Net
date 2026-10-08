using System;
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
            var accessToken = Environment.GetEnvironmentVariable("WHATSAPP_ACCESS_TOKEN");
            var phoneNumberId = Environment.GetEnvironmentVariable("WHATSAPP_PHONE_NUMBER_ID");
            testNumber = Environment.GetEnvironmentVariable("WHATSAPP_TEST_NUMBER");

            Require(accessToken, nameof(accessToken) + " (WHATSAPP_ACCESS_TOKEN)");
            Require(phoneNumberId, nameof(phoneNumberId) + " (WHATSAPP_PHONE_NUMBER_ID)");
            Require(testNumber, nameof(testNumber) + " (WHATSAPP_TEST_NUMBER)");

            return new WhatsAppClient(new WhatsAppClientOptions
            {
                AccessToken = accessToken,
                PhoneNumberId = phoneNumberId
            });
        }

        private static void Require(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                Assert.Ignore($"Skipped: {name} environment variable is not set.");
        }

        [Test]
        public async Task SendTextMessage_ToTestNumber_ReturnsMessageId()
        {
            var client = CreateClient(out var testNumber);

            var result = await client.SendTextAsync(
                testNumber,
                $"Integration test {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC - WhatsAppCloud .NET SDK");

            Assert.IsTrue(result.Successful, "The send call did not return a message id.");
            Assert.IsFalse(string.IsNullOrEmpty(result.MessageId));
            Assert.AreEqual(testNumber, result.To);
        }

        [Test]
        public async Task SendTemplateMessage_HelloWorld_ReturnsMessageId()
        {
            // hello_world (en_US) ships with every Meta test number, so template
            // sends work even outside an open 24h customer service window.
            var client = CreateClient(out var testNumber);

            var result = await client.SendTemplateAsync(testNumber, "hello_world", "en_US");

            Assert.IsTrue(result.Successful, "The send call did not return a message id.");
            Assert.IsFalse(string.IsNullOrEmpty(result.MessageId));
        }

        [Test]
        public async Task SendImageMessage_ByPublicUrl_ReturnsMessageId()
        {
            var client = CreateClient(out var testNumber);

            var result = await client.SendImageAsync(
                testNumber,
                "https://placehold.co/600x400/png",
                "WhatsAppCloud integration test");

            Assert.IsTrue(result.Successful, "The send call did not return a message id.");
            Assert.IsFalse(string.IsNullOrEmpty(result.MessageId));
        }

        [Test]
        public async Task SendLocationMessage_ReturnsMessageId()
        {
            var client = CreateClient(out var testNumber);

            var result = await client.SendLocationAsync(
                testNumber, 30.0444, 31.2357, "Cairo", "Integration test location");

            Assert.IsTrue(result.Successful, "The send call did not return a message id.");
            Assert.IsFalse(string.IsNullOrEmpty(result.MessageId));
        }

        [Test]
        public async Task MarkAsRead_AfterSend_CompletesWithoutError()
        {
            var client = CreateClient(out var testNumber);

            var sent = await client.SendTextAsync(testNumber, "Mark-as-read integration test");
            Assert.IsTrue(sent.Successful, "The send call did not return a message id.");

            Assert.DoesNotThrowAsync(async () =>
                await client.MarkAsReadAsync(sent.MessageId));
        }
    }
}
