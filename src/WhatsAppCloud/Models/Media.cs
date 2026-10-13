using System.Text.Json.Serialization;

namespace WhatsAppCloud.Models
{
    /// <summary>
    /// Result of a media upload. The media ID is valid for 30 days and can be
    /// reused across messages; send messages referencing it with the
    /// Send*ByIdAsync methods instead of re-uploading or hosting a URL.
    /// </summary>
    public class MediaUploadResult
    {
        /// <summary>
        /// The media ID returned by the Cloud API (upload response "id").
        /// </summary>
        public string? MediaId { get; set; }

        public bool Successful => !string.IsNullOrEmpty(MediaId);
    }

    /// <summary>
    /// Metadata for a media object, from GET /{media-id}. The <see cref="Url"/>
    /// is short-lived (about 5 minutes) — download the bytes immediately.
    /// </summary>
    public class MediaInfo
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        public string? MimeType { get; set; }
        public string? Sha256 { get; set; }
        public long? FileSizeBytes { get; set; }
    }

    internal class MediaUploadApiResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    internal class MediaInfoApiResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("mime_type")]
        public string? MimeType { get; set; }

        [JsonPropertyName("sha256")]
        public string? Sha256 { get; set; }

        [JsonPropertyName("file_size")]
        public string? FileSize { get; set; }
    }

    internal class MediaDeleteApiResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
    }
}
