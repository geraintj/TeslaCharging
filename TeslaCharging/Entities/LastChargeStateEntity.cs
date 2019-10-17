using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace TeslaCharging.Entities
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LastChargeState
    {
        public ChargingStatus Status { get; set; }

        public void Set(ChargingStatus newStatus) => this.Status = newStatus;

        public ChargingStatus Get() => this.Status;

        [FunctionName(nameof(LastChargeState))]
        public static Task Run([EntityTrigger] IDurableEntityContext ctx)
            => ctx.DispatchAsync<LastChargeState>();
    }
}
