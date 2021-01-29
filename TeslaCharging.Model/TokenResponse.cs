using Newtonsoft.Json;

namespace TeslaCharging.Model
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}
