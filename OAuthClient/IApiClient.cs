using System.Threading;
using System.Threading.Tasks;

namespace OAuthClient
{
    public interface IApiClient
    {
        Task<string> GetData(string requestUri, CancellationToken cancellationToken);
    }
}