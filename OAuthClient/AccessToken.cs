using System;
using Newtonsoft.Json;

namespace OAuthClient
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc6749#section-4.2.2
    /// </summary>
    public class AccessToken
    {
        private long _expiresInSeconds;

        [JsonProperty("access_token")]
        public string Token { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public long ExpiresInSeconds
        {
            get => _expiresInSeconds;
            set
            {
                _expiresInSeconds = value;
                var secondsToAccountForClockDrift = TimeSpan.FromMinutes(5).Seconds;
                ExpiresOnUtc = DateTime.UtcNow.AddSeconds(_expiresInSeconds - secondsToAccountForClockDrift);
            }
        }

        public DateTime? ExpiresOnUtc { get; private set; }

        public bool IsValid => ExpiresOnUtc != null && DateTime.UtcNow < ExpiresOnUtc;
    }
}