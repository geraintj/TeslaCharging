using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TeslaCharging.Model;

namespace TeslaCharging.Services
{
    class ChargeStateService : IChargeStateService
    {
        public async Task<List<ChargeState>> GetSavedCharges()
        {
            throw new NotImplementedException();
        }
    }
}
