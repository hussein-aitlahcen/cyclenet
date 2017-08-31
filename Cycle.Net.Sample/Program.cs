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
using System.Reactive.Subjects;
using System.Reactive;
using System.Text;

namespace Cycle.Net.Sample
{
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

        public static TcpClientState Reducer(TcpClientState previous, ITcpResponse message)
        {
            switch (message)
            {
                case ClientDataReceived received:
                    return new TcpClientState(previous.Id, previous.BytesReceived + received.Buffer.ReadableBytes);
                default:
                    return previous;
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
                .Scan(new Func<int, int>(x => x), (fun, response) =>
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
                                                .Scan(new TcpClientState(clientStream.Key), TcpClientState.Reducer))
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
}
