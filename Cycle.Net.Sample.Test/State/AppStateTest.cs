using Xunit;
using System;
using System.Reactive;
using System.Reactive.Linq;
using Cycle.Net.Sample.State;
using System.Collections.Immutable;
using Cycle.Net.Http;
using DotNetty.Transport.Channels;

namespace Cycle.Net.Sample.Test.State
{
    public sealed class AppStateTest
    {
        [Fact]
        public void CombineIsOk()
        {
            var httpState = new HttpState(ImmutableList.Create<IHttpResponse>());
            var tcpState = new TcpState(ImmutableHashSet.Create<IChannelId>());
            var appState = AppState.Combine(httpState, tcpState);
            Assert.True(appState.Http == httpState && appState.Tcp == tcpState);
        }
    }
}