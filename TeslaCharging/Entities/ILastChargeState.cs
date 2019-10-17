using System.Threading.Tasks;

namespace TeslaCharging.Entities
{
    public interface ILastChargeState
    {
        void Set(ChargingStatus newStatus);
        Task<ChargingStatus> Get();
    }
}