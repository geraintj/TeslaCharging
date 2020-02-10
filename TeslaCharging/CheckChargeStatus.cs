using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
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
        [FunctionName("OrchestrateCheck")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            // get username & password from input
            var orchestrationData = context.GetInput<OrchestrationData>();
            //log.LogInformation($"Executing 'OrchestrateCheck' with email '{loginData.Email}''");


            // set up monitor
            while (true)
            {
                var chargeState = await context.CallActivityAsync<ChargeState>("CallTeslaAPI", orchestrationData.LoginData);

                var lastChargeStatus = await context.CallEntityAsync<ChargingStatus>(orchestrationData.EntityId, "Get");

                if (chargeState != null && chargeState.ChargingState != lastChargeStatus)
                {
                    if (lastChargeStatus == ChargingStatus.Charging)
                    {
                        var savedChargesResponse = await context.CallActivityAsync<IActionResult>("GetSavedCharges", chargeState.Vin);
                        //var savedChargeResponse = await savedChargesResponse.ExecuteResultAsync();

                        //var lastSavedCharge = JsonConvert.DeserializeObject<List<TeslaCharge>>(savedChargesResponse)
                        log.LogInformation($"************** SAVE TO DB, new: {chargeState.ChargingState}; old: {lastChargeStatus}");
                        await context.CallActivityAsync("SaveCharge", chargeState);
                    } 
                    log.LogInformation($"************** Setting LastChargeStatus in Entity to {chargeState.ChargingState.ToString()}. Replaying {context.IsReplaying}");
                    //await entityClient.SignalEntityAsync<ILastChargeState>(entityId, op => op.Set(chargeState.ChargingState));
                    context.SignalEntity(orchestrationData.EntityId, "Set", chargeState.ChargingState);
                    //lastChargeStatus = chargeState.ChargingState;
                }
                else
                {
                    log.LogInformation($"************** Charge status not changed from {lastChargeStatus.ToString()}");
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

                    var wakeUpResponse = await client.PostAsync(new Uri(
                        $"{Environment.GetEnvironmentVariable("TeslaUri")}api/1/vehicles/{vehiclesResult.Response[0].Id}/wake_up"), null);
                    var wakeUpResult = JsonConvert.DeserializeObject<WakeUpResponse>(await wakeUpResponse.Content.ReadAsStringAsync());

                    var chargeStateResponse = await client.GetStringAsync(
                        new Uri(
                            $"{Environment.GetEnvironmentVariable("TeslaUri")}api/1/vehicles/{vehiclesResult.Response[0].Id}/data_request/charge_state"));
                    
                    var chargeStateResult = JsonConvert.DeserializeObject<ChargeStateResponse>(chargeStateResponse);

                    chargeStateResult.Response.Vin = vehiclesResult.Response[0].Vin;
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

        [FunctionName("SaveCharge")]
        public static async void SaveCharge([ActivityTrigger] ChargeState charge, [CosmosDB(
            databaseName: "ChargeState",
            collectionName: "Charges",
            ConnectionStringSetting = "CosmosConnection")]IAsyncCollector<TeslaCharge> teslaCharge, ILogger log)
        {
            var newCharge = new TeslaCharge()
            {
                Vin = charge.Vin,
                Amount = charge.ChargeEnergyAdded,
                Date = DateTime.UtcNow,
                Location = "St Julians"
            };

            try
            {
                await teslaCharge.AddAsync(newCharge);
                log.LogInformation("************** CHARGING DATA SAVED TO DB");
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                throw;
            }
        }

        [FunctionName("GetSavedCharges")]
        public static async Task<IActionResult> GetSavedCharges([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "CosmosConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Validation and error checking omitted for demo purposes

            string vin = req.Query["vin"];

            Uri chargeCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseId: "ChargeState", collectionId: "Charges");

            var options = new FeedOptions { EnableCrossPartitionQuery = true }; // Enable cross partition query

            IDocumentQuery<TeslaCharge> query = client.CreateDocumentQuery<TeslaCharge>(chargeCollectionUri, options)
                .Where(c => c.Vin == vin)
                .AsDocumentQuery();

            var savedCharges = new List<TeslaCharge>();

            while (query.HasMoreResults)
            {
                foreach (TeslaCharge charge in await query.ExecuteNextAsync())
                {
                    savedCharges.Add(charge);
                }
            }

            return new OkObjectResult(savedCharges);
        }

        [FunctionName("OrchestrateCheck_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [DurableClient]IDurableOrchestrationClient starter,
            ILogger log)
        {
            var loginData = await req.Content.ReadAsAsync<TeslaLogin>();

            var entityId = new EntityId(nameof(LastChargeState), loginData.Email);
            //await entityClient.SignalEntityAsync(entityId, "Set", ChargingStatus.Other);

            string instanceId = await starter.StartNewAsync("OrchestrateCheck", new OrchestrationData() { LoginData = loginData, EntityId = entityId});
            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}