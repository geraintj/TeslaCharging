using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeslaCharging.Model;

namespace TeslaCharging.Tests
{
    public class TestDurableOrchestrationContext : IDurableOrchestrationContext
    {
        public ChargeState ReturnChargeState { get; set; }
        public OrchestrationData ReturnOrchestrationData { get; set; }
        public ChargingStatus ReturnChargingStatus { get; set; }
        public Dictionary<string,ArrayList> CallActivityCalls { get; set; }
        public Dictionary<string, ArrayList> SignalEntityCalls { get; set; }

        public TestDurableOrchestrationContext()
        {
            CallActivityCalls = new Dictionary<string, ArrayList>();
            SignalEntityCalls= new Dictionary<string, ArrayList>();
        }
        
        public TInput GetInput<TInput>()
        {
            if (typeof(TInput).Name == "OrchestrationData")
            {
                return (TInput)Convert.ChangeType(ReturnOrchestrationData, typeof(TInput));
            }

            return (TInput)Convert.ChangeType(null, typeof(TInput));
        }

        public void SetOutput(object output)
        {
            throw new NotImplementedException();
        }

        public void ContinueAsNew(object input, bool preserveUnprocessedEvents = false)
        {
            throw new NotImplementedException();
        }

        public void SetCustomStatus(object customStatusObject)
        {
            throw new NotImplementedException();
        }

        public async Task<DurableHttpResponse> CallHttpAsync(HttpMethod method, Uri uri, string content = null)
        {
            throw new NotImplementedException();
        }

        public async Task<DurableHttpResponse> CallHttpAsync(DurableHttpRequest req)
        {
            throw new NotImplementedException();
        }

        public async Task<TResult> CallEntityAsync<TResult>(EntityId entityId, string operationName, object operationInput)
        {
            if (typeof(TResult).Name == "ChargingStatus")
            {
                return (TResult)Convert.ChangeType(ReturnChargingStatus, typeof(TResult));
            }

            return (TResult)Convert.ChangeType(null, typeof(TResult));
        }

        public async Task CallEntityAsync(EntityId entityId, string operationName, object operationInput)
        {
            throw new NotImplementedException();
        }

        public async Task<TResult> CallSubOrchestratorAsync<TResult>(string functionName, string instanceId, object input)
        {
            throw new NotImplementedException();
        }

        public async Task<TResult> CallSubOrchestratorWithRetryAsync<TResult>(string functionName, RetryOptions retryOptions, string instanceId,
            object input)
        {
            throw new NotImplementedException();
        }

        public async Task<T> CreateTimer<T>(DateTime fireAt, T state, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        public async Task<T> WaitForExternalEvent<T>(string name)
        {
            throw new NotImplementedException();
        }

        public async Task<T> WaitForExternalEvent<T>(string name, TimeSpan timeout, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        public async Task<T> WaitForExternalEvent<T>(string name, TimeSpan timeout, T defaultValue, CancellationToken cancelToken)
        {
            throw new NotImplementedException();
        }

        public async Task<IDisposable> LockAsync(params EntityId[] entities)
        {
            throw new NotImplementedException();
        }

        public bool IsLocked(out IReadOnlyList<EntityId> ownedLocks)
        {
            throw new NotImplementedException();
        }

        public Guid NewGuid()
        {
            throw new NotImplementedException();
        }

        public async Task<TResult> CallActivityAsync<TResult>(string functionName, object input)
        {
            ArrayList callArray;
            if (CallActivityCalls.TryGetValue(functionName, out callArray))
            {
                CallActivityCalls[functionName].Add(input);
            }
            else
            {
                CallActivityCalls.Add(functionName, new ArrayList(){ input});
            }

            if (typeof(TResult).Name == "ChargeState")
            {
                return (TResult) Convert.ChangeType(ReturnChargeState, typeof(TResult));
            }

            return (TResult)Convert.ChangeType(null, typeof(TResult));
        }

        public async Task<TResult> CallActivityWithRetryAsync<TResult>(string functionName, RetryOptions retryOptions, object input)
        {
            throw new NotImplementedException();
        }

        public void SignalEntity(EntityId entity, string operationName, object operationInput = null)
        {
            var signalEntityKey = string.Concat(operationInput.GetType().Name, "_", operationName);
            ArrayList callArray;
            if (SignalEntityCalls.TryGetValue(signalEntityKey, out callArray))
            {
                SignalEntityCalls[signalEntityKey].Add(operationInput);
            }
            else
            {
                callArray = new ArrayList() {operationInput};
                SignalEntityCalls.Add(signalEntityKey, callArray);
            }
        }

        public void SignalEntity(EntityId entity, DateTime scheduledTimeUtc, string operationName, object operationInput = null)
        {
            throw new NotImplementedException();
        }

        public string StartNewOrchestration(string functionName, object input, string instanceId = null)
        {
            throw new NotImplementedException();
        }

        public string Name { get; }
        public string InstanceId { get; }
        public string ParentInstanceId { get; }
        public DateTime CurrentUtcDateTime { get; }
        public bool IsReplaying { get; }
    }
}