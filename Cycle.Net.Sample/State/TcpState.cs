using System.Collections.Immutable;
using Cycle.Net.Tcp;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Sample.State
{
    public class TcpState
    {
        public static TcpState Initial = new TcpState(ImmutableHashSet.Create<IChannelId>());
        public ImmutableHashSet<IChannelId> Clients { get; }
        public TcpState(ImmutableHashSet<IChannelId> clients) => Clients = clients;

        public static TcpState Reducer(TcpState previous, ITcpResponse response)
        {
            switch (response)
            {
                case ClientConnected connected:
                    return new TcpState(previous.Clients.Add(connected.ClientId));
                case ClientDisconnected disconnected:
                    return new TcpState(previous.Clients.Remove(disconnected.ClientId));
                default:
                    return previous;
            }
        }
    }
}