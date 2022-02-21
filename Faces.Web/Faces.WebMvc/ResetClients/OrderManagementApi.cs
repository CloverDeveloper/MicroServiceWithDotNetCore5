using Faces.WebMvc.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Faces.WebMvc.ResetClients
{
    public class OrderManagementApi : IOrderManagementApi
    {
        private IOrderManagementApi _restClient;
        private IOptions<Appsettings> settings;
        public OrderManagementApi(/*IConfiguration config,*/HttpClient client, IOptions<Appsettings> settings) 
        {
            // var apiHostAndPort = config.GetSection("ApiServiceLocations").GetValue<string>("OrdersApiLocation");
            var apiHostAndPort = settings.Value.OrdersApiUrl;

            client.BaseAddress = new Uri($"{apiHostAndPort}/api");
            this._restClient = RestService.For<IOrderManagementApi>(client);
            this.settings = settings;
        }

        public async Task<OrderViewModel> GetOrderById(Guid orderId)
        {
            try 
            {
                return await this._restClient.GetOrderById(orderId);
            }
            catch(ApiException ex) 
            {
                if(ex.StatusCode == HttpStatusCode.NotFound) 
                {
                    return null;
                }

                throw;
            }
        }

        public async Task<List<OrderViewModel>> GetOrders()
        {
            return await this._restClient.GetOrders();
        }
    }
}
