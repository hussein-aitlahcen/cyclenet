<h1 align="center">Cycle.Net</h1>

<div align="center">
  <strong>A functional and reactive framework for predictable code</strong>
  <i>Heavily based on <a href="https://github.com/cyclejs/cyclejs">cycle.js</a></i>
</div>

## Introduction
This project aim to port the Cycle.js pattern to .Net

## Contributions
Feel free to fork and make PR, any help will be appreciated !

## Sample

A basic implementation of the ApiDriver, DotNettyDriver and LogDriver are given in the [Sample](https://github.com/hussein-aitlahcen/cyclenet/tree/master/Cycle.Net.Sample) directory and here is an exemple of pure dataflow, fully functionnal, immutable and side-effects free (function **Flow**)

This example retrieve data from three apis and accept tcp connections on port 5000 Echoing back any data received.
```csharp
using Driver = IObservable<IResponse>;
using DriverMaker = Func<IObservable<IRequest>, IObservable<IResponse>>;
using Drivers = Dictionary<string, Func<IObservable<IRequest>, IObservable<IResponse>>>;

class Program
{
    static void Main(string[] args)
    {
        var scheduler = new EventLoopScheduler();
        new CycleNet().Run(Flow, scheduler, new Drivers()
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
        public static TcpState Initial = new TcpState(ImmutableHashSet.Create<IChannelId>());
        public ImmutableHashSet<IChannelId> Clients { get; }
        public TcpState(ImmutableHashSet<IChannelId> clients) => Clients = clients;
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
                        return new TcpState(state.Clients.Add(connected.ClientId));
                    case ClientDisconnected disconnected:
                        return new TcpState(state.Clients.Remove(disconnected.ClientId));
                }
                return state;
            });

    static IObservable<AppState> AppStateStream(IObservable<ApiState> apiStateStream, IObservable<TcpState> tcpStateStream) =>
        Observable.CombineLatest(
            apiStateStream,
            tcpStateStream,
            (apiState, tcpState) => new AppState(apiState, tcpState));

    static IObservable<LogRequest> LogStateSink(IObservable<AppState> appStateStream) =>
        appStateStream
            .Select(state => new LogRequest(
                $"nb of responses: {state.Api.Responses.Count}, nb of clients: {state.Tcp.Clients.Count}"));

    static IObservable<LogRequest> LogTcpSink(IObservable<IDotNettyResponse> tcpStream) =>
        tcpStream
            .OfType<ClientDataReceived>()
            .Select(data => new LogRequest($"client data recv: {data.Buffer.ToString()}"));

    static IObservable<IDotNettyRequest> EchoTcpSink(IObservable<IDotNettyResponse> tcpStream) =>
        tcpStream
            .OfType<ClientDataReceived>()
            .Select(data => new ClientDataSend(data.ClientId, data.Buffer));

    static IObservable<IRequest> Flow(ISource source)
    {
        var apiStream = source.GetDriver(ApiDriver.ID).OfType<ApiResponse>();
        var tcpStream = source.GetDriver(DotNettyDriver.ID).OfType<IDotNettyResponse>();
        var apiStateStream = ApiStateStream(apiStream);
        var tcpStateStream = TcpStateStream(tcpStream);
        var appStateStream = AppStateStream(
            apiStateStream.StartWith(ApiState.Initial),
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
```
