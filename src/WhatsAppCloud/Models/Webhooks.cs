using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WhatsAppCloud.Models
{
    /// <summary>
    /// A single inbound text message, flattened from the webhook payload.
    /// </summary>
    public class IncomingMessage
    {
        /// <summary>Sender phone number (wa_id where available).</summary>
        public string From { get; set; }

        /// <summary>WhatsApp message id (wamid.…).</summary>
        public string Id { get; set; }

        /// <summary>Unix timestamp of the message.</summary>
        public string Timestamp { get; set; }

        /// <summary>Message type: "text", "image", …</summary>
        public string Type { get; set; }

        /// <summary>Body for text messages; caption for media messages.</summary>
        public string Text { get; set; }
    }

    /// <summary>
    /// Delivery/read status update for an outbound message.
    /// </summary>
    public class MessageStatus
    {
        public string MessageId { get; set; }
        public string Status { get; set; } // sent, delivered, read, failed
        public string Timestamp { get; set; }
        public string RecipientId { get; set; }
    }

    // Raw webhook payload shape (only the parts we care about).
    public class WebhookPayload
    {
        [JsonPropertyName("object")]
        public string Object { get; set; }

        [JsonPropertyName("entry")]
        public List<WebhookEntry> Entry { get; set; }
    }

    public class WebhookEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("changes")]
        public List<WebhookChange> Changes { get; set; }
    }

    public class WebhookChange
    {
        [JsonPropertyName("field")]
        public string Field { get; set; }

        [JsonPropertyName("value")]
        public WebhookValue Value { get; set; }
    }

    public class WebhookValue
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; set; }

        [JsonPropertyName("messages")]
        public List<WebhookMessage> Messages { get; set; }

        [JsonPropertyName("statuses")]
        public List<WebhookStatus> Statuses { get; set; }
    }

    public class WebhookMessage
    {
        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("text")]
        public WebhookText Text { get; set; }

        [JsonPropertyName("image")]
        public WebhookMedia Image { get; set; }
    }

    public class WebhookText
    {
        [JsonPropertyName("body")]
        public string Body { get; set; }
    }

    public class WebhookMedia
    {
        [JsonPropertyName("caption")]
        public string Caption { get; set; }

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }

    public class WebhookStatus
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("recipient_id")]
        public string RecipientId { get; set; }
    }
}
