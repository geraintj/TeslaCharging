using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace GoogleApi.Geocode
{
    public class Point
    {
        [JsonProperty("lat")]
        public double Latitude { get; set; }
        [JsonProperty("long")]
        public double Longitude { get; set; }
    }
}
