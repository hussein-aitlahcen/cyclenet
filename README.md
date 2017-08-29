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

    public static TcpClientState Reducer(TcpClientState previous, int bytesReceived) =>
        new TcpClientState(previous.Id, previous.BytesReceived + bytesReceived);
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
            await TcpDriver.Create(pipe => { }, 5000)
        });
    }

    public static IObservable<IObservable<TcpClientState>> TcpClientStatesStream(IObservable<ITcpResponse> tcpStream) =>
        tcpStream
            .OfType<ClientConnected>()
            .Select(connected =>
            {
                var eventStream = tcpStream
                                    .Where(response => response.ClientId == connected.ClientId)
                                    .TakeWhile(response =>
                                    {
                                        var clientDisconnected = response as ClientDisconnected;
                                        if (clientDisconnected != null)
                                        {
                                            return clientDisconnected.ClientId != connected.ClientId;
                                        }
                                        return true;
                                    });
                var stateStream = eventStream
                            .OfType<ClientDataReceived>()
                            .Select(data => data.Buffer.ReadableBytes)
                            .Scan(new TcpClientState(connected.ClientId), TcpClientState.Reducer);
                return stateStream;
            });

    public static IObservable<ITcpRequest> EchoBytesReceivedStream(IObservable<IObservable<TcpClientState>> clientStatesStream) =>
        clientStatesStream
            .SelectMany(stateStream => stateStream
                                            .Select(state => new ClientDataSend(state.Id, Encoding.UTF8.GetBytes($"bytes received: {state.BytesReceived.ToString()}"))));

    public static IObservable<ILogRequest> LogTcpClientStateStream(IObservable<IObservable<TcpClientState>> clientStatesStream) =>
        clientStatesStream
            .SelectMany(stateStream => stateStream
                                            .Select(state => new LogRequest($"tcp client: id={state.Id}, bytesReceived={state.BytesReceived}")));

    public static IObservable<ILogRequest> LogHttpStream(IObservable<IHttpResponse> httpStream) =>
        httpStream
            .Select(response => new LogRequest($"http response: id={response.Origin.Id}"));

    public static IObservable<ILogRequest> LogTcpSink(IObservable<ITcpResponse> tcpStream) =>
        tcpStream
            .OfType<ClientDataReceived>()
            .Select(data => new LogRequest($"tcp client: data={data.Buffer.ToString()}"));

    public static IObservable<IRequest> Flow(IObservable<IResponse> source)
    {
        var httpStream = source.OfType<IHttpResponse>();
        var tcpStream = source.OfType<ITcpResponse>();
        var tcpClientsStream = TcpClientStatesStream(tcpStream);

        var tcpSink = EchoBytesReceivedStream(tcpClientsStream);
        var httpSink = new[]
        {
            RequestUsers,
            RequestPosts,
            RequestComments
        }.ToObservable();

        var logTcpSink = LogTcpSink(tcpStream);
        var logHttpSink = LogHttpStream(httpStream);
        var logTcpClientSink = LogTcpClientStateStream(tcpClientsStream);
        var logSink = Observable.Merge(logTcpSink, logHttpSink, logTcpClientSink);

        return Observable.Merge<IRequest>(tcpSink, httpSink, logSink);
    }
}
```
