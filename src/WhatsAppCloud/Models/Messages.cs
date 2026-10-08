using System.Text.Json.Serialization;

namespace WhatsAppCloud.Models
{
    /// <summary>
    /// Result of a send-message call.
    /// </summary>
    public class SendMessageResult
    {
        /// <summary>
        /// The WhatsApp message id (wamid.…). Null when the send failed.
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// The recipient's WhatsApp ID as echoed by the API.
        /// </summary>
        public string To { get; set; }

        public bool Successful => !string.IsNullOrEmpty(MessageId);
    }

    /// <summary>
    /// Error thrown when the Cloud API answers with an error payload.
    /// </summary>
    public class WhatsAppApiException : System.Exception
    {
        public int Code { get; }
        public string ErrorType { get; }

        public WhatsAppApiException(string message, int code, string errorType)
            : base($"WhatsApp API error {code} ({errorType}): {message}")
        {
            Code = code;
            ErrorType = errorType;
        }
    }

    internal class SendMessageApiResponse
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; set; }

        [JsonPropertyName("contacts")]
        public ApiContact[] Contacts { get; set; }

        [JsonPropertyName("messages")]
        public ApiMessage[] Messages { get; set; }

        public class ApiContact
        {
            [JsonPropertyName("input")]
            public string Input { get; set; }

            [JsonPropertyName("wa_id")]
            public string WaId { get; set; }
        }

        public class ApiMessage
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
        }
    }

    internal class ApiErrorResponse
    {
        [JsonPropertyName("error")]
        public ApiError Error { get; set; }

        public class ApiError
        {
            [JsonPropertyName("message")]
            public string Message { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("code")]
            public int Code { get; set; }
        }
    }
}
