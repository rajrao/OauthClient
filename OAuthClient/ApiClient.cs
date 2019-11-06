using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using IHttpClientFactory = System.Net.Http.IHttpClientFactory;

namespace OAuthClient
{
    public class ApiClient : IApiClient
    {
        private HttpClient _httpClient;
        private ILogger<IApiClient> _logger;

        public ApiClient(HttpClient httpClient, ILogger<IApiClient> logger)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<string> GetData(string requestUri, CancellationToken cancellationToken)
        {
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, requestUri);

            HttpResponseMessage responseMsg;
            responseMsg = await _httpClient.SendAsync(req,HttpCompletionOption.ResponseContentRead, cancellationToken);
            

            if (responseMsg.IsSuccessStatusCode)
            {
                return await responseMsg.Content.ReadAsStringAsync();
            }
            else
            {
                return null;
            }
        }
    }
}