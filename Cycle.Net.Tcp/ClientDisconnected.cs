using Cycle.Net.Core.Abstract;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{

    public sealed class ClientDisconnected : ITcpResponse
    {
        public string ClientId { get; }
        public ClientDisconnected(string clientId) => ClientId = clientId;
    }

}