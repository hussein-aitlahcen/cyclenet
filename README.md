<h1 align="center">Cycle.Net</h1>

<div align="center">
  <strong>A functional and reactive framework for predictable code</strong>
  <i>Heavily based on <a href="https://github.com/cyclejs/cyclejs">cycle.js</a></i>
</div>

## Introduction
This project aim to port the Cycle.js pattern to .Net

A basic implementation of the HttpDriver, TcpDriver and LogDriver are given.

## Contributions
Feel free to fork and make PR, any help will be appreciated !

## Sample
 The [Sample](https://github.com/hussein-aitlahcen/cyclenet/tree/master/Cycle.Net.Sample) is an exemple of pure dataflow, fully functionnal, immutable and side-effects free (function **Flow**)

This example send three requests to fetch api data, accept connections ont localhost:5000 and echo back total bytes received.
```csharp
using Driver = IObservable<IResponse>;
using DriverMaker = Func<IObservable<IRequest>, IObservable<IResponse>>;
using Drivers = Dictionary<string, Func<IObservable<IRequest>, IObservable<IResponse>>>;

public static class Helper
{
    public static Func<T, T> Identity<T>() => x => x;

    public static Func<TIn, TOut> Compose<TIn, TTransform, TOut>(
        this Func<TIn, TTransform> f, Func<TTransform, TOut> g) => x => g(f(x));
}

public sealed class TcpClientState
{
    public string Id { get; }
    public int BytesReceived { get; }

    public TcpClientState(string id) : this(id, 0)
    {
    }

    public TcpClientState(string id, int bytesReceived)
    {
        Id = id;
        BytesReceived = bytesReceived;
    }

    public static Func<TcpClientState, TcpClientState> Reducers(Func<TcpClientState, TcpClientState> fun, ITcpResponse message)
    {
        switch (message)
        {
            case ClientDataReceived received:
                return fun.Compose(previous => new TcpClientState(previous.Id, previous.BytesReceived + received.Buffer.ReadableBytes));
            default:
                return fun;
        }
    }
}

public class Program
{
    private static HttpRequest RequestPosts = new HttpRequest("posts", "https://jsonplaceholder.typicode.com/posts");
    private static HttpRequest RequestUsers = new HttpRequest("users", "https://jsonplaceholder.typicode.com/users");
    private static HttpRequest RequestComments = new HttpRequest("comments", "https://jsonplaceholder.typicode.com/comments");

    public static void Main(string[] args)
    {
        Bootstrap().Wait();
        Console.Read();
    }

    private static async Task Bootstrap()
    {
        CycleNet.Run(Flow, new[]
        {
            LogDriver.Create,
            HttpDriver.Create(),
            await TcpDriver.Create(5000)
        });
    }

    public static IObservable<ILogRequest> LogOnlineClient(IObservable<int> onlineClientStream) =>
        onlineClientStream
            .Select(counter => new LogRequest($"there are {counter} clients online."));

    public static IObservable<int> OnlineClientCounter(IObservable<ITcpResponse> tcpStream) =>
        tcpStream
            .Scan(Helper.Identity<int>(), (fun, response) =>
                {
                    switch (response)
                    {
                        case ClientConnected connected:
                            return x => fun(x) + 1;
                        case ClientDisconnected disconnected:
                            return x => fun(x) - 1;
                        default:
                            return fun;
                    }
                }
            )
            .Select(fun => fun(0))
            .DistinctUntilChanged();

    public static IObservable<IGroupedObservable<string, TcpClientState>> TcpClientStatesStream(IObservable<ITcpResponse> tcpStream) =>
        tcpStream
            .GroupBy(response => response.ClientId)
            .SelectMany(clientStream => clientStream
                    .TakeWhile(response => !(response is ClientDisconnected))
                    .Scan(Helper.Identity<TcpClientState>(), TcpClientState.Reducers)
                    .Select(fun => fun(new TcpClientState(clientStream.Key))))
            .GroupBy(state => state.Id);

    public static IObservable<ITcpRequest> EchoBytesReceivedStream(IObservable<IGroupedObservable<string, TcpClientState>> clientStatesStream) =>
        clientStatesStream
            .SelectMany(stateStream => stateStream
                                            .Skip(1) // ignore initial state
                                            .Select(state => new ClientDataSend(
                                                state.Id,
                                                Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes($"bytes received: {state.BytesReceived.ToString()}"))))
                                            .Take(1) // echo once and kick
                                            .Concat<ITcpRequest>(
                                                Observable.Return(new ClientKick(stateStream.Key))));

    public static IObservable<ILogRequest> LogHttpStream(IObservable<IHttpResponse> httpStream) =>
        httpStream
            .Select(response => new LogRequest($"http response: id={response.Origin.Id}"));


    public static IObservable<IRequest> Flow(IObservable<IResponse> source)
    {
        var httpStream = source.OfType<IHttpResponse>();
        var tcpStream = source.OfType<ITcpResponse>();
        var tcpClientsStream = TcpClientStatesStream(tcpStream);
        var onlineCountStream = OnlineClientCounter(tcpStream);

        var tcpSink = EchoBytesReceivedStream(tcpClientsStream);
        var httpSink = new[]
        {
            RequestUsers,
            RequestPosts,
            RequestComments
        }.ToObservable();

        var logOnlineCount = LogOnlineClient(onlineCountStream);
        var logHttpSink = LogHttpStream(httpStream);
        var logSink = Observable.Merge(logHttpSink, logOnlineCount);

        return Observable.Merge<IRequest>(tcpSink, httpSink, logSink);
    }
}
```
