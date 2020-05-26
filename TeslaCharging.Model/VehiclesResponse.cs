using System.Collections.Generic;

namespace TeslaCharging.Model
{
    public class VehiclesResponse
    {
        public List<VehiclesResponseVehicle> Response { get; set; }
        public int Count { get; set; }
    }
}