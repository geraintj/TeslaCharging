using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace TeslaCharging
{
    public static class OrchestrateCheck
    {
        [FunctionName("OrchestrateCheck")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            // get username & password from input

            // set up monitor

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
        public static object CallTeslaApi([ActivityTrigger] string userName, string password)
        {
            
        }

        [FunctionName("OrchestrateCheck_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("OrchestrateCheck", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}