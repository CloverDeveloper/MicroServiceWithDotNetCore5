using MassTransit;
using Messaging.InterfacesConstants.Events;
using Microsoft.AspNetCore.SignalR;
using OrdersApi.Hubs;
using OrdersApi.Models;
using OrdersApi.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersApi.Messages.Consumers
{
    public class OrderDispatchedEventConsumer : IConsumer<IOrderDispatchedEvent>
    {
        private IOrderRepository orderRepo;
        private IHubContext<OrderHub> hubContext;

        public OrderDispatchedEventConsumer(IOrderRepository orderRepo, IHubContext<OrderHub> hubContext) 
        {
            this.orderRepo = orderRepo;
            this.hubContext = hubContext;
        }

        public async Task Consume(ConsumeContext<IOrderDispatchedEvent> context)
        {
            var message = context.Message;

            Guid orderId = message.OrderId;

            await UpdateOrder(orderId);

            await this.hubContext.Clients.All.SendAsync("UpdateOrders", new object[] { "Order Dispatched", orderId });
        }

        private async Task UpdateOrder(Guid orderId)
        {
            var order = await this.orderRepo.GetOrderAsync(orderId);
            if (order != null) 
            {
                order.Status = Status.Sent;

                this.orderRepo.UpdateOrder(order);
            }
        }
    }
}
