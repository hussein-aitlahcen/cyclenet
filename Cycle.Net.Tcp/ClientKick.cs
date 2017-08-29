using Cycle.Net.Core.Abstract;

namespace Cycle.Net.Tcp
{

    public sealed class ClientKick : ITcpRequest
    {
        public string ClientId { get; }
        public ClientKick(string clientId) => ClientId = clientId;
    }

}