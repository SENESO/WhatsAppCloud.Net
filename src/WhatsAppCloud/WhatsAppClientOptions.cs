namespace WhatsAppCloud
{
    /// <summary>
    /// Options for <see cref="WhatsAppClient"/>.
    /// </summary>
    public class WhatsAppClientOptions
    {
        /// <summary>
        /// The phone number ID from the Meta developer console (not the phone number itself).
        /// </summary>
        public string PhoneNumberId { get; set; }

        /// <summary>
        /// A system-user or temporary access token with whatsapp_business_messaging permission.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Graph API version, e.g. "v26.0". Defaults to the latest known version.
        /// </summary>
        public string ApiVersion { get; set; } = "v26.0";

        /// <summary>
        /// Override for tests or proxies. Defaults to https://graph.facebook.com.
        /// </summary>
        public string BaseUrl { get; set; } = "https://graph.facebook.com";
    }
}
