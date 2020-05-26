using System.Collections.Generic;
using Newtonsoft.Json;

namespace TeslaCharging.Model
{
    public class VehiclesResponseVehicle
    {
        public string Id { get; set; }
        [JsonProperty("vehicle_id")]
        public string VehicleId { get; set; }
        public string Vin { get; set; }
        public List<string> Tokens { get; set; }
    }
}