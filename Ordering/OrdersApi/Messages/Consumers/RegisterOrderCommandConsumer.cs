using MassTransit;
using Messaging.InterfacesConstants.Commands;
using Messaging.InterfacesConstants.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OrdersApi.Hubs;
using OrdersApi.Models;
using OrdersApi.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace OrdersApi.Messages.Consumers
{
    public class RegisterOrderCommandConsumer : IConsumer<IRegisterOrderCommand>
    {
        private IOrderRepository orderRepo;
        private IHttpClientFactory httpClientFactory;
        private IHubContext<OrderHub> hubContext;
        private IOptions<OrderSettings> settings;

        public RegisterOrderCommandConsumer(
            IOrderRepository orderRepo,
            IHttpClientFactory httpClientFactory,
            IHubContext<OrderHub> hubContext,
            IOptions<OrderSettings> settings) 
        {
            this.orderRepo = orderRepo;
            this.httpClientFactory = httpClientFactory;
            this.hubContext = hubContext;
            this.settings = settings;
        }

        public async Task Consume(ConsumeContext<IRegisterOrderCommand> context)
        {
            var result = context.Message;
            if (result.OrderId != Guid.Empty && result.PictureUrl != null && result.UserEmail != null && result.ImageData != null) 
            {
                SaveOrder(result);

                await this.hubContext.Clients.All.SendAsync("UpdateOrders", "New Order Created", result.OrderId);

                var client = this.httpClientFactory.CreateClient();
                Tuple<List<byte[]>, Guid> orderDetailData = await GetFacesFromFacesApiAsync(client, result.ImageData, result.OrderId);

                List<byte[]> faces = orderDetailData.Item1;
                Guid orderId = orderDetailData.Item2;
                await SaveOrderDetails(orderId, faces);

                await this.hubContext.Clients.All.SendAsync("UpdateOrders", "Order processed", result.OrderId);

                await context.Publish<IOrderProcessedEvent>(new
                {
                    OrderId = orderId,
                    UserEmail = result.UserEmail,
                    Faces = faces,
                    PictureUrl = result.PictureUrl
                });
            }
        }

        private async Task SaveOrderDetails(Guid orderId, List<byte[]> faces)
        {
            var order = await this.orderRepo.GetOrderAsync(orderId);
            if (order != null) 
            {
                order.Status = Status.Processed;
                OrderDetail detail;
                foreach (var face in faces) 
                {
                    detail = new OrderDetail();
                    detail.OrderId = orderId;
                    detail.FaceData = face;

                    order.OrderDetails.Add(detail);
                }

                this.orderRepo.UpdateOrder(order);
            }
        }

        private async Task<Tuple<List<byte[]>, Guid>> GetFacesFromFacesApiAsync(HttpClient client, byte[] imageData, Guid orderId)
        {
            var byteContent = new ByteArrayContent(imageData);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var url = $"{this.settings.Value.FacesApiurl}/api/Faces?orderId=";
            Tuple<List<byte[]>, Guid> orderDetailData = null;
            using (var response = await client.PostAsync($"{url}{orderId}", byteContent)) 
            {
                var apiResponse = await response.Content.ReadAsStringAsync();
                orderDetailData = JsonConvert.DeserializeObject<Tuple<List<byte[]>, Guid>>(apiResponse);
            }

            return orderDetailData;
        }

        private void SaveOrder(IRegisterOrderCommand result)
        {
            var order = new Order();
            order.OrderId = result.OrderId;
            order.PictureUrl = result.PictureUrl;
            order.UserEmail = result.UserEmail;
            order.ImageData = result.ImageData;
            order.Status = Status.Registered;

            this.orderRepo.RegisterOrder(order);
        }
    }
}
