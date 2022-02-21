using Faces.WebMvc.Models;
using Faces.WebMvc.ViewModels;
using MassTransit;
using Messaging.InterfacesConstants.Commands;
using Messaging.InterfacesConstants.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Faces.WebMvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBusControl busControl;

        public HomeController(ILogger<HomeController> logger, IBusControl busControl)
        {
            _logger = logger;
            this.busControl = busControl;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult RegisterOrder() 
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterOrder(OrderViewModel model) 
        {
            MemoryStream memory = new MemoryStream();
            using (var uploadFile = model.File.OpenReadStream()) 
            {
                await uploadFile.CopyToAsync(memory);
            }

            model.ImageData = memory.ToArray();
            model.ImageUrl = model.File.FileName;
            model.OrderId = Guid.NewGuid();

            var sendToUri = new Uri($"{RabbitmqMassTransitConstants.RabbitMquri}{RabbitmqMassTransitConstants.RegisterOrderCommandQueue}");

            var endPoint = await this.busControl.GetSendEndpoint(sendToUri);

            await endPoint.Send<IRegisterOrderCommand>(
                new 
                {
                    OrderId = model.OrderId,
                    PictureUrl = model.ImageUrl,
                    UserEmail = model.UserEmail,
                    ImageData = model.ImageData 
                });

            ViewData["OrderId"] = model.OrderId;

            return View("Thanks");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
