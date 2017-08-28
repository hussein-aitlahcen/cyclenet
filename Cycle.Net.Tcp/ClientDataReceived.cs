using Cycle.Net.Core.Abstract;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{
    public sealed class ClientDataReceived : ITcpResponse
    {
        public IChannelId ClientId { get; }
        public IByteBuffer Buffer { get; }
        public ClientDataReceived(IChannelId clientId, DotNetty.Buffers.IByteBuffer buffer)
        {
            ClientId = clientId;
            Buffer = buffer;
        }
    }
}