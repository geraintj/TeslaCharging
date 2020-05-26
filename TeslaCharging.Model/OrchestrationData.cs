using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace TeslaCharging.Model
{
    public class OrchestrationData
    {
        public TeslaLogin LoginData { get; set; }
        public EntityId EntityId { get; set; }
    }
}
