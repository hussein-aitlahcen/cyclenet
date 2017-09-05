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

This example accept connections on **localhost:5000** and allow you to send commands like:

* **bytes** : returns total bytes received
* **messages** : returns nb of messages received
* **bye** : kick yourself

```csharp
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

public sealed class CommandHandler
{
    public string Text { get; }
    public Func<TcpClientCommand, ITcpRequest> Selector { get; }
    public CommandHandler(string text, Func<TcpClientCommand, ITcpRequest> selector)
    {
        Text = text;
        Selector = selector;
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

    public static IObservable<IGroupedObservable<string, TcpClientState>> ClientsStateStream(
        IObservable<ITcpResponse> tcpStream)
    =>
        tcpStream
            .GroupBy(response => response.ClientId)
            .SelectMany(clientStream => clientStream
                    .TakeWhile(response => !(response is ClientDisconnected))
                    .ToState(new TcpClientState(clientStream.Key)))
            .GroupBy(state => state.Id);

    public static IObservable<IObservable<TcpClientCommand>> ClientsCommandsStream(
        IObservable<ITcpResponse> tcpStream,
        IObservable<IGroupedObservable<string, TcpClientState>> tcpClientsStateStream)
    =>
        tcpClientsStateStream
            .Select(clientStateStream =>
                            tcpStream
                                .OfType<ClientDataReceived>()
                                .Where(response => response.ClientId == clientStateStream.Key)
                                .Select(response => response.Buffer)
                                .Select(buffer => ByteBufferUtil
                                                    .DecodeString(
                                                        buffer,
                                                        0,
                                                        buffer.ReadableBytes,
                                                        Encoding.UTF8))
                                .WithLatestFrom(
                                    clientStateStream,
                                    (text, client) =>
                                        new TcpClientCommand(
                                            client,
                                            text
                                        )
                                )
                        );

    public static IObservable<ITcpRequest> HandleClientCommands(
        IObservable<TcpClientCommand> clientCommandsStream,
        IObservable<CommandHandler> commandHandlers)
    =>
        commandHandlers
            .SelectMany(handler => clientCommandsStream
                                    .Where(command => command.Text.StartsWith(handler.Text))
                                    .Select(handler.Selector));

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
        var tcpStream = source.OfType<ITcpResponse>();

        var clientsStateStream = ClientsStateStream(tcpStream);

        var clientsCommandsStream = ClientsCommandsStream(tcpStream, clientsStateStream);

        var handlersStream = new[]
        {
            new CommandHandler("bytes", CmdSend(command => $"total bytes received: {command.Client.BytesReceived}")),
            new CommandHandler("messages", CmdSend(command => $"nb of msg received: {command.Client.MessagesReceived}")),
            new CommandHandler("bye", CmdKick())
        }.ToObservable();

        var tcpSink = HandleClientCommands(clientsCommandsStream.Merge(), handlersStream);

        return tcpSink;
    }
}
```
