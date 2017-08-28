using System;
using Cycle.Net.Core;
using Cycle.Net.Core.Abstract;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Collections.Immutable;
using System.Collections.Generic;
using DotNetty.Transport.Channels;
using Cycle.Net.Run;
using Cycle.Net.Http;
using Cycle.Net.Tcp;
using Cycle.Net.Log;

namespace Cycle.Net.Sample
{
    using Driver = IObservable<IResponse>;
    using DriverMaker = Func<IObservable<IRequest>, IObservable<IResponse>>;
    using Drivers = Dictionary<string, Func<IObservable<IRequest>, IObservable<IResponse>>>;

    class Program
    {
        static void Main(string[] args)
        {
            var scheduler = new EventLoopScheduler();
            new CycleNet().Run<AppSource>(Flow, scheduler, new Drivers()
            {
                [LogDriver.ID] = LogDriver.Create,
                [HttpDriver.ID] = HttpDriver.Create(scheduler),
                [TcpDriver.ID] = TcpDriver.Create(scheduler, pipe => { }, 5000).Result
            });
            Console.Read();
        }

        static HttpRequest RequestPosts = new HttpRequest("posts", "https://jsonplaceholder.typicode.com/posts");
        static HttpRequest RequestUsers = new HttpRequest("users", "https://jsonplaceholder.typicode.com/users");
        static HttpRequest RequestComments = new HttpRequest("comments", "https://jsonplaceholder.typicode.com/comments");

        class HttpState
        {
            public static HttpState Initial = new HttpState(ImmutableList.Create<HttpResponse>());
            public ImmutableList<HttpResponse> Responses { get; }
            public HttpState(ImmutableList<HttpResponse> responses) => Responses = responses;
        }

        class TcpState
        {
            public static TcpState Initial = new TcpState(ImmutableHashSet.Create<IChannelId>());
            public ImmutableHashSet<IChannelId> Clients { get; }
            public TcpState(ImmutableHashSet<IChannelId> clients) => Clients = clients;
        }

        class AppState
        {
            public HttpState Http { get; }
            public TcpState Tcp { get; }
            public AppState(HttpState httpState, TcpState tcpState)
            {
                Http = httpState;
                Tcp = tcpState;
            }
        }

        class AppSource : SimpleSource
        {
            public IObservable<HttpResponse> Http => GetDriver(HttpDriver.ID).OfType<HttpResponse>();
            public IObservable<ITcpResponse> Tcp => GetDriver(TcpDriver.ID).OfType<ITcpResponse>();
        }

        static IObservable<HttpState> HttpStateStream(IObservable<HttpResponse> HttpStream) =>
            HttpStream
                .Scan(HttpState.Initial, (state, response) => new HttpState(state.Responses.Add(response)));

        static IObservable<TcpState> TcpStateStream(IObservable<ITcpResponse> tcpStream) =>
            Observable.Merge<ITcpResponse>(
                tcpStream.OfType<ClientConnected>(),
                tcpStream.OfType<ClientDisconnected>()
            ).Scan(TcpState.Initial, (state, response) =>
                {
                    switch (response)
                    {
                        case ClientConnected connected:
                            return new TcpState(state.Clients.Add(connected.ClientId));
                        case ClientDisconnected disconnected:
                            return new TcpState(state.Clients.Remove(disconnected.ClientId));
                    }
                    return state;
                });

        static IObservable<AppState> AppStateStream(IObservable<HttpState> httpStateStream, IObservable<TcpState> tcpStateStream) =>
            Observable.CombineLatest(
                httpStateStream,
                tcpStateStream,
                (httpState, tcpState) => new AppState(httpState, tcpState));

        static IObservable<ILogRequest> LogStateSink(IObservable<AppState> appStateStream) =>
            appStateStream
                .Select(state => new LogRequest(
                    $"nb of responses: {state.Http.Responses.Count}, nb of clients: {state.Tcp.Clients.Count}"));

        static IObservable<ILogRequest> LogTcpSink(IObservable<ITcpResponse> tcpStream) =>
            tcpStream
                .OfType<ClientDataReceived>()
                .Select(data => new LogRequest($"client data recv: {data.Buffer.ToString()}"));

        static IObservable<ITcpRequest> EchoTcpSink(IObservable<ITcpResponse> tcpStream) =>
            tcpStream
                .OfType<ClientDataReceived>()
                .Select(data => new ClientDataSend(data.ClientId, data.Buffer));

        static IObservable<IRequest> Flow(AppSource source)
        {
            var httpStream = source.Http;
            var tcpStream = source.Tcp;
            var httpStateStream = HttpStateStream(httpStream);
            var tcpStateStream = TcpStateStream(tcpStream);
            var appStateStream = AppStateStream(
                httpStateStream.StartWith(HttpState.Initial),
                tcpStateStream.StartWith(TcpState.Initial));
            var logSink = Observable.Merge(LogStateSink(appStateStream), LogTcpSink(tcpStream));
            var tcpSink = EchoTcpSink(tcpStream);
            var httpSink = new[]
                {
                    RequestPosts,
                    RequestUsers,
                    RequestComments
                }.ToObservable();
            return Observable.Merge<IRequest>(httpSink, tcpSink, logSink);
        }
    }
}
