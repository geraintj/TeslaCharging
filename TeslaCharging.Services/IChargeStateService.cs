using System.Collections.Generic;
using System.Threading.Tasks;
using TeslaCharging.Model;

namespace TeslaCharging.Services
{
    public interface IChargeStateService
    {
        Task<List<ChargeState>> GetSavedCharges();
    }
}