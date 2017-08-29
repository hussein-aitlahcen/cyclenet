using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Tcp
{

    public sealed class ClientDisconnected : ITcpResponse
    {
        public string ClientId { get; }
        public ClientDisconnected(string clientId) => ClientId = clientId;
    }

}