using Microsoft.EntityFrameworkCore;
using OrdersApi.Models;
using OrdersApi.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersApi
{
    public class OrderRepository : IOrderRepository
    {
        private OrdersContext context;

        public OrderRepository(OrdersContext context) 
        {
            this.context = context;
        }

        public Order GetOrder(Guid id)
        {
            return this.context.Orders
                .Include("OrderDetails")
                .FirstOrDefault(c => c.OrderId == id);
        }

        public Task<Order> GetOrderAsync(Guid id)
        {
            return this.context.Orders
                .Include("OrderDetails")
                .FirstOrDefaultAsync(c => c.OrderId == id);
        }

        public async Task<IEnumerable<Order>> GetOrderAsync()
        {
            return await this.context.Orders.ToListAsync();
        }

        public Task RegisterOrder(Order order)
        {
            this.context.Orders.Add(order);
            this.context.SaveChanges();

            return Task.FromResult(true);
        }

        public void UpdateOrder(Order order)
        {
            this.context.Entry(order).State = EntityState.Modified;
            this.context.SaveChanges();
        }
    }
}
