using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace TeslaCharging.Entities
{
    public class LastChargeState : ILastChargeState
    {
        public ChargingStatus Status { get; set; }

        public void Set(ChargingStatus newStatus)
        {
            Status = newStatus;
        }

        public async Task<ChargingStatus> Get()
        {
            return await Task.FromResult(Status);
        }

        [FunctionName(nameof(LastChargeState))]
        public static Task Run([EntityTrigger] IDurableEntityContext context) =>
            context.DispatchAsync<LastChargeState>();
    }
}