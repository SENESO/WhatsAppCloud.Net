using System;
using System.Security.Cryptography;
using System.Text;

namespace WhatsAppCloud.Webhooks
{
    /// <summary>
    /// Security helpers for inbound webhooks.
    /// </summary>
    public static class WebhookSecurity
    {
        /// <summary>
        /// Answers Meta's webhook verification handshake (the GET request Meta sends
        /// when you configure the webhook URL). Returns the challenge to echo back,
        /// or null when the request is not a valid verification attempt.
        /// </summary>
        public static string VerifyChallenge(string mode, string verifyToken, string challenge, string expectedVerifyToken)
        {
            if (mode == "subscribe"
                && !string.IsNullOrEmpty(challenge)
                && verifyToken == expectedVerifyToken)
            {
                return challenge;
            }
            return null;
        }

        /// <summary>
        /// Validates the X-Hub-Signature-256 header (HMAC-SHA256 of the raw request
        /// body, keyed with your app secret). Always validate inbound POSTs on a
        /// publicly reachable webhook — otherwise anyone can forge messages.
        /// </summary>
        public static bool IsValidSignature(string appSecret, string requestBody, string signatureHeader)
        {
            if (string.IsNullOrEmpty(appSecret)
                || string.IsNullOrEmpty(requestBody)
                || string.IsNullOrEmpty(signatureHeader))
                return false;

            const string prefix = "sha256=";
            if (!signatureHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            var providedHex = signatureHeader.Substring(prefix.Length);

            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
                // Meta sends lowercase hex; normalize both sides before comparing.
                var computedHex = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                return FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedHex),
                    Encoding.UTF8.GetBytes(providedHex.ToLowerInvariant()));
            }
        }

        // Constant-time comparison to avoid timing attacks. Implemented manually
        // for netstandard2.0 (CryptographicOperations is netstandard2.1+).
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
