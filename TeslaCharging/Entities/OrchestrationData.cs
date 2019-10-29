using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.WebJobs;

namespace TeslaCharging.Entities
{
    public class OrchestrationData
    {
        public TeslaLogin LoginData { get; set; }
        public EntityId EntityId { get; set; }
    }
}
