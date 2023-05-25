using LocationTest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
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
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
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
            //log.LogInformation($"Executing 'OrchestrateCheck' with email '{loginData.Email}''");


            // set up monitor
            while (true)
            {
                var dataResponse = await context.CallActivityAsync<DataResponse>("CallTeslaAPI", orchestrationData.LoginData);

                var lastChargeStatus = await context.CallEntityAsync<ChargingStatus>(orchestrationData.EntityId, "Get");

                if (dataResponse != null && dataResponse.ChargeState.ChargingState != lastChargeStatus)
                {
                    if (lastChargeStatus == ChargingStatus.Charging)
                    {
                        log.LogInformation($"************** SAVE TO DB, new: {dataResponse.ChargeState.ChargingState}; old: {lastChargeStatus}");
                        await context.CallActivityAsync("SaveCharge", dataResponse);
                    } 
                    log.LogInformation($"************** Setting LastChargeStatus in Entity to {dataResponse.ChargeState.ChargingState.ToString()}. Replaying {context.IsReplaying}");

                    context.SignalEntity(orchestrationData.EntityId, "Set", dataResponse.ChargeState.ChargingState);
                }
                else
                {
                    log.LogInformation($"************** Charge status not changed from {lastChargeStatus.ToString()}");
                }

                var nextCheckTime = context.CurrentUtcDateTime.AddSeconds(30);
                await context.CreateTimer(nextCheckTime, CancellationToken.None);
            }
        }

        [FunctionName("CallTeslaAPI")]
        public async Task<DataResponse> CallTeslaApi([ActivityTrigger] TeslaLogin loginData, ILogger log)
        {
            if (loginData != null)
            {
                try
                {
                    log.LogInformation("Start CallTeslApi");

                    var stringContent = new StringContent(
                        $"{{ \"email\": \"{loginData.Email}\", \"password\": \"{loginData.Password}\" }}",
                        Encoding.UTF8);

                    var tokenResponse =
                        await _httpClient.PostAsJsonAsync(new Uri(Environment.GetEnvironmentVariable("TeslaTokenUri")), stringContent);
                    var tokenResult = await tokenResponse.Content.ReadAsStringAsync();
                    log.LogInformation($"Token response: HTTP {tokenResponse.StatusCode}");

                   _httpClient.DefaultRequestHeaders.Add("User-Agent", "TeslaCharging");
                   _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", tokenResult);

                    var vehiclesResponse =
                        await _httpClient.GetStringAsync(
                            new Uri($"{Environment.GetEnvironmentVariable("TeslaUri")}api/1/vehicles"));
                    var vehiclesResult = JsonConvert.DeserializeObject<VehiclesResponse>(vehiclesResponse);
                    log.LogInformation($"Vehicles response: HTTP {(string.IsNullOrEmpty(vehiclesResponse) ? "failed" : "successful")}");

                    var wakeUpResponse = await _httpClient.PostAsync(new Uri(
                        $"{Environment.GetEnvironmentVariable("TeslaUri")}api/1/vehicles/{vehiclesResult.Response[0].Id}/wake_up"), null);
                    var wakeUpResult = JsonConvert.DeserializeObject<WakeUpResponse>(await wakeUpResponse.Content.ReadAsStringAsync());
                    log.LogInformation($"Vehicles response: HTTP {wakeUpResponse.StatusCode}");

                    var dataResponseString = await _httpClient.GetStringAsync(
                        new Uri(
                            $"{Environment.GetEnvironmentVariable("TeslaUri")}api/1/vehicles/{vehiclesResult.Response[0].Id}/vehicle_data"));
                    if (string.IsNullOrEmpty(dataResponseString))
                    {
                        log.LogCritical("Data response null");
                        return null;
                    }
                    var dataResponseMessage = JsonConvert.DeserializeObject<DataResponseMessage>(dataResponseString);
                    return dataResponseMessage.Response;
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
        public async Task SaveCharge([ActivityTrigger] DataResponse dataResponse, [CosmosDB(
            databaseName: "ChargeState",
            containerName: "Charges",
            Connection = "CosmosConnection")]IAsyncCollector<TeslaCharge> teslaCharge, ILogger log)
        {
            var client = new HttpClient();
            var locationResponse = await client.PostAsync(Environment.GetEnvironmentVariable(
                "GoogleReverseGeocodeUri") +
                $"?latlng={dataResponse.DriveState.Latitude},{dataResponse.DriveState.Longitude}&key={Environment.GetEnvironmentVariable("GoogleMapsApiKey")}", null);
            var locationResult = JsonConvert.DeserializeObject<ReverseResponse>(await locationResponse.Content.ReadAsStringAsync());

            var newCharge = new TeslaCharge()
            {
                Vin = dataResponse.Vin,
                Amount = dataResponse.ChargeState.ChargeEnergyAdded,
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
            [CosmosDB(Connection = "CosmosConnection")] CosmosClient client,
            ILogger log, string vin)
        {
            //TODO: validation for null vin
            log.LogInformation("C# HTTP trigger function processed a request.");

            var container = client.GetContainer(databaseId: "ChargeState", containerId: "Charges");

            IOrderedQueryable<TeslaCharge> queryable = container.GetItemLinqQueryable<TeslaCharge>();

            var results = queryable.Where(x => x.Vin == vin);

            using FeedIterator<TeslaCharge> query = results.ToFeedIterator();

            var savedCharges = new List<TeslaCharge>();

            while (query.HasMoreResults)
            {
                FeedResponse<TeslaCharge> response = await query.ReadNextAsync();

                foreach (TeslaCharge charge in response)
                {
                    savedCharges.Add(charge);
                }
            }

            return new OkObjectResult(savedCharges);
        }

        [FunctionName("DeleteSavedCharge")]
        public async Task<IActionResult> DeleteSavedCharge([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "charges/{vin}/{id}")]
            HttpRequest req,
            [CosmosDB(Connection = "CosmosConnection")]
            CosmosClient client,
            ILogger log,
            string vin,
            string id)
        {
            //TODO: validation for null  id
            log.LogInformation("C# HTTP trigger function processed a request.");

            var container = client.GetContainer(databaseId: "ChargeState", containerId: "Charges");
            try
            {
                var response = await container.DeleteItemAsync<TeslaCharge>(id, new PartitionKey(string.Empty));

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