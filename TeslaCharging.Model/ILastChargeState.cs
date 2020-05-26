using System.Threading.Tasks;

namespace TeslaCharging.Model
{
    public interface ILastChargeState
    {
        void Set(ChargingStatus newStatus);
        Task<ChargingStatus> Get();
    }
}