using Cycle.Net.Core.Abstract;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{
    public sealed class ClientConnected : ITcpResponse
    {
        public string ClientId { get; }
        public ClientConnected(string clientId) => ClientId = clientId;
    }
}