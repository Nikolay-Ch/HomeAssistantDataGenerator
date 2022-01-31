using HomeAssistantDataGenerator.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HomeAssistantDataGenerator
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    var env = ctx.HostingEnvironment;

                    cfg.AddJsonFile("/config/appsettings.json", true, false)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, false)
                        .AddEnvironmentVariables();
                })
                .ConfigureLogging((ctx, logging) =>
                {
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<MqttConfiguration>(hostContext.Configuration.GetSection("MqttConfiguration"));
                    services.Configure<ProgramConfiguration>(hostContext.Configuration.GetSection("ProgramConfiguration"));
                    services.AddHostedService<WorkerDataGenerator>();
                });
    }
}
