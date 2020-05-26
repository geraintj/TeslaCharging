using System.Threading.Tasks;
using TeslaCharging.Model;

namespace TeslaCharging
{
    public interface ILastChargeState
    {
        void Set(ChargingStatus newStatus);
        Task<ChargingStatus> Get();
    }
}