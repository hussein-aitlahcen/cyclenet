using Cycle.Net.Core.Abstract;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{

    public sealed class ClientDisconnected : ITcpResponse
    {
        public IChannelId ClientId { get; }
        public ClientDisconnected(IChannelId clientId) => ClientId = clientId;
    }

}