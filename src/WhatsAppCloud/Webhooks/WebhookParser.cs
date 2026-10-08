using System.Collections.Generic;
using System.Text.Json;
using WhatsAppCloud.Models;

namespace WhatsAppCloud.Webhooks
{
    /// <summary>
    /// Turns raw webhook JSON into handy <see cref="IncomingMessage"/> /
    /// <see cref="MessageStatus"/> lists.
    /// </summary>
    public static class WebhookParser
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false
        };

        public static WebhookPayload Parse(string json)
            => JsonSerializer.Deserialize<WebhookPayload>(json, JsonOptions);

        /// <summary>
        /// Flattens all inbound messages across entries/changes.
        /// </summary>
        public static List<IncomingMessage> GetMessages(WebhookPayload payload)
        {
            var result = new List<IncomingMessage>();
            if (payload?.Entry == null) return result;

            foreach (var entry in payload.Entry)
            {
                if (entry.Changes == null) continue;
                foreach (var change in entry.Changes)
                {
                    var messages = change.Value?.Messages;
                    if (messages == null) continue;
                    foreach (var m in messages)
                    {
                        result.Add(new IncomingMessage
                        {
                            From = m.From,
                            Id = m.Id,
                            Timestamp = m.Timestamp,
                            Type = m.Type,
                            Text = m.Text?.Body ?? m.Image?.Caption
                        });
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Flattens all delivery/read status updates across entries/changes.
        /// </summary>
        public static List<MessageStatus> GetStatuses(WebhookPayload payload)
        {
            var result = new List<MessageStatus>();
            if (payload?.Entry == null) return result;

            foreach (var entry in payload.Entry)
            {
                if (entry.Changes == null) continue;
                foreach (var change in entry.Changes)
                {
                    var statuses = change.Value?.Statuses;
                    if (statuses == null) continue;
                    foreach (var s in statuses)
                    {
                        result.Add(new MessageStatus
                        {
                            MessageId = s.Id,
                            Status = s.Status,
                            Timestamp = s.Timestamp,
                            RecipientId = s.RecipientId
                        });
                    }
                }
            }

            return result;
        }
    }
}
