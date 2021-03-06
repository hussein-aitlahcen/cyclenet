using Cycle.Net.Core.Abstract;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Tcp
{
    public interface ITcpResponse : IResponse
    {
        string ClientId { get; }
    }
}