using Cycle.Net.Core.Abstract;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{
    public sealed class ClientDataReceived : ITcpResponse
    {
        public string ClientId { get; }
        public IByteBuffer Buffer { get; }
        public ClientDataReceived(string clientId, DotNetty.Buffers.IByteBuffer buffer)
        {
            ClientId = clientId;
            Buffer = buffer;
        }
    }
}