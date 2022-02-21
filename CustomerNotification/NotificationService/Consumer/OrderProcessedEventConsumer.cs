using EmailService;
using MassTransit;
using Messaging.InterfacesConstants.Events;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
// using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Consumer
{
    public class OrderProcessedEventConsumer : IConsumer<IOrderProcessedEvent>
    {
        private IEmailSender emailSender;
        public OrderProcessedEventConsumer(IEmailSender emailSender) 
        {
            this.emailSender = emailSender;
        }

        public async Task Consume(ConsumeContext<IOrderProcessedEvent> context)
        {
            var rootFolder = AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin"));
            var result = context.Message;
            var facesData = result.Faces;
            if (facesData.Count < 1)
            {
                await Console.Out.WriteLineAsync($"No faces Detected");
            }
            else
            {
                int j = 0;
                foreach (var face in facesData)
                {
                    MemoryStream ms = new MemoryStream(face);

                    var image = Image.Load(ms.ToArray());
                    image.Save(rootFolder + "/Images/face" + j + ".jpg");

                    //var image = Image.FromStream(ms);
                    //image.Save(rootFolder + "/Images/face" + j + ".jpg", ImageFormat.Jpeg);

                    j++;
                }
            }

            // Here we will add the Email Sending Code

            string[] mailAddress = { result.UserEmail };
            var message = new Message(mailAddress,$"your order {result.OrderId}","From FacesAndFaces", facesData);

            await this.emailSender.SendEmailAsync(message);


            await context.Publish<IOrderDispatchedEvent>(
                new
                {
                    OrderId = context.Message.OrderId,
                    DispatchDateTime = DateTime.UtcNow
                });
        }
    }
}
