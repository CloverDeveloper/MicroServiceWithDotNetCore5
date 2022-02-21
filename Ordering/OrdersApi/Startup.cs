using GreenPipes;
using MassTransit;
using Messaging.InterfacesConstants.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrdersApi.Hubs;
using OrdersApi.Messages.Consumers;
using OrdersApi.Persistence;
using OrdersApi.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<OrdersContext>(options => options.UseSqlServer(
                    //Configuration.GetConnectionString("OrdersContextConnection")
                    Configuration["OrdersContextConnection"]
                ));

            services.AddHttpClient();
            services.Configure<OrderSettings>(this.Configuration);

            // 註冊 MassTransit 相關類別

            services.AddMassTransit(c => 
            {
                c.AddConsumer<RegisterOrderCommandConsumer>();
                c.AddConsumer<OrderDispatchedEventConsumer>();
            });
            services.AddSingleton(provider => Bus.Factory.CreateUsingRabbitMq(
                cfg =>
                {
                    var host = cfg.Host("rabbitmq", "/", h => { });
                    cfg.ReceiveEndpoint(RabbitmqMassTransitConstants.RegisterOrderCommandQueue, recConfig =>
                    {
                        // 限制收到的併發消息數設定
                        recConfig.PrefetchCount = 16;
                        // 無法連接 rebbitmq 伺服器時 Retry 2 次，每次間隔 10 秒
                        recConfig.UseMessageRetry(x => x.Interval(2, TimeSpan.FromSeconds(2)));

                        // event bus and consumer class connection
                        recConfig.Consumer<RegisterOrderCommandConsumer>(provider);
                    });

                    cfg.ReceiveEndpoint(RabbitmqMassTransitConstants.OrderDispatchedServiceQueue, recConfig =>
                     {
                         // 限制收到的併發消息數設定
                         recConfig.PrefetchCount = 16;
                         // 無法連接 rebbitmq 伺服器時 Retry 2 次，每次間隔 10 秒
                         recConfig.UseMessageRetry(x => x.Interval(2, TimeSpan.FromSeconds(2)));

                         // event bus and consumer class connection
                         recConfig.Consumer<OrderDispatchedEventConsumer>(provider);
                     });

                    cfg.ConfigureEndpoints(provider);
                }));

            services.AddSingleton<IHostedService, BusService>();

            // 註冊 MassTransit 相關類別

            // 註冊  SignalR
            services.AddSignalR()
                .AddJsonProtocol(optional =>
                {
                    // 不修改屬性駝峰命名預設值
                    optional.PayloadSerializerOptions.PropertyNamingPolicy = null;
                });

            services.AddTransient<IOrderRepository, OrderRepository>();
            services.AddControllers();
            services.AddCors((options) => 
            {
                options.AddPolicy("CorsPolicy", builder =>
                 {
                     builder.AllowAnyHeader();
                     builder.AllowAnyMethod();
                     builder.SetIsOriginAllowed((host) => true);
                     builder.AllowCredentials();
                 });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors("CorsPolicy");
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<OrderHub>("/orderhub");
            });

            // 服務都註冊後使用 OrdersContext 並執行 MigrateDB 方法創建 DB
            using var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();

            scope.ServiceProvider.GetService<OrdersContext>().MigrateDB();
        }
    }
}
