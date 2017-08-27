using System;
using Cycle.Net.Run;
using Cycle.Net.Run.Abstract;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Collections.Immutable;
using System.Collections.Generic;
using DotNetty.Transport.Channels;
using Cycle.Net.Sample.Driver;

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
            new CycleNet().Run(Flow, new Drivers()
            {
                [LogDriver.ID] = LogDriver.Create,
                [ApiDriver.ID] = ApiDriver.Create(scheduler),
                [DotNettyDriver.ID] = DotNettyDriver.Create(scheduler, pipe => { }, 5000).Result
            });
            Console.Read();
        }

        static ApiRequest RequestPosts = new ApiRequest("posts", "https://jsonplaceholder.typicode.com/posts");
        static ApiRequest RequestUsers = new ApiRequest("users", "https://jsonplaceholder.typicode.com/users");
        static ApiRequest RequestComments = new ApiRequest("comments", "https://jsonplaceholder.typicode.com/comments");

        class ApiState
        {
            public static ApiState Initial = new ApiState(ImmutableList.Create<ApiResponse>());
            public ImmutableList<ApiResponse> Responses { get; }
            public ApiState(ImmutableList<ApiResponse> responses) => Responses = responses;
        }

        class TcpState
        {
            public static TcpState Initial = new TcpState(ImmutableHashSet.Create<IChannel>());
            public ImmutableHashSet<IChannel> Clients { get; }
            public TcpState(ImmutableHashSet<IChannel> clients) => Clients = clients;
        }

        class AppState
        {
            public ApiState Api { get; }
            public TcpState Tcp { get; }
            public AppState(ApiState apiState, TcpState tcpState)
            {
                Api = apiState;
                Tcp = tcpState;
            }
        }

        static IObservable<ApiState> ApiStateStream(IObservable<ApiResponse> apiStream) =>
            apiStream
                .Scan(ApiState.Initial, (state, response) => new ApiState(state.Responses.Add(response)));

        static IObservable<TcpState> TcpStateStream(IObservable<IDotNettyResponse> tcpStream) =>
            Observable.Merge<IDotNettyResponse>(
                tcpStream.OfType<ClientConnected>(),
                tcpStream.OfType<ClientDisconnected>()
            ).Scan(TcpState.Initial, (state, response) =>
                {
                    switch (response)
                    {
                        case ClientConnected connected:
                            return new TcpState(state.Clients.Add(connected.Client));
                        case ClientDisconnected disconnected:
                            return new TcpState(state.Clients.Remove(disconnected.Client));
                    }
                    return state;
                });

        static IObservable<AppState> AppStateStream(IObservable<ApiState> apiStateStream, IObservable<TcpState> tcpStateStream) =>
            Observable.CombineLatest(
                apiStateStream.StartWith(ApiState.Initial),
                tcpStateStream.StartWith(TcpState.Initial),
                (apiState, tcpState) => new AppState(apiState, tcpState));

        static IObservable<LogRequest> LogSink(IObservable<AppState> appStateStream) =>
            appStateStream
                .Select(state => new LogRequest(
                    $"nb of responses: {state.Api.Responses.Count}, nb of clients: {state.Tcp.Clients.Count}"));

        static IObservable<IRequest> Flow(ISource source)
        {
            var apiStream = source.GetDriver(ApiDriver.ID).OfType<ApiResponse>();
            var tcpStream = source.GetDriver(DotNettyDriver.ID).OfType<IDotNettyResponse>();
            var apiStateStream = ApiStateStream(apiStream);
            var tcpStateStream = TcpStateStream(tcpStream);
            var appStateStream = AppStateStream(apiStateStream, tcpStateStream);
            var logSink = LogSink(appStateStream);
            var httpSink = new[]
                {
                    RequestPosts,
                    RequestUsers,
                    RequestComments
                }.ToObservable();
            return Observable.Merge<IRequest>(httpSink, logSink);
        }
    }
}
