using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OAuthClient;
using Polly;
using Quartz;
using Quartz.Spi;

namespace WindowsService
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
                //typically the following is done part of the host (AzFunction host, Web host, etc).
                //which is why I have pulled it out here.
                .ConfigureHostConfiguration(configuration =>
                {
                }); ;
            var host = Configure(hostBuilder).UseConsoleLifetime().Build();

            //https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.2

            var cancellationToken = new CancellationToken(false);
            using (host)
            {
                await host.StartAsync(cancellationToken);

                var worker = host.Services.GetService<IWorker>();
                await worker.DoWork(cancellationToken);

                await host.WaitForShutdownAsync(cancellationToken);
            }
        }

        /// <summary>
        /// This function is setup to look like the startup of other hosts
        /// eg: FunctionsStartup, which has the public override void Configure(IFunctionsHostBuilder builder) method in which one is expected
        /// to do such startup setup.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="args"></param>
        static IHostBuilder Configure(IHostBuilder hostBuilder, string[] args = null)
        {
            hostBuilder
                .ConfigureAppConfiguration((hostContext, configurations) =>
                {
                    configurations.SetBasePath(Directory.GetCurrentDirectory());
                    if (args != null)
                    {
                        configurations.AddCommandLine(args);
                    }

                    configurations.AddJsonFile("appSettings.json", false);
                    configurations.AddUserSecrets<Program>();
                    configurations.Build();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    services.AddHttpClient<OAuthMessageHandler>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        client.BaseAddress = new Uri(configuration.GetValue<string>("apiSettings:authority"));
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    });

                    services.AddHttpClient();
                    services.AddHttpClient<IApiClient, ApiClient>(client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);
                        client.BaseAddress = new Uri(configuration.GetValue<string>("apiSettings:baseUrl"));
                    })
                        .AddHttpMessageHandler<OAuthMessageHandler>()
                        .AddTransientHttpErrorPolicy(policyBuilder =>
                            policyBuilder.WaitAndRetryAsync(3, sleepDurationProvider => TimeSpan.FromMilliseconds(600)))
                        .AddTransientHttpErrorPolicy(
                            policyBuilder => policyBuilder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

                    services.AddLogging((configure) =>
                    {
                        configure.AddConfiguration(configuration.GetSection("logging"));
                    });

                    services.AddHostedService<HostedService>();

                    services.AddScoped<IWorker, HostedService>();

                    services.AddSingleton<IJobFactory>(provider =>
                    {
                        var jobFactory = new JobFactory(provider);
                        return jobFactory;
                    });
                })
                .ConfigureLogging((hostContext, configLogging) =>
                {
                    configLogging.AddConsole();
                    configLogging.AddDebug();
                });

            return hostBuilder;
        }
    }

    public class JobFactory : IJobFactory
    {
        private readonly IServiceProvider _provider;
        public JobFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var job = _provider.GetRequiredService(bundle.JobDetail.JobType) as IJob;
            if (job == null)
            {
                throw new NotSupportedException($"{bundle.JobDetail.JobType.Name} is not registered");
            }

            return job;
        }

        public void ReturnJob(IJob job)
        {
            if (job is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
