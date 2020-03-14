using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace GoogleApi.Geocode
{
    public class Geometry
    {
        public Bounds Bounds { get; set; }
        public Point Location { get; set; }
        [JsonProperty("location_type")]
        public string LocationType { get; set; }
        public Bounds ViewPort { get; set; }
    }
}
