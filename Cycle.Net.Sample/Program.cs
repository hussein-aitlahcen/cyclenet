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
}
