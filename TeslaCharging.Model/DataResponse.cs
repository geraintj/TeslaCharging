using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace TeslaCharging.Model
{
    public class DataResponse
    {
        public string Vin { get; set; }
        [JsonProperty("charge_state")]
        public ChargeState ChargeState { get; set; }
        [JsonProperty("drive_state")]
        public DriveState DriveState { get; set; }
    }
}
