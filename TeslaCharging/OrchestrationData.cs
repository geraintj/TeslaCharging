using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeslaCharging.Model;

namespace TeslaCharging
{
    public class OrchestrationData
    {
        public TeslaLogin LoginData { get; set; }
        public EntityId EntityId { get; set; }
    }
}
