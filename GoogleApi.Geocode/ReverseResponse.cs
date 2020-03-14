using System.Collections.Generic;
using GoogleApi.Geocode;
using Newtonsoft.Json;

namespace LocationTest
{
    public class ReverseResponse
    {
        [JsonProperty("plus_code")]
        public PlusCode PlusCode { get; set; }
        public List<Result> Results { get; set; }
        public string Status { get; set; }
    }
}