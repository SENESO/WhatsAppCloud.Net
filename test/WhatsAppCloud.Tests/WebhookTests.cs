using System;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using WhatsAppCloud.Webhooks;

namespace WhatsAppCloud.Tests
{
    public class WebhookTests
    {
        private const string AppSecret = "test-app-secret";
        private const string Body = @"{""object"":""whatsapp_business_account"",""entry"":[]}";

        private static string Sign(string secret, string body)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                return "sha256=" + BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        [Test]
        public void ValidSignature_Passes()
        {
            Assert.IsTrue(WebhookSecurity.IsValidSignature(AppSecret, Body, Sign(AppSecret, Body)));
        }

        [Test]
        public void TamperedBody_Fails()
        {
            Assert.IsFalse(WebhookSecurity.IsValidSignature(AppSecret, Body + "x", Sign(AppSecret, Body)));
        }

        [Test]
        public void WrongSecret_Fails()
        {
            Assert.IsFalse(WebhookSecurity.IsValidSignature("other-secret", Body, Sign(AppSecret, Body)));
        }

        [Test]
        public void MissingHeader_Fails()
        {
            Assert.IsFalse(WebhookSecurity.IsValidSignature(AppSecret, Body, null));
        }

        [Test]
        public void VerifyChallenge_ReturnsChallengeOnMatch()
        {
            var challenge = WebhookSecurity.VerifyChallenge("subscribe", "my-token", "abc123", "my-token");
            Assert.AreEqual("abc123", challenge);
        }

        [Test]
        public void VerifyChallenge_ReturnsNullOnMismatch()
        {
            Assert.IsNull(WebhookSecurity.VerifyChallenge("subscribe", "wrong", "abc123", "my-token"));
            Assert.IsNull(WebhookSecurity.VerifyChallenge("unsubscribe", "my-token", "abc123", "my-token"));
        }

        [Test]
        public void Parser_ExtractsTextMessages()
        {
            const string json = @"{
                ""object"": ""whatsapp_business_account"",
                ""entry"": [{
                    ""id"": ""111"",
                    ""changes"": [{
                        ""field"": ""messages"",
                        ""value"": {
                            ""messaging_product"": ""whatsapp"",
                            ""messages"": [{
                                ""from"": ""201012345678"",
                                ""id"": ""wamid.aaa"",
                                ""timestamp"": ""1728000000"",
                                ""type"": ""text"",
                                ""text"": { ""body"": ""Hello bot"" }
                            }]
                        }
                    }]
                }]
            }";

            var messages = WebhookParser.GetMessages(WebhookParser.Parse(json));

            Assert.AreEqual(1, messages.Count);
            Assert.AreEqual("201012345678", messages[0].From);
            Assert.AreEqual("wamid.aaa", messages[0].Id);
            Assert.AreEqual("Hello bot", messages[0].Text);
        }

        [Test]
        public void Parser_ExtractsStatuses()
        {
            const string json = @"{
                ""object"": ""whatsapp_business_account"",
                ""entry"": [{
                    ""id"": ""111"",
                    ""changes"": [{
                        ""field"": ""messages"",
                        ""value"": {
                            ""messaging_product"": ""whatsapp"",
                            ""statuses"": [{
                                ""id"": ""wamid.bbb"",
                                ""status"": ""delivered"",
                                ""timestamp"": ""1728000001"",
                                ""recipient_id"": ""201012345678""
                            }]
                        }
                    }]
                }]
            }";

            var statuses = WebhookParser.GetStatuses(WebhookParser.Parse(json));

            Assert.AreEqual(1, statuses.Count);
            Assert.AreEqual("wamid.bbb", statuses[0].MessageId);
            Assert.AreEqual("delivered", statuses[0].Status);
        }
    }
}
