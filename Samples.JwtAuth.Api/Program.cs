using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.JwtAuth.Api.Services;

namespace Samples.JwtAuth.Api
{
    internal class Program
    {
        private static async Task Main()
        {
            var hostBuilder = new HostBuilder().UseContentRoot(Directory.GetCurrentDirectory())
                                               .ConfigureHostConfiguration(b => b.AddConfiguration(BuildConfiguration))
                                               .ConfigureAppConfiguration((wc, conf) => conf.AddConfiguration(BuildConfiguration))
                                               .ConfigureWebHost(whb => whb.UseShutdownTimeout(TimeSpan.FromSeconds(15))
                                                                           .UseUrls("http://*:8084")
                                                                           .UseKestrel()
                                                                           .UseStartup<ApiStartup>())
                                               .ConfigureLogging((x, b) => b.AddConfiguration(x.Configuration.GetSection("Logging"))
                                                                            .AddConsole()
                                                                            .AddDebug()
                                                                            .SetMinimumLevel(LogLevel.Debug))
                                               .UseConsoleLifetime();

            var host = hostBuilder.Build();

            var authDynProvider = host.Services.GetRequiredService<IAuthDynProvider>();

            await authDynProvider.InitSchemaAsync();

            await host.RunAsync();
        }

        internal static IConfiguration BuildConfiguration { get; } = new ConfigurationBuilder().AddJsonFile("appsettings.json", true)
                                                                                               .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local"}.json", true, true)
                                                                                               .AddEnvironmentVariables("SMP_")
                                                                                               .Build();
    }
}
