using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using WhatsAppCloud.Models;

namespace WhatsAppCloud
{
    /// <summary>
    /// Full-featured, dependency-free client for Meta's WhatsApp Business Cloud API.
    /// One typed method per message type (text, template, image, video, audio,
    /// document, sticker, location, contacts, interactive buttons/lists), read
    /// receipts, and reply support on every send.
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

        // ---------- text & templates ----------

        /// <summary>
        /// Send a free-form text message. Only works inside an open 24h customer
        /// service window; otherwise use a template.
        /// </summary>
        public Task<SendMessageResult> SendTextAsync(
            string to, string body, bool previewUrl = false, string replyToMessageId = null,
            CancellationToken cancellationToken = default)
        {
            var payload = WithReply(new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to,
                type = "text",
                text = new { preview_url = previewUrl, body }
            }, replyToMessageId);
            return PostMessageAsync(payload, cancellationToken);
        }

        /// <summary>
        /// Send an approved message template, with optional {{1}}, {{2}}… body parameters.
        /// </summary>
        public Task<SendMessageResult> SendTemplateAsync(
            string to, string templateName, string languageCode,
            IEnumerable<string> bodyParameters = null, string replyToMessageId = null,
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

            var payload = WithReply(new
            {
                messaging_product = "whatsapp",
                to,
                type = "template",
                template
            }, replyToMessageId);
            return PostMessageAsync(payload, cancellationToken);
        }

        // ---------- media ----------

        /// <summary>Send an image by public HTTPS URL, with an optional caption.</summary>
        public Task<SendMessageResult> SendImageAsync(
            string to, string imageUrl, string caption = null, string replyToMessageId = null,
            CancellationToken cancellationToken = default)
            => PostMessageAsync(WithReply(new
            {
                messaging_product = "whatsapp",
                to,
                type = "image",
                image = new { link = imageUrl, caption }
            }, replyToMessageId), cancellationToken);

        /// <summary>Send a video by public HTTPS URL, with an optional caption.</summary>
        public Task<SendMessageResult> SendVideoAsync(
            string to, string videoUrl, string caption = null, string replyToMessageId = null,
            CancellationToken cancellationToken = default)
            => PostMessageAsync(WithReply(new
            {
                messaging_product = "whatsapp",
                to,
                type = "video",
                video = new { link = videoUrl, caption }
            }, replyToMessageId), cancellationToken);

        /// <summary>Send an audio file by public HTTPS URL (voice notes play inline).</summary>
        public Task<SendMessageResult> SendAudioAsync(
            string to, string audioUrl, string replyToMessageId = null,
            CancellationToken cancellationToken = default)
            => PostMessageAsync(WithReply(new
            {
                messaging_product = "whatsapp",
                to,
                type = "audio",
                audio = new { link = audioUrl }
            }, replyToMessageId), cancellationToken);

        /// <summary>Send a document by public HTTPS URL.</summary>
        public Task<SendMessageResult> SendDocumentAsync(
            string to, string documentUrl, string filename = null, string caption = null,
            string replyToMessageId = null, CancellationToken cancellationToken = default)
            => PostMessageAsync(WithReply(new
            {
                messaging_product = "whatsapp",
                to,
                type = "document",
                document = new { link = documentUrl, filename, caption }
            }, replyToMessageId), cancellationToken);

        /// <summary>Send a sticker (WEBP) by public HTTPS URL.</summary>
        public Task<SendMessageResult> SendStickerAsync(
            string to, string stickerUrl, string replyToMessageId = null,
            CancellationToken cancellationToken = default)
            => PostMessageAsync(WithReply(new
            {
                messaging_product = "whatsapp",
                to,
                type = "sticker",
                sticker = new { link = stickerUrl }
            }, replyToMessageId), cancellationToken);

        // ---------- location & contacts ----------

        /// <summary>Send a location pin.</summary>
        public Task<SendMessageResult> SendLocationAsync(
            string to, double latitude, double longitude,
            string name = null, string address = null, string replyToMessageId = null,
            CancellationToken cancellationToken = default)
            => PostMessageAsync(WithReply(new
            {
                messaging_product = "whatsapp",
                to,
                type = "location",
                location = new { latitude, longitude, name, address }
            }, replyToMessageId), cancellationToken);

        /// <summary>Send one or more contacts.</summary>
        public Task<SendMessageResult> SendContactsAsync(
            string to, IEnumerable<WhatsAppContact> contacts, string replyToMessageId = null,
            CancellationToken cancellationToken = default)
        {
            if (contacts == null) throw new ArgumentNullException(nameof(contacts));
            return PostMessageAsync(WithReply(new
            {
                messaging_product = "whatsapp",
                to,
                type = "contacts",
                contacts = contacts.Select(c => new
                {
                    name = new
                    {
                        formatted_name = c.FormattedName,
                        first_name = c.FirstName,
                        last_name = c.LastName
                    },
                    phones = (c.Phones ?? Enumerable.Empty<ContactPhone>()).Select(p => new
                    {
                        phone = p.Phone,
                        type = p.Type ?? "CELL",
                        wa_id = p.WhatsAppId
                    }).ToArray()
                }).ToArray()
            }, replyToMessageId), cancellationToken);
        }

        // ---------- interactive ----------

        /// <summary>
        /// Send up to 3 reply buttons. The user's tap comes back as an
        /// interactive message via webhook.
        /// </summary>
        public Task<SendMessageResult> SendButtonsAsync(
            string to, string bodyText, IEnumerable<ReplyButton> buttons,
            string footerText = null, string replyToMessageId = null,
            CancellationToken cancellationToken = default)
        {
            if (buttons == null) throw new ArgumentNullException(nameof(buttons));
            var list = buttons.Take(3).ToList();
            if (list.Count == 0) throw new ArgumentException("At least one button is required.", nameof(buttons));

            return PostMessageAsync(WithReply(new
            {
                messaging_product = "whatsapp",
                to,
                type = "interactive",
                interactive = new
                {
                    type = "button",
                    body = new { text = bodyText },
                    footer = footerText == null ? null : new { text = footerText },
                    action = new
                    {
                        buttons = list.Select(b => new
                        {
                            type = "reply",
                            reply = new { id = b.Id, title = b.Title }
                        }).ToArray()
                    }
                }
            }, replyToMessageId), cancellationToken);
        }

        /// <summary>
        /// Send an interactive list (up to 10 sections, 10 rows each).
        /// </summary>
        public Task<SendMessageResult> SendListAsync(
            string to, string bodyText, string buttonText,
            IEnumerable<ListSection> sections, string footerText = null,
            string replyToMessageId = null, CancellationToken cancellationToken = default)
        {
            if (sections == null) throw new ArgumentNullException(nameof(sections));

            return PostMessageAsync(WithReply(new
            {
                messaging_product = "whatsapp",
                to,
                type = "interactive",
                interactive = new
                {
                    type = "list",
                    body = new { text = bodyText },
                    footer = footerText == null ? null : new { text = footerText },
                    action = new
                    {
                        button = buttonText,
                        sections = sections.Select(s => new
                        {
                            title = s.Title,
                            rows = s.Rows.Select(r => new
                            {
                                id = r.Id,
                                title = r.Title,
                                description = r.Description
                            }).ToArray()
                        }).ToArray()
                    }
                }
            }, replyToMessageId), cancellationToken);
        }

        // ---------- receipts ----------

        /// <summary>
        /// Mark an inbound message as read (blue ticks). Call this when your
        /// webhook receives a message — Meta expects it promptly.
        /// </summary>
        public Task<SendMessageResult> MarkAsReadAsync(
            string messageId, CancellationToken cancellationToken = default)
            => PostMessageAsync(new
            {
                messaging_product = "whatsapp",
                status = "read",
                message_id = messageId
            }, cancellationToken);

        // ---------- plumbing ----------

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

        private static object WithReply(object payload, string replyToMessageId)
        {
            if (string.IsNullOrEmpty(replyToMessageId))
                return payload;

            var node = JsonSerializer.SerializeToNode(payload, JsonOptions);
            node["context"] = new JsonObject { ["message_id"] = replyToMessageId };
            return node;
        }
    }
}
