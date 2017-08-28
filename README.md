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

This example proxies the json content fetched from an api as raw data to clients connecting on localhost:5000
```csharp
using Driver = IObservable<IResponse>;
using DriverMaker = Func<IObservable<IRequest>, IObservable<IResponse>>;
using Drivers = Dictionary<string, Func<IObservable<IRequest>, IObservable<IResponse>>>;

public sealed class TcpState
{
    public static TcpState Initial = new TcpState(ImmutableHashSet.Create<string>());
    public ImmutableHashSet<string> Clients { get; }
    public TcpState(ImmutableHashSet<string> clients) => Clients = clients;

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

public class Program
{
    public static void Main(string[] args)
    {
        Bootstrap().Wait();
        Console.Read();
    }

    private static async Task Bootstrap()
    {
        new CycleNet().Run(Flow, new[]
        {
            LogDriver.Create,
            HttpDriver.Create(),
            await TcpDriver.Create(pipe => { }, 5000)
        });
    }

    static HttpRequest RequestUsers = new HttpRequest("users", "https://jsonplaceholder.typicode.com/users");

    public static IObservable<TcpState> TcpStateStream(IObservable<ITcpResponse> source) =>
        source
            .Scan(TcpState.Initial, TcpState.Reducer)
            .DistinctUntilChanged();

    public static IObservable<ILogRequest> LogHttpStream(IObservable<IHttpResponse> httpStream) =>
        httpStream
            .Select(response => new LogRequest($"response received: {response.Origin.Id}"));

    public static IObservable<ILogRequest> LogStateSink(IObservable<TcpState> tcpStateStream) =>
        tcpStateStream
            .Select(state => new LogRequest($"nb of clients: {state.Clients.Count}"));

    public static IObservable<ILogRequest> LogTcpSink(IObservable<ITcpResponse> tcpStream) =>
        tcpStream
            .OfType<ClientDataReceived>()
            .Select(data => new LogRequest($"client data recv: {data.Buffer.ToString()}"));

    public static IObservable<IHttpRequest> HttpSink(IObservable<ITcpResponse> tcpStream) =>
        tcpStream
            .OfType<ClientConnected>()
            .Select(connected => new HttpRequest(connected.ClientId, "https://jsonplaceholder.typicode.com/users"));

    public static IObservable<ITcpRequest> TcpSink(IObservable<IHttpResponse> httpStream) =>
        httpStream
            .OfType<HttpResponse>()
            .SelectMany(response => response.Message.Content.ReadAsByteArrayAsync()
                                        .ToObservable()
                                        .Select(content => new ClientDataSend(response.Origin.Id, content))
                                        .Do(request => response.Dispose()));

    public static IObservable<IRequest> Flow(IObservable<IResponse> source)
    {
        var httpStream = source.OfType<IHttpResponse>();
        var tcpStream = source.OfType<ITcpResponse>();
        var tcpStateStream = TcpStateStream(tcpStream);
        var tcpSink = TcpSink(httpStream);
        var httpSink = HttpSink(tcpStream);
        var logSink = Observable.Merge(LogStateSink(tcpStateStream), LogTcpSink(tcpStream), LogHttpStream(httpStream));
        return Observable.Merge<IRequest>(httpSink, tcpSink, logSink);
    }
}
```
