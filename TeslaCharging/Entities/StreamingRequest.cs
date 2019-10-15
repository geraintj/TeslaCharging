using Newtonsoft.Json;

namespace TeslaCharging.Entities
{
    public class StreamingRequest
    {
        [JsonProperty(propertyName:"msg_type")]
        public string MessageType { get; set; }
        public string Token { get; set; }
        public string Value { get; set; }
        public string Tag { get; set; }
    }
}