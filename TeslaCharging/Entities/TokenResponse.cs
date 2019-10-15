using Newtonsoft.Json;

namespace TeslaCharging.Entities
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}
