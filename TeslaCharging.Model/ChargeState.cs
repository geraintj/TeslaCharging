using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;

namespace TeslaCharging.Model
{
    public class ChargeState
    {
        [JsonProperty(propertyName: "charge_energy_added")]
        public decimal ChargeEnergyAdded { get; set; }
        [JsonProperty(propertyName: "charging_state")]
        public ChargingStatus ChargingState { get; set; }
        public long Timestamp { get; set; }
    }
}