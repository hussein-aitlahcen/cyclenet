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
using System.Threading.Tasks;
using System.Linq;
using DotNetty.Buffers;
using System.Reactive.Threading.Tasks;

namespace Cycle.Net.Sample
{
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
}
