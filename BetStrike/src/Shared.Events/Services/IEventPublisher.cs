using System.Threading;
using System.Threading.Tasks;
using Shared.Events.Models;

namespace Shared.Events.Services;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, EventEnvelope<T> evento, CancellationToken cancellationToken = default);
}
