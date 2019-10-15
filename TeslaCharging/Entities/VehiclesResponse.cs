using System.Collections.Generic;

namespace TeslaCharging.Entities
{
    public class VehiclesResponse
    {
        public List<VehiclesResponseVehicle> Response { get; set; }
        public int Count { get; set; }
    }
}