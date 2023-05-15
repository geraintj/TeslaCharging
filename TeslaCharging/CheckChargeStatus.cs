using LocationTest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.Documents;
using Newtonsoft.Json.Linq;
using TeslaCharging;
using TeslaCharging.Model;

namespace TeslaCharging
{
    public class CheckChargeStatus
    {
        private readonly HttpClient _httpClient;

        public CheckChargeStatus(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [FunctionName("OrchestrateCheck")]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            // get username & password from input
            var orchestrationData = context.GetInput<OrchestrationData>();
            log.LogInformation($"Executing 'OrchestrateCheck' with email '{orchestrationData.LoginData.Email}''");


            // set up monitor
            while (true)
            {
                var vehicleData = await context.CallActivityAsync<DataResponse>("CallTeslaAPI", orchestrationData.LoginData);

                var lastChargeStatus = await context.CallEntityAsync<ChargingStatus>(orchestrationData.EntityId, "Get");

                if (vehicleData != null && vehicleData.ChargeState.ChargingState != lastChargeStatus)
                {
                    if (lastChargeStatus == ChargingStatus.Charging)
                    {
                        log.LogInformation($"************** SAVE TO DB, new: {vehicleData.ChargeState.ChargingState}; old: {lastChargeStatus}");
                        await context.CallActivityAsync("SaveCharge", vehicleData);
                    } 
                    log.LogInformation($"************** Setting LastChargeStatus in Entity to {vehicleData.ChargeState.ChargingState.ToString()}. Replaying {context.IsReplaying}");

                    context.SignalEntity(orchestrationData.EntityId, "Set", vehicleData.ChargeState.ChargingState);
                }
                else
                {
                    log.LogInformation($"************** Charge status not changed from {lastChargeStatus.ToString()}");
                }

                var nextCheckTime = context.CurrentUtcDateTime.AddSeconds(300);
                await context.CreateTimer(nextCheckTime, CancellationToken.None);
            }
        }

        [FunctionName("CallTeslaAPI")]
        public async Task<DataResponseMessage> CallTeslaApi([ActivityTrigger] TeslaLogin loginData, ILogger log)
        {
            if (loginData != null)
            {
                try
                {
                    log.LogInformation("Start CallTeslApi without getting token");

                    var tokenLogin = new {email = loginData.Email, password = loginData.Password};

                    var tokenResult = Environment.GetEnvironmentVariable("TeslaToken");

                   _httpClient.DefaultRequestHeaders.Add("User-Agent", "TeslaCharging");
                   _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenResult);

                    var vehiclesResponse =
                        await _httpClient.GetStringAsync(
                            new Uri($"{Environment.GetEnvironmentVariable("TeslaUri")}api/1/vehicles"));
                    if (string.IsNullOrEmpty(vehiclesResponse))
                    {
                        log.LogCritical("Vehicles response null");
                        return null;
                    }
                    var vehiclesResult = JsonConvert.DeserializeObject<VehiclesResponse>(vehiclesResponse);

                    var wakeUpResponse = await _httpClient.PostAsync(new Uri(
                        $"{Environment.GetEnvironmentVariable("TeslaUri")}api/1/vehicles/{vehiclesResult.Response[0].Id}/wake_up"), null);
                    var wakeUpResponseString = await wakeUpResponse.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(wakeUpResponseString))
                    {
                        log.LogCritical("Wake Up response null");
                        return null;
                    }
                    var wakeUpResult = JsonConvert.DeserializeObject<WakeUpResponse>(wakeUpResponseString);

                    var dataResponse = await _httpClient.GetStringAsync(
                        new Uri(
                            $"{Environment.GetEnvironmentVariable("TeslaUri")}api/1/vehicles/{vehiclesResult.Response[0].Id}/vehicle_data"));
                    if (string.IsNullOrEmpty(dataResponse))
                    {
                        log.LogCritical("Data response null");
                        return null;
                    }
                    return JsonConvert.DeserializeObject<DataResponseMessage>(dataResponse);
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
        public async Task SaveCharge([ActivityTrigger] DataResponse data, [CosmosDB(
            databaseName: "ChargeState",
            collectionName: "Charges",
            ConnectionStringSetting = "CosmosConnection")]IAsyncCollector<TeslaCharge> teslaCharge, ILogger log)
        {
            var client = new HttpClient();
            var locationResponse = await client.PostAsync(Environment.GetEnvironmentVariable(
                "GoogleReverseGeocodeUri") +
                $"?latlng={data.DriveState.Latitude},{data.DriveState.Longitude}&key={Environment.GetEnvironmentVariable("GoogleMapsApiKey")}", null);
            var locationResult = JsonConvert.DeserializeObject<ReverseResponse>(await locationResponse.Content.ReadAsStringAsync());

            var newCharge = new TeslaCharge()
            {
                Vin = data.Vin,
                Amount = data.ChargeState.ChargeEnergyAdded,
                Date = DateTime.UtcNow,
                Location = locationResult.Results[0].FormattedAddress
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
        public async Task<IActionResult> GetSavedCharges([HttpTrigger(AuthorizationLevel.Function, "get", Route = "charges/{vin}")] HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "CosmosConnection")] DocumentClient client,
            ILogger log, string vin)
        {
            //TODO: validation for null vin
            log.LogInformation("C# HTTP trigger function processed a request.");

            Uri chargeCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseId: "ChargeState", collectionId: "Charges");

            var options = new FeedOptions { EnableCrossPartitionQuery = true }; 

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

        [FunctionName("DeleteSavedCharge")]
        public async Task<IActionResult> DeleteSavedCharge([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "charges/{vin}/{id}")]
            HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "CosmosConnection")]
            DocumentClient client,
            ILogger log,
            string vin,
            string id)
        {
            //TODO: validation for null  id
            log.LogInformation("C# HTTP trigger function processed a request.");

            var documentUri = UriFactory.CreateDocumentUri(databaseId: "ChargeState", collectionId: "Charges", id);
            try
            {
                var response = await client.DeleteDocumentAsync(documentUri, new RequestOptions() { PartitionKey = new PartitionKey(Undefined.Value) });

                if (response.StatusCode == HttpStatusCode.Accepted
                    || response.StatusCode == HttpStatusCode.Created
                    || response.StatusCode == HttpStatusCode.NoContent)
                {
                    return new OkObjectResult(id);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Attempt to delete document with id {id} " + ex.Message);
            }
            return new InternalServerErrorResult();

        }

        [FunctionName("OrchestrateCheck_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
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