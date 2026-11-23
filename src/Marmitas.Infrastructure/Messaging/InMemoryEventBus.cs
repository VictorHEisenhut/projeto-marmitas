using System.Threading.Channels;
using Marmitas.Application.Common.Interfaces;

namespace Marmitas.Infrastructure.Messaging;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Channel<object> _channel = Channel.CreateUnbounded<object>();

    public ChannelReader<object> Reader => _channel.Reader;

    public ValueTask PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        return _channel.Writer.WriteAsync(integrationEvent, cancellationToken);
    }
}
