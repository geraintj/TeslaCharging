using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Moq;
using TeslaCharging.Model;
using Xunit;

namespace TeslaCharging.Tests
{
    public class OrchestratorTests
    {
        private CheckChargeStatus _checkChargeStatus;
        public OrchestratorTests()
        {
            var clientMock = new Mock<IHttpClientFactory>();

        }
        
        public static IEnumerable<object[]> GetNonChargingStatuses()
        {
            yield return new object[] { ChargingStatus.Disconnected };
            yield return new object[] { ChargingStatus.Stopped };
            yield return new object[] { ChargingStatus.Complete };
        }

        [Theory]
        [MemberData(nameof(GetNonChargingStatuses))]
        public void ChargeSaved_LastStatusSet_OnStatusChange(ChargingStatus newStatus)
        {
            // Arrange
            var logger = new Mock<ILogger>();
            var durableOrchestrationContextMock = new TestDurableOrchestrationContext();
            durableOrchestrationContextMock.ReturnChargeState=new ChargeState() { ChargingState = newStatus };
            durableOrchestrationContextMock.ReturnOrchestrationData = new OrchestrationData();
            durableOrchestrationContextMock.ReturnChargingStatus = ChargingStatus.Charging;

            // Act
            //var result = CheckChargeStatus.RunOrchestrator(durableOrchestrationContextMock, logger.Object);

            ArrayList saveChargeApiCalls = new ArrayList();
            durableOrchestrationContextMock.CallActivityCalls.TryGetValue("SaveCharge", out saveChargeApiCalls);
            Assert.Single(saveChargeApiCalls);

            ArrayList setChargingStatusCalls = new ArrayList();
            durableOrchestrationContextMock.SignalEntityCalls.TryGetValue("ChargingStatus_Set", out setChargingStatusCalls);
            Assert.Single(setChargingStatusCalls);
            Assert.Equal(newStatus, setChargingStatusCalls[0]);
        }

        [Fact]
        public void ChargeNotSaved_LastStatusNotSet_OnCharging()
        {
            // Arrange
            var logger = new Mock<ILogger>();
            var durableOrchestrationContextMock = new TestDurableOrchestrationContext();
            durableOrchestrationContextMock.ReturnChargeState = new ChargeState() { ChargingState = ChargingStatus.Charging };
            durableOrchestrationContextMock.ReturnOrchestrationData = new OrchestrationData();
            durableOrchestrationContextMock.ReturnChargingStatus = ChargingStatus.Charging;

            // Act
            //var result = CheckChargeStatus.RunOrchestrator(durableOrchestrationContextMock, logger.Object);

            ArrayList saveChargeApiCalls;
            Assert.False(durableOrchestrationContextMock.CallActivityCalls.TryGetValue("SaveCharge", out saveChargeApiCalls));

            ArrayList setChargingStatusCalls = new ArrayList();
            Assert.False(durableOrchestrationContextMock.SignalEntityCalls.TryGetValue("ChargingStatus_Set", out setChargingStatusCalls));
        }

        [Theory]
        [MemberData(nameof(GetNonChargingStatuses))]
        public void CorrectChargeAmountSaved_OnStatusChange(ChargingStatus newStatus)
        {
            // Arrange
            var logger = new Mock<ILogger>();
            var durableOrchestrationContextMock = new TestDurableOrchestrationContext();
            durableOrchestrationContextMock.ReturnChargeState = new ChargeState() { ChargingState = newStatus, ChargeEnergyAdded = 18.99m };
            durableOrchestrationContextMock.ReturnOrchestrationData = new OrchestrationData();
            durableOrchestrationContextMock.ReturnChargingStatus = ChargingStatus.Charging;

            // Act
            //gularDockervar result = CheckChargeStatus.RunOrchestrator(durableOrchestrationContextMock, logger.Object);

            ArrayList saveChargeApiCalls = new ArrayList();
            durableOrchestrationContextMock.CallActivityCalls.TryGetValue("SaveCharge", out saveChargeApiCalls);
            var charge = saveChargeApiCalls[0] as ChargeState;
            Assert.Equal(18.99m, charge.ChargeEnergyAdded);
        }

    }
}
