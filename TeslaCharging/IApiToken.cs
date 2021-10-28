using System.Threading.Tasks;
using TeslaCharging.Model;

namespace TeslaCharging
{
    public interface IApiToken
    {
        void Set(TokenResponse response);
        Task<ApiToken> Get();
    }
}