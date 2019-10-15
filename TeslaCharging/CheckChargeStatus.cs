using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using TeslaCharging.Entities;

namespace TeslaCharging
{
    public static class CheckChargeStatus
    {
        [FunctionName("OrchestrateCheck")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger log)
        {
            // get username & password from input
            var loginData = context.GetInput<TeslaLogin>();
            log.LogInformation($"Executing 'OrchestrateCheck' with email '{loginData.Email}''");

            // set up monitor
            while (true)
            {
                await context.CallActivityAsync("CallTeslaAPI", loginData);

                var nextCheckTime = context.CurrentUtcDateTime.AddSeconds(60);
                log.LogInformation($"************** Sleeping orchestration until {nextCheckTime.ToLongTimeString()}");
                await context.CreateTimer(nextCheckTime, CancellationToken.None);
            }

            // while monitor running
            // {
            //      get charge state from  API
            //      get last charge state (LCS) from entity
            //      if LCS not null
            //      {
            //          if LCS.charging_state != CCS.charging_state && CCS.charging_state == "Charging"
            //          {
            //              save to db { charge_energy_added, charger_power, timestamp }
            //          }
            //      }
            //      save current charge state (CCS) to entity { charging_state, charger_power }
            // }


        }

        [FunctionName("CallTeslaAPI")]
        public static void CallTeslaApi([ActivityTrigger] TeslaLogin loginData, ILogger log)
        {
            log.LogInformation("************** Call API");
        }

        [FunctionName("OrchestrateCheck_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            var loginData = await req.Content.ReadAsAsync<TeslaLogin>();
            string instanceId = await starter.StartNewAsync("OrchestrateCheck", loginData);

            log.LogInformation($"************** Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}