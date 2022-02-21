using Faces.WebMvc.ResetClients;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Faces.WebMvc.Controllers
{
    public class OrderManagementController : Controller
    {
        private IOrderManagementApi orderManagementApi;
        public OrderManagementController(IOrderManagementApi orderManagementApi) 
        {
            this.orderManagementApi = orderManagementApi;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await this.orderManagementApi.GetOrders();
            foreach(var order in orders) 
            {
                order.ImageString = this.ConvertAndFormatToString(order.ImageData);
            }

            return View(orders);
        }

        [Route("/Details/{orderId}")]
        public async Task<IActionResult> Details(string orderId) 
        {
            var order = await this.orderManagementApi.GetOrderById(Guid.Parse(orderId));

            order.ImageString = this.ConvertAndFormatToString(order.ImageData);

            foreach (var detail in order.OrderDetails) 
            {
                detail.ImageString = this.ConvertAndFormatToString(detail.FaceData);
            }

            return View(order);
        }

        private string ConvertAndFormatToString(byte[] imageData) 
        {
            var imageBase64Data = Convert.ToBase64String(imageData);

            return $"data:image/png;base64, {imageBase64Data}";
        }
    }
}
