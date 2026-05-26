using System.Threading.Tasks;
using Shared.Messaging.Models;

namespace Shared.Messaging.Services;

public interface IRabbitMqPublisher
{
    void Publish<T>(string routingKey, MessageEnvelope<T> message);
}
