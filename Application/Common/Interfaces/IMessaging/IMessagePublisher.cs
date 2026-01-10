using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces.IMessaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync(string message, string queue);
    }
}
