using System.Collections.Generic;
using Newtonsoft.Json;

namespace GoogleApi.Geocode
{
    public class AddressComponent
    {
        [JsonProperty("long_name")]
        public string LongName { get; set; }
        [JsonProperty("short_name")]
        public string ShortName { get; set; }
        public List<string> Types { get; set; }
    }
}