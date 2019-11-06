using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OAuthClient
{
    public class OAuthMessageHandler : DelegatingHandler
    {
        private ILogger<OAuthMessageHandler> _logger;
        private HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private AccessToken _token;

        public OAuthMessageHandler(HttpClient httpClient, IConfiguration configuration, ILogger<OAuthMessageHandler> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var authorityUrl = _configuration.GetValue<string>("apiSettings:authority");
            var clientId = _configuration.GetValue<string>("apiSettings:clientId");
            var clientSecret = _configuration.GetValue<string>("apiSettings:clientSecret");
            if (string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("appSettings:clientSecret is not set. See readme.md on how to do it");
            }
            var scopes = _configuration.GetValue<string>("apiSettings:scopes");

            string credentials = $"{clientId}:{clientSecret}";
            string grantType = "client_credentials";
            

            if (_token == null || !_token.IsValid)
            {
                var client = _httpClient;
                
                //Prepare Request Body
                List<KeyValuePair<string, string>> requestData = new List<KeyValuePair<string, string>>();
                requestData.Add(new KeyValuePair<string, string>("grant_type", grantType));
                if (!string.IsNullOrEmpty(scopes))
                {
                    requestData.Add(new KeyValuePair<string, string>("scope", scopes));
                }

                var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, authorityUrl);
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
                httpRequestMessage.Content = new FormUrlEncodedContent(requestData);
                
                //Request Token
                var tokenRequest = await client.SendAsync(httpRequestMessage);
                if (tokenRequest.IsSuccessStatusCode)
                {
                    var response = await tokenRequest.Content.ReadAsStringAsync();
                    _token = JsonConvert.DeserializeObject<AccessToken>(response);
                    _logger.LogDebug(_token.Token);
                }
                else
                {
                    throw new InvalidOperationException(tokenRequest.ReasonPhrase);
                }
               

            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token.Token);
            return await base.SendAsync(request, CancellationToken.None);
        }
    }
}