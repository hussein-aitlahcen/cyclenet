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

public sealed class AppState : AbstractReducableState<AppState>
{
    public ImmutableList<string> Clients { get; }
    public AppState() : this(ImmutableList.Create<string>()) { }
    public AppState(ImmutableList<string> clients) => Clients = clients;
    public override AppState Reduce(IResponse response)
    {
        switch (response)
        {
            case ClientConnected connected:
                return new AppState(Clients.Add(connected.ClientId));
            case ClientDisconnected disconnected:
                return new AppState(Clients.Remove(disconnected.ClientId));
            default:
                return this;
        }
    }
}

public sealed class TcpClientState : AbstractReducableState<TcpClientState>
{
    public string Id { get; }
    public int BytesReceived { get; }
    public int MessagesReceived { get; }
    public TcpClientState(string id) : this(id, 0, 0)
    {
    }

    public TcpClientState(string id, int bytesReceived, int messagesReceived)
    {
        Id = id;
        BytesReceived = bytesReceived;
        MessagesReceived = messagesReceived;
    }

    public override TcpClientState Reduce(IResponse response)
    {
        switch (response)
        {
            case ClientDataReceived received:
                return new TcpClientState(Id, BytesReceived + received.Buffer.ReadableBytes, MessagesReceived + 1);
            default:
                return this;
        }
    }
}

public sealed class TcpClientCommand
{
    public TcpClientState Client { get; }
    public string Text { get; }
    public TcpClientCommand(TcpClientState client, string text)
    {
        Client = client;
        Text = text;
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
        CycleNet.Run(Flow, new[]
        {
            LogDriver.Create,
            HttpDriver.Create(),
            await TcpDriver.Create(5000)
        });
    }

    public static IObservable<AppState> AppStateStream(IObservable<ITcpResponse> tcpStream)
    =>
        tcpStream.ToState(new AppState());

    public static IObservable<IGroupedObservable<string, TcpClientState>> TcpClientStatesStream(IObservable<ITcpResponse> tcpStream)
    =>
        tcpStream
            .GroupBy(response => response.ClientId)
            .SelectMany(clientStream => clientStream
                    .TakeWhile(response => !(response is ClientDisconnected))
                    .ToState(new TcpClientState(clientStream.Key)))
            .GroupBy(state => state.Id);

    public static IObservable<ITcpRequest> HandleClientCommand(
        IObservable<TcpClientCommand> clientCommandsStream,
        string commandText,
        Func<TcpClientCommand, ITcpRequest> selector)
    =>
        clientCommandsStream
            .Where(command => command.Text.StartsWith(commandText))
            .Select(selector);

    public static Func<TcpClientCommand, ITcpRequest> CmdSend(Func<TcpClientCommand, string> transform)
    =>
        command =>
            new ClientDataSend(
                command.Client.Id,
                Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes(transform(command) + "\n")));

    public static Func<TcpClientCommand, ITcpRequest> CmdKick()
    =>
        command =>
            new ClientKick(command.Client.Id);

    public static IObservable<IRequest> Flow(IObservable<IResponse> source)
    {
        var httpStream = source.OfType<IHttpResponse>();
        var tcpStream = source.OfType<ITcpResponse>();
        var clientsStateStream = TcpClientStatesStream(tcpStream);

        var clientsCommandsStream = clientsStateStream
            .Select(clientStateStream =>
                        tcpStream
                            .OfType<ClientDataReceived>()
                            .Where(response => response.ClientId == clientStateStream.Key)
                            .Select(response => response.Buffer)
                            .WithLatestFrom(
                                clientStateStream,
                                (buffer, state) =>
                                    new TcpClientCommand(
                                        state,
                                        ByteBufferUtil
                                            .DecodeString(
                                                buffer,
                                                0,
                                                buffer.ReadableBytes,
                                                Encoding.UTF8)
                                    )
                            )
                    );

        var tcpSink = clientsCommandsStream
            .SelectMany(
                commandsStream =>
                    Observable.Merge(
                        HandleClientCommand(
                            commandsStream,
                            "bytes",
                            CmdSend(command =>
                                $"total bytes received: {command.Client.BytesReceived}")),
                        HandleClientCommand(
                            commandsStream,
                            "messages",
                            CmdSend(command =>
                                $"nb of msg received: {command.Client.MessagesReceived}")
                        ),
                        HandleClientCommand(
                            commandsStream,
                            "bye",
                            CmdKick()
                        )
                    )
            );

        return tcpSink;
    }
}
```
