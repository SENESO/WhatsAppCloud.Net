using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WhatsAppCloud.Models;

namespace WhatsAppCloud
{
    /// <summary>
    /// Minimal, dependency-free client for Meta's WhatsApp Business Cloud API.
    /// Pass your own <see cref="HttpClient"/> (e.g. from IHttpClientFactory) to
    /// control lifetime, retries and logging.
    /// </summary>
    public class WhatsAppClient
    {
        private readonly WhatsAppClientOptions _options;
        private readonly HttpClient _http;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // we already use snake_case names
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public WhatsAppClient(WhatsAppClientOptions options, HttpClient httpClient = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(options.PhoneNumberId))
                throw new ArgumentException("PhoneNumberId is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.AccessToken))
                throw new ArgumentException("AccessToken is required.", nameof(options));

            _http = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Send a free-form text message. Only works inside an open 24h customer
        /// service window or to the test number; otherwise use a template.
        /// </summary>
        public Task<SendMessageResult> SendTextAsync(
            string to, string body, bool previewUrl = false,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to,
                type = "text",
                text = new { preview_url = previewUrl, body }
            };
            return PostMessageAsync(payload, cancellationToken);
        }

        /// <summary>
        /// Send an approved message template, with optional {{1}}, {{2}}… body parameters.
        /// </summary>
        public Task<SendMessageResult> SendTemplateAsync(
            string to, string templateName, string languageCode,
            IEnumerable<string> bodyParameters = null,
            CancellationToken cancellationToken = default)
        {
            object template = bodyParameters?.Any() == true
                ? (object)new
                {
                    name = templateName,
                    language = new { code = languageCode },
                    components = new[]
                    {
                        new
                        {
                            type = "body",
                            parameters = bodyParameters
                                .Select(p => new { type = "text", text = p })
                                .ToArray()
                        }
                    }
                }
                : new
                {
                    name = templateName,
                    language = new { code = languageCode }
                };

            var payload = new
            {
                messaging_product = "whatsapp",
                to,
                type = "template",
                template
            };
            return PostMessageAsync(payload, cancellationToken);
        }

        /// <summary>
        /// Send an image by public HTTPS URL, with an optional caption.
        /// </summary>
        public Task<SendMessageResult> SendImageAsync(
            string to, string imageUrl, string caption = null,
            CancellationToken cancellationToken = default)
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                to,
                type = "image",
                image = new { link = imageUrl, caption }
            };
            return PostMessageAsync(payload, cancellationToken);
        }

        /// <summary>
        /// The raw POST used by all send methods. Protected virtual so tests can
        /// intercept without HTTP.
        /// </summary>
        protected virtual async Task<SendMessageResult> PostMessageAsync(
            object payload, CancellationToken cancellationToken)
        {
            var url = $"{_options.BaseUrl.TrimEnd('/')}/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";
            var json = JsonSerializer.Serialize(payload, JsonOptions);

            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using (var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return ParseSendResponse(body);
                }
            }
        }

        public static SendMessageResult ParseSendResponse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new WhatsAppApiException("Empty response from WhatsApp API.", 0, "EmptyResponse");

            using (var doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.TryGetProperty("error", out _))
                {
                    var err = JsonSerializer.Deserialize<ApiErrorResponse>(json, JsonOptions);
                    throw new WhatsAppApiException(
                        err?.Error?.Message ?? "Unknown error",
                        err?.Error?.Code ?? 0,
                        err?.Error?.Type ?? "Unknown");
                }

                var ok = JsonSerializer.Deserialize<SendMessageApiResponse>(json, JsonOptions);
                var message = ok?.Messages?.FirstOrDefault();
                return new SendMessageResult
                {
                    MessageId = message?.Id,
                    To = ok?.Contacts?.FirstOrDefault()?.WaId
                };
            }
        }
    }
}
