using Cycle.Net.Core.Abstract;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{
    public interface ITcpRequest : IRequest
    {
        IChannelId ClientId { get; }
    }
}