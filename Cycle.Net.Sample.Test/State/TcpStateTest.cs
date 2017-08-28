using Xunit;
using System;
using System.Reactive;
using System.Reactive.Linq;
using Cycle.Net.Sample.State;
using System.Collections.Immutable;
using Cycle.Net.Http;
using DotNetty.Transport.Channels;
using Cycle.Net.Tcp;

namespace Cycle.Net.Sample.Test.State
{
    public sealed class FakeChannelId : IChannelId
    {
        public string Id { get; }

        public FakeChannelId(string id) => Id = id;

        public string AsLongText() => Id;

        public string AsShortText() => Id;

        public int CompareTo(IChannelId other)
        {
            switch (other)
            {
                case FakeChannelId fkChannelId: return fkChannelId.Id.CompareTo(Id);
                default: return -1;
            }
        }
    }

    public sealed class TcpStateTest
    {
        public static IChannelId FakeChannelId = new FakeChannelId("fake-id");
        public static ITcpResponse FakeClientConnected = new ClientConnected(FakeChannelId);
        public static ITcpResponse FakeClientDisconnected = new ClientDisconnected(FakeChannelId);

        [Fact]
        public void ReducerAddConnectedClients()
        {
            var state = TcpState.Reducer(TcpState.Initial, FakeClientConnected);
            Assert.True(state.Clients.Contains(FakeChannelId));
        }

        [Fact]
        public void ReducerRemoveDisconnectedClients()
        {
            var state = TcpState.Reducer(TcpState.Initial, FakeClientConnected);
            var nextState = TcpState.Reducer(state, FakeClientDisconnected);
            Assert.False(nextState.Clients.Contains(FakeChannelId));
        }
    }
}