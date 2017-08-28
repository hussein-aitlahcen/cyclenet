using System;
using System.Reactive.Linq;
using Cycle.Net.Core;
using Cycle.Net.Http;
using Cycle.Net.Tcp;

namespace Cycle.Net.Sample
{
    public class AppSource : SimpleSource
    {
        public IObservable<IHttpResponse> Http => GetDriver(HttpDriver.ID).OfType<IHttpResponse>();
        public IObservable<ITcpResponse> Tcp => GetDriver(TcpDriver.ID).OfType<ITcpResponse>();
    }
}