using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Newtonsoft.Json;

namespace TeslaCharging.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ApiToken : IApiToken
    {
        public string AccessToken { get; set; }
        public DateTime Expires { get; set; }

        public void Set(TokenResponse response)
        {
            AccessToken = response.AccessToken;
            Expires = DateTimeOffset.FromUnixTimeMilliseconds(response.Created).DateTime
                .AddSeconds(response.Expiry);
        }

        public async Task<ApiToken> Get()
        {
            return await Task.FromResult(this);
        }

        [FunctionName(nameof(ApiToken))]
        public static Task Run([EntityTrigger] IDurableEntityContext context) =>
            context.DispatchAsync<ApiToken>();
    }
}
