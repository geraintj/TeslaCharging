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
        static ChargingStatus lastChargeStatus = ChargingStatus.Other;

        [FunctionName("OrchestrateCheck")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            // get username & password from input
            var loginData = context.GetInput<TeslaLogin>();
            //log.LogInformation($"Executing 'OrchestrateCheck' with email '{loginData.Email}''");

            // set up monitor
            while (true)
            {
                var chargeState = await context.CallActivityAsync<ChargeState>("CallTeslaAPI", loginData);

                if (chargeState != null && chargeState.ChargingState != lastChargeStatus)
                {
                    if (lastChargeStatus == ChargingStatus.Charging 
                        && (chargeState.ChargingState == ChargingStatus.Complete || chargeState.ChargingState == ChargingStatus.Disconnected ))
                    {
                        log.LogInformation("************** SAVE TO DB");
                    }
                    log.LogInformation($"************** Setting LastChargeStatus in Entity");
                    lastChargeStatus = chargeState.ChargingState;
                }
                else
                {
                    log.LogInformation("************** Charge status not changed");
                }

                var nextCheckTime = context.CurrentUtcDateTime.AddSeconds(30);
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
        public static async Task<ChargeState> CallTeslaApi([ActivityTrigger] TeslaLogin loginData, ILogger log)
        {
            if (loginData != null )
            {
                try
                {
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
                        await client.PostAsync(new Uri($"{Environment.GetEnvironmentVariable("TeslaUri")}oauth/token"),
                            encodedContent);
                    var tokenResult =
                        JsonConvert.DeserializeObject<TokenResponse>(await tokenResponse.Content.ReadAsStringAsync());
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

                    var vehiclesResponse =
                        await client.GetStringAsync(
                            new Uri($"{Environment.GetEnvironmentVariable("TeslaUri")}api/1/vehicles"));
                    var vehiclesResult = JsonConvert.DeserializeObject<VehiclesResponse>(vehiclesResponse);

                    
                    var chargeStateResponse = await client.GetStringAsync(
                        new Uri(
                            $"{Environment.GetEnvironmentVariable("TeslaUri")}api/1/vehicles/{vehiclesResult.Response[0].Id}/data_request/charge_state"));
                    var chargeStateResult = JsonConvert.DeserializeObject<ChargeStateResponse>(chargeStateResponse);

                    return chargeStateResult.Response;
                }
                catch (Exception e)
                {
                    log.LogError(e.Message, e);
                    return null;
                }
            }
            return null;
        }

        [FunctionName("OrchestrateCheck_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [DurableClient]IDurableOrchestrationClient starter,
            ILogger log)
        {
            var loginData = await req.Content.ReadAsAsync<TeslaLogin>();
            string instanceId = await starter.StartNewAsync("OrchestrateCheck", loginData);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}