using Cycle.Net.Core.Abstract;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{
    public sealed class ClientDataSend : ITcpRequest
    {

        public IChannelId ClientId { get; }
        public IByteBuffer Buffer { get; }
        public ClientDataSend(IChannelId clientId, IByteBuffer buffer)
        {
            ClientId = clientId;
            Buffer = buffer;
        }
    }
}