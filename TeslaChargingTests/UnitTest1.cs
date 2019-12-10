using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Telerik.JustMock;
using TeslaCharging;
using TeslaCharging.Entities;
using Xunit;

namespace TeslaChargingTests
{
    public class UnitTest1
    {
        private readonly IDurableOrchestrationContext _durableOrchestrationContextMock;
        private readonly ILogger _loggerMock;

        public UnitTest1() 
        {
            _durableOrchestrationContextMock = Mock.Create<IDurableOrchestrationContext>();
            _loggerMock = Mock.Create<ILogger>();
        }

        [Fact]
        public async Task Test1()
        {
            var entityId = new EntityId(nameof(LastChargeState), "test@test.com");

            Mock.Arrange(() => _durableOrchestrationContextMock.GetInput<OrchestrationData>()).Returns(new OrchestrationData() {EntityId = entityId});
            Mock.Arrange(() => _durableOrchestrationContextMock.CallActivityAsync<ChargeState>("CallTeslaAPI", Arg.IsAny<TeslaLogin>()))
                .Returns(Task.FromResult(new ChargeState() { ChargingState = ChargingStatus.Disconnected }));
            Mock.Arrange(() => _durableOrchestrationContextMock.CallEntityAsync<ChargingStatus>(entityId, "Get"))
                .Returns(Task.FromResult(ChargingStatus.Other));
            Mock.Arrange(() => _durableOrchestrationContextMock.CallActivityAsync("SaveCharge", Arg.IsAny<ChargeState>()));

            await CheckChargeStatus.RunOrchestrator(_durableOrchestrationContextMock, _loggerMock);

            Mock.Assert(() => _durableOrchestrationContextMock.CallActivityAsync("SaveCharge", Arg.IsAny<ChargeState>()), Occurs.Never());
            Mock.Assert(() => _durableOrchestrationContextMock.SignalEntity(entityId, "Set", Arg.IsAny<ChargingStatus>()), Occurs.Once());
        }
    }
}
