using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrdersApi.Models;
using OrdersApi.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrdersApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private IOrderRepository orderRepo;

        public OrdersController(IOrderRepository orderRepo) 
        {
            this.orderRepo = orderRepo;
        }

        [HttpGet]
        [Route("[action]")]
        public IActionResult GetTest()
        {
            return Ok("XDD");
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync() 
        {
            var datas = await this.orderRepo.GetOrderAsync();
            foreach (var order in datas)
            {
                order.OrderStatus = Enum.GetName(typeof(Status), order.Status);
            }
            return Ok(datas);
        }

        [HttpGet]
        [Route("{orderId}",Name = "GetByOrderId")]
        public async Task<IActionResult> GetByOrderId(string orderId)
        {
            var data = await this.orderRepo.GetOrderAsync(Guid.Parse(orderId));
            if (data == null) return NotFound();

            data.OrderStatus = Enum.GetName(typeof(Status), data.Status);

            return Ok(data);
        }
    }
}
