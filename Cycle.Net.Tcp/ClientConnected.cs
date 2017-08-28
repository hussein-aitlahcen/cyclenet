using Cycle.Net.Core.Abstract;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{
    public sealed class ClientConnected : ITcpResponse
    {
        public IChannelId ClientId { get; }
        public ClientConnected(IChannelId clientId) => ClientId = clientId;
    }
}