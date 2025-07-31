using Registry.Data;
using Registry.Models;

namespace Registry.Services
{
    public interface ITokenService
    {
        void InsertInto(ApiKey key);
        void StorePending(ApiKey key);
        ApiKey? ConfirmPending(string url);

    }
}
