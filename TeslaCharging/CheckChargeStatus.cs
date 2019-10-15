using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TeslaCharging.Entities;

namespace TeslaCharging
{
    public static class CheckChargeStatus
    {
        static DurableOrchestrationContext _context;

        [FunctionName("OrchestrateCheck")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger log)
        {
            _context = context;

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
        public static async void CallTeslaApi([ActivityTrigger] TeslaLogin loginData, ILogger log)
        {
            if (loginData != null )
            {
                log.LogInformation("************** Call API");
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "stuff");
                var parameters = new Dictionary<string, string>()
                {
                    {"grant_type", "password"}, {"client_id", Environment.GetEnvironmentVariable("TeslaClientId")},
                    {"client_secret", Environment.GetEnvironmentVariable("TeslaClientSecret")},
                    {"email", loginData.Email}, {"password", loginData.Password}
                };
                var encodedContent = new FormUrlEncodedContent(parameters);

                var tokenResponse =
                    await client.PostAsync(new Uri($"{Environment.GetEnvironmentVariable("TeslaUri")}/oauth/token"),
                        encodedContent);
                var tokenResult =
                    JsonConvert.DeserializeObject<TokenResponse>(await tokenResponse.Content.ReadAsStringAsync());
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

                var vehiclesResponse =
                    await client.GetStringAsync(
                        new Uri($"{Environment.GetEnvironmentVariable("TeslaUri")}/api/1/vehicles"));
                var vehiclesResult = JsonConvert.DeserializeObject<VehiclesResponse>(vehiclesResponse);

                var chargeStateResponse = await client.GetStringAsync(
                    new Uri(
                        $"{Environment.GetEnvironmentVariable("TeslaUri")}/api/1/vehicles/{vehiclesResult.Response[0].Id}/data_request/charge_state"));
                var chargeStateResult = JsonConvert.DeserializeObject<ChargeStateResponse>(chargeStateResponse);

                if (chargeStateResult.Response.ChargingState == ChargingStatus.Charging)
                {
                    // change to 1 minute schedule
                    // save charge status as lastChargeStatus
                }

                log.LogInformation(chargeStateResult.Response.ChargingState.ToString());
                // if lastChargeStatus = Charging and current = Disconnected
                // {
                //      revert to 5 minute schedule
                //      save charge data amount to storage
                // }
            }
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