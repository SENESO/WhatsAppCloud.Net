![WhatsAppCloud.Net](docs/banner.png)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0%20%7C%20net8.0-512BD4)](https://dotnet.microsoft.com/)
[![GitHub stars](https://img.shields.io/github/stars/SENESO/WhatsAppCloud.Net)](https://github.com/SENESO/WhatsAppCloud.Net/stargazers)
[![NuGet](https://img.shields.io/nuget/v/WhatsAppCloud.Net)](https://www.nuget.org/packages/WhatsAppCloud.Net)

# WhatsAppCloud.Net

The missing .NET SDK for Meta's **WhatsApp Business Cloud API** — more complete
than Meta's own Node.js SDK: every message type, read receipts, replies,
webhook security, and typed parsing.

Send text, template, image, video, audio, document, sticker, location,
contacts, and interactive buttons/lists. Verify webhooks, parse inbound
messages. No dependencies beyond `System.Text.Json`. Targets `netstandard2.0`
and `net8.0`.

## Install

```bash
dotnet add package WhatsAppCloud.Net
```

## Quick start

```csharp
using WhatsAppCloud;

var client = new WhatsAppClient(new WhatsAppClientOptions
{
    PhoneNumberId = "<phone-number-id>",   // from the Meta developer console
    AccessToken = "<access-token>"         // system user token
});

// Free-form text (needs an open 24h customer service window)
await client.SendTextAsync("201012345678", "Hello from .NET!");

// Approved template with {{1}}, {{2}} parameters
await client.SendTemplateAsync("201012345678", "order_update", "en_US",
    new[] { "Eslam", "12345" });

// Image by URL
await client.SendImageAsync("201012345678",
    "https://example.com/order.jpg", caption: "Your order shipped!");

// Video, audio, document, sticker
await client.SendVideoAsync("201012345678", "https://example.com/demo.mp4");
await client.SendDocumentAsync("201012345678", "https://example.com/invoice.pdf", filename: "invoice.pdf");

// Location & contacts
await client.SendLocationAsync("201012345678", 30.0444, 31.2357, "Cairo", "Egypt");
await client.SendContactsAsync("201012345678", new[]
{
    new WhatsAppContact { FormattedName = "Ahmed Hassan", FirstName = "Ahmed",
        Phones = new List<ContactPhone> { new ContactPhone { Phone = "+201012345678" } } }
});

// Interactive reply buttons (up to 3)
await client.SendButtonsAsync("201012345678", "Confirm your order?",
    new[]
    {
        new ReplyButton { Id = "confirm", Title = "Confirm" },
        new ReplyButton { Id = "cancel", Title = "Cancel" }
    });

// Interactive list
await client.SendListAsync("201012345678", "Pick a drink", "Open menu",
    new[]
    {
        new ListSection
        {
            Title = "Drinks",
            Rows = new List<ListRow>
            {
                new ListRow { Id = "tea", Title = "Tea" },
                new ListRow { Id = "coffee", Title = "Coffee", Description = "Freshly brewed" }
            }
        }
    });

// Reply to a specific message (works on every send method)
await client.SendTextAsync("201012345678", "Got it!", replyToMessageId: "wamid.xyz");

// Blue ticks — call when your webhook receives a message
await client.MarkAsReadAsync("wamid.xyz");
```

Bring your own `HttpClient` (e.g. from `IHttpClientFactory`):

```csharp
var client = new WhatsAppClient(options, httpClient);
```

## Media upload

Media can be sent by public HTTPS URL (above), or uploaded once and reused by
ID — cheaper for repeated sends (the media ID is valid for 30 days):

```csharp
// Upload from disk…
var upload = await client.UploadMediaAsync("/path/to/photo.jpg", "image/jpeg");

// …or from any stream (memory, network, …)
using (var stream = File.OpenRead("/path/to/invoice.pdf"))
{
    upload = await client.UploadMediaAsync(stream, "invoice.pdf", "application/pdf");
}

Console.WriteLine(upload.MediaId); // pass this to the Send*ByIdAsync methods

// Send by media ID instead of URL
await client.SendImageByIdAsync("201012345678", upload.MediaId, "Your photo!");
await client.SendVideoByIdAsync("201012345678", videoMediaId, "Watch this");
await client.SendAudioByIdAsync("201012345678", audioMediaId);
await client.SendDocumentByIdAsync("201012345678", docMediaId, filename: "invoice.pdf");
await client.SendStickerByIdAsync("201012345678", stickerMediaId);

// Look up a media object (e.g. an ID from an inbound webhook) to get a
// short-lived download URL, MIME type and size — download immediately,
// the URL expires after ~5 minutes
var info = await client.GetMediaInfoAsync(inboundMessage.MediaId);
Console.WriteLine($"{info!.MimeType} — {info.FileSizeBytes} bytes: {info.Url}");

// Free the storage when you're done with an upload
await client.DeleteMediaAsync(upload.MediaId);
```

Size limits: images 5 MB, video/audio 16 MB, documents 100 MB, stickers 500 KB.

## Webhooks

Verification handshake (the GET Meta sends when you save the webhook URL):

```csharp
using WhatsAppCloud.Webhooks;

var challenge = WebhookSecurity.VerifyChallenge(
    mode, verifyToken, challengeParam, expectedVerifyToken);
if (challenge != null) return Results.Text(challenge); // 200 with the challenge
```

Validate inbound POSTs — never skip this on a public URL:

```csharp
var signature = Request.Headers["X-Hub-Signature-256"];
var body = await new StreamReader(Request.Body).ReadToEndAsync();
if (!WebhookSecurity.IsValidSignature(appSecret, body, signature))
    return Results.Unauthorized();

var payload = WebhookParser.Parse(body);
foreach (var msg in WebhookParser.GetMessages(payload))
    Console.WriteLine($"{msg.From}: {msg.Text}");

foreach (var status in WebhookParser.GetStatuses(payload))
    Console.WriteLine($"{status.MessageId}: {status.Status}");
```

`IncomingMessage` carries `MediaId` for inbound media messages (feed it to
`GetMediaInfoAsync` for a download URL), and `ReplyId`/`ReplyTitle` for
interactive button/list replies (`Text` holds the tapped title).

## API reference

**`WhatsAppClient`** (construct with `WhatsAppClientOptions`; optional own `HttpClient`):

| Method | Description |
|---|---|
| `SendTextAsync(to, body, previewUrl?, replyToMessageId?, ct?)` | Free-form text (needs an open 24h window) |
| `SendTemplateAsync(to, templateName, languageCode, bodyParameters?, replyToMessageId?, ct?)` | Approved template with `{{1}}`, `{{2}}`… parameters |
| `SendImageAsync / SendVideoAsync` | Media by public HTTPS URL, optional caption |
| `SendAudioAsync / SendStickerAsync` | Audio / sticker (WEBP) by public HTTPS URL |
| `SendDocumentAsync` | Document by URL, optional filename + caption |
| `SendImageByIdAsync / SendVideoByIdAsync / SendAudioByIdAsync / SendDocumentByIdAsync / SendStickerByIdAsync` | Media by uploaded media ID |
| `UploadMediaAsync(filePath, mimeType, ct?)` / `UploadMediaAsync(stream, fileName, mimeType, ct?)` | Upload media → reusable `MediaUploadResult.MediaId` (30 days) |
| `GetMediaInfoAsync(mediaId, ct?)` | Metadata + short-lived download URL for a media ID |
| `DeleteMediaAsync(mediaId, ct?)` | Delete an uploaded media object |
| `SendLocationAsync(to, latitude, longitude, name?, address?, …)` | Location pin |
| `SendContactsAsync(to, contacts, …)` | One or more contact cards |
| `SendButtonsAsync(to, bodyText, buttons, footerText?, …)` | Up to 3 interactive reply buttons |
| `SendListAsync(to, bodyText, buttonText, sections, footerText?, …)` | Interactive list (≤10 sections × 10 rows) |
| `MarkAsReadAsync(messageId, ct?)` | Blue ticks for an inbound message |
| `ParseSendResponse(json)` | Parse a raw API response (throws `WhatsAppApiException` on errors) |

**Webhooks** (`WhatsAppCloud.Webhooks`): `WebhookSecurity.VerifyChallenge(…)` answers
Meta's verification handshake; `WebhookSecurity.IsValidSignature(appSecret, body, header)`
validates `X-Hub-Signature-256` (constant-time); `WebhookParser.Parse(…)` /
`GetMessages(…)` / `GetStatuses(…)` flatten the payload into `IncomingMessage` /
`MessageStatus` lists.

**Errors**: API errors throw `WhatsAppApiException` with Meta's `Code` and `ErrorType`;
`SendMessageResult.Successful` / `MediaUploadResult.Successful` are false when no ID came back.

## API notes

- Default Graph API version is `v26.0`; override with `WhatsAppClientOptions.ApiVersion`.
- API errors throw `WhatsAppApiException` with Meta's `code` and `type`.
- `SendMessageResult.MessageId` is the `wamid.…` you can correlate with webhook statuses.

## Why not just use the REST API directly?

You can — this SDK just removes the boilerplate: correct payload shapes, template components, auth headers, error mapping, webhook signature validation (constant-time), and payload parsing. PRs welcome.

## License

MIT
