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

    static IObservable<LogRequest> LogStateSink(IObservable<AppState> appStateStream) =>
        appStateStream
            .Select(state => new LogRequest(
                $"nb of responses: {state.Api.Responses.Count}, nb of clients: {state.Tcp.Clients.Count}"));

    static IObservable<LogRequest> LogTcpSink(IObservable<IDotNettyResponse> tcpStream) =>
        tcpStream
            .OfType<ClientDataReceived>()
            .Select(data => new LogRequest($"client data recv: {data.Buffer.ToString()}"));

    static IObservable<IRequest> Flow(ISource source)
    {
        var apiStream = source.GetDriver(ApiDriver.ID).OfType<ApiResponse>();
        var tcpStream = source.GetDriver(DotNettyDriver.ID).OfType<IDotNettyResponse>();
        var apiStateStream = ApiStateStream(apiStream);
        var tcpStateStream = TcpStateStream(tcpStream);
        var appStateStream = AppStateStream(apiStateStream, tcpStateStream);
        var logSink = Observable.Merge(LogStateSink(appStateStream), LogTcpSink(tcpStream));
        var httpSink = new[]
            {
                RequestPosts,
                RequestUsers,
                RequestComments
            }.ToObservable();
        return Observable.Merge<IRequest>(httpSink, logSink);
    }
}

/* Console output when connecting a browser to http://localhost:5000
nb of responses: 1, nb of clients: 0
nb of responses: 2, nb of clients: 0
nb of responses: 3, nb of clients: 0
nb of responses: 3, nb of clients: 1
nb of responses: 3, nb of clients: 2
client data recv: SimpleLeakAwareByteBuffer(PooledHeapByteBuffer(ridx: 0, widx: 416, cap: 1024))
nb of responses: 3, nb of clients: 3
nb of responses: 3, nb of clients: 2
nb of responses: 3, nb of clients: 1
nb of responses: 3, nb of clients: 0
*/
```
