using System;
using System.Collections.Immutable;
using System.Threading;

namespace Cycle.Net.Http.State
{
    internal sealed class RunningRequest : IDisposable
    {
        public IHttpRequest Origin { get; }
        public CancellationTokenSource Token { get; }

        public RunningRequest(IHttpRequest origin, CancellationTokenSource tokenSource)
        {
            Origin = origin;
            Token = tokenSource;
        }

        public void Dispose() => Token.Dispose();
    }
    internal sealed class HttpState
    {
        public ImmutableList<RunningRequest> RunningIds { get; }
        public HttpState(ImmutableList<RunningRequest> runningIds) => RunningIds = runningIds;
    }
}