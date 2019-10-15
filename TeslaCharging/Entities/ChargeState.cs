using Newtonsoft.Json;

namespace TeslaCharging.Entities
{
    public class ChargeState
    {
        [JsonProperty(propertyName: "charge_energy_added")]
        public string ChargeEnergyAdded { get; set; }
        [JsonProperty(propertyName: "charging_state")]
        public ChargingStatus ChargingState { get; set; }
        public long Timestamp { get; set; }
    }
}