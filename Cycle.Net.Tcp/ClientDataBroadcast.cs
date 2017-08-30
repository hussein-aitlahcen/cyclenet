using System.IO;
using Cycle.Net.Core.Abstract;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{
    public sealed class ClientDataBroadcast : ITcpRequest
    {
        public IByteBuffer Buffer { get; }
        public ClientDataBroadcast(IByteBuffer buffer)
        {
            Buffer = buffer;
        }
    }
}