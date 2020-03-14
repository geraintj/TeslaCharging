using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace GoogleApi.Geocode
{
    public class Result
    {
        [JsonProperty("address_components")]
        public List<AddressComponent> AddressComponents { get; set; }
        [JsonProperty("formatted_address")]
        public string FormattedAddress { get; set; }
        public Geometry Geometry { get; set; }
        [JsonProperty("place_id")]
        public string PlaceId { get; set; }
        public List<string> Types { get; set; }
    }
}
