using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OAuthClient
{
    public interface IWorker
    {
        Task DoWork(CancellationToken cancellationToken);
    }
    public class HostedService : IHostedService, IWorker
    {
        private IApiClient _apiClient;
        private ILogger<HostedService> _logger;
        private IHost _host;
        private IConfiguration _configuration;

        public HostedService(IHost host, IConfiguration configuration, IApiClient apiClient, ILogger<HostedService> logger)
        {
            _configuration = configuration;
            _host = host;
            _logger = logger;
            _logger.LogDebug("HostedService");

            _apiClient = apiClient;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                _logger.LogDebug("StartAsync");
            }, cancellationToken);
        }

        public async Task DoWork(CancellationToken cancellationToken)
        {
            var url = _configuration.GetValue<string>("apiSettings:testUrl");
            var data = await _apiClient.GetData(url, cancellationToken);

            _logger.LogDebug(data);

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                _logger.LogDebug("StopAsync");
            }, cancellationToken);
        }
    }
}