using Newtonsoft.Json;

namespace TeslaCharging.Model
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
        [JsonProperty("created_at")]
        public int Created { get; set; }
        [JsonProperty("expires_in")]
        public int Expiry { get; set; }
    }
}
