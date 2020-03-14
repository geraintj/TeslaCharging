using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace GoogleApi.Geocode
{
    [JsonObject("plus_code")]
    public class PlusCode
    {
        [JsonProperty("compound_code")]
        public string CompoundCode { get; set; }
        [JsonProperty("global_code")]
        public string GlobalCode { get; set; }
    }
}
