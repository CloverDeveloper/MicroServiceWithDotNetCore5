using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messaging.InterfacesConstants.Constants
{
    public class RabbitmqMassTransitConstants
    {
        public const string RabbitMquri = "rabbitmq://rabbitmq:5672/";

        public const string UserName = "guest";

        public const string Password = "guest";

        public const string RegisterOrderCommandQueue = "register.order.command";

        public const string NotificationServiceQueue = "notification.service.queue";

        public const string OrderDispatchedServiceQueue = "order.dispatch.service.queue";
    }
}
