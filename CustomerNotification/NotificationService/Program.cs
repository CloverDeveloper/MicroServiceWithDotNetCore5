using EmailService;
using GreenPipes;
using MassTransit;
using Messaging.InterfacesConstants.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationService.Consumer;
using System;
using System.IO;
using System.Threading.Tasks;

namespace NotificationService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost => 
                {
                    // 設定專案基本路徑
                    configHost.SetBasePath(Directory.GetCurrentDirectory());

                    // 新增 appsettings.json 進主機配置
                    configHost.AddJsonFile("appsettings.json", optional: false);

                    // 使主機可以讀取環境變數
                    configHost.AddEnvironmentVariables();

                    // 可以從 command line 讀取設定檔的值
                    configHost.AddCommandLine(args);
                })
                .ConfigureAppConfiguration((hostContext,config) => 
                {
                    // 此處註冊任何帶有環境變數名稱的 json 檔
                    config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: false);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var emailConfig = hostContext.Configuration.GetSection("EmailConfiguration").Get<EmailConfig>();

                    services.AddSingleton(emailConfig);
                    services.AddScoped<IEmailSender, EmailSender>();

                    services.AddMassTransit(c =>
                    {
                        c.AddConsumer<OrderProcessedEventConsumer>();
                    });

                    services.AddSingleton(provider => Bus.Factory.CreateUsingRabbitMq(
                        cfg =>
                    {
                        var host = cfg.Host("rabbitmq", "/", h => { });

                        cfg.ReceiveEndpoint(RabbitmqMassTransitConstants.NotificationServiceQueue, recConfig =>
                         {
                             recConfig.PrefetchCount = 16;
                             // 無法連接 rebbitmq 伺服器時 Retry 2 次，每次間隔 10 秒
                             recConfig.UseMessageRetry(x => x.Interval(2, TimeSpan.FromSeconds(2)));

                             recConfig.Consumer<OrderProcessedEventConsumer>(provider);
                         });
                        cfg.ConfigureEndpoints(provider);
                    }));

                    services.AddSingleton<IHostedService, BusService>();
                });

            return hostBuilder;
        }
    }
}
