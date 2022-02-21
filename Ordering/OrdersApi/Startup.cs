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

            // ���U MassTransit �������O

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
                        // ����쪺�ֵo�����Ƴ]�w
                        recConfig.PrefetchCount = 16;
                        // �L�k�s�� rebbitmq ���A���� Retry 2 ���A�C�����j 10 ��
                        recConfig.UseMessageRetry(x => x.Interval(2, TimeSpan.FromSeconds(2)));

                        // event bus and consumer class connection
                        recConfig.Consumer<RegisterOrderCommandConsumer>(provider);
                    });

                    cfg.ReceiveEndpoint(RabbitmqMassTransitConstants.OrderDispatchedServiceQueue, recConfig =>
                     {
                         // ����쪺�ֵo�����Ƴ]�w
                         recConfig.PrefetchCount = 16;
                         // �L�k�s�� rebbitmq ���A���� Retry 2 ���A�C�����j 10 ��
                         recConfig.UseMessageRetry(x => x.Interval(2, TimeSpan.FromSeconds(2)));

                         // event bus and consumer class connection
                         recConfig.Consumer<OrderDispatchedEventConsumer>(provider);
                     });

                    cfg.ConfigureEndpoints(provider);
                }));

            services.AddSingleton<IHostedService, BusService>();

            // ���U MassTransit �������O

            // ���U  SignalR
            services.AddSignalR()
                .AddJsonProtocol(optional =>
                {
                    // ���ק��ݩʾm�p�R�W�w�]��
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

            // �A�ȳ����U��ϥ� OrdersContext �ð��� MigrateDB ��k�Ы� DB
            using var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();

            scope.ServiceProvider.GetService<OrdersContext>().MigrateDB();
        }
    }
}
