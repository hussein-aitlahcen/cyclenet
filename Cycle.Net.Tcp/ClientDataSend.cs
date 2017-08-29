using System.IO;
using Cycle.Net.Core.Abstract;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{
    public sealed class ClientDataSend : ITcpRequest
    {

        public string ClientId { get; }
        public IByteBuffer Buffer { get; }
        public ClientDataSend(string clientId, IByteBuffer buffer)
        {
            ClientId = clientId;
            Buffer = buffer;
        }
    }
}