using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Cycle.Net.Run;
using Cycle.Net.Run.Abstract;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Cycle.Net.Sample.Driver
{
    public interface IDotNettyResponse
    {
        IChannel Client { get; }
    }

    public interface IDotNettyRequest
    {
        IChannel Client { get; }
    }

    public sealed class ClientConnected : IResponse, IDotNettyResponse
    {
        public IChannel Client { get; }
        public ClientConnected(IChannel client) => Client = client;
    }

    public sealed class ClientDisconnected : IResponse, IDotNettyResponse
    {
        public IChannel Client { get; }
        public ClientDisconnected(IChannel client) => Client = client;
    }

    public sealed class ClientDataReceived : IResponse, IDotNettyResponse
    {
        public IChannel Client { get; }
        public IByteBuffer Buffer { get; }
        public ClientDataReceived(IChannel client, IByteBuffer buffer)
        {
            Client = client;
            Buffer = buffer;
        }
    }

    public sealed class ClientDataSend : IRequest, IDotNettyRequest
    {

        public IChannel Client { get; }
        public IByteBuffer Buffer { get; }
        public ClientDataSend(IChannel client, IByteBuffer buffer)
        {
            Client = client;
            Buffer = buffer;
        }
    }

    public sealed class ClientDisconnect : IRequest, IDotNettyRequest
    {
        public IChannel Client { get; }
        public ClientDisconnect(IChannel client) => Client = client;
    }

    public sealed class DotNettyDriver
    {
        public const string ID = "dotnetty-driver";

        public static void BasicPipelineConfigurator(IChannelPipeline pipeline)
        {
            pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
            pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
        }

        public static async Task<Func<IObservable<IRequest>, IObservable<IResponse>>> Create(
            IScheduler scheduler,
            Action<IChannelPipeline> pipelineConfigurator,
            int port)
        {
            var handler = new SocketHandler();
            var bossGroup = new MultithreadEventLoopGroup(1);
            var workerGroup = new MultithreadEventLoopGroup();
            var bootstrap = new ServerBootstrap();
            bootstrap
                .Group(bossGroup, workerGroup)
                .Channel<TcpServerSocketChannel>()
                .Option(ChannelOption.SoBacklog, 100)
                .ChildHandler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipelineConfigurator(pipeline);
                    pipeline.AddLast("handler", handler);
                }));
            var boundChannel = await bootstrap.BindAsync(port);
            return (requests) =>
            {
                requests.Subscribe(handler);
                return handler;
            };
        }

        private class SocketHandler : ChannelHandlerAdapter, IObservable<IResponse>, IObserver<IRequest>
        {
            private readonly List<IObserver<IResponse>> m_observers;

            public SocketHandler() =>
                m_observers = new List<IObserver<IResponse>>();

            public override void ChannelActive(IChannelHandlerContext context) =>
                Notify(new ClientConnected(context.Channel));

            public override void ChannelInactive(IChannelHandlerContext context) =>
                Notify(new ClientDisconnected(context.Channel));

            public override void ChannelRead(IChannelHandlerContext context, object message)
            {
                var buffer = message as IByteBuffer;
                if (buffer != null)
                {
                    Notify(new ClientDataReceived(context.Channel, buffer));
                }
            }

            private void Notify(IResponse response) =>
                m_observers.ForEach
                (
                    observer => observer.OnNext(response)
                );

            public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) =>
                context.CloseAsync();


            public IDisposable Subscribe(IObserver<IResponse> observer)
            {
                m_observers.Add(observer);
                return Disposable.Create(() => m_observers.Remove(observer));
            }

            public void OnCompleted()
            {
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(IRequest value)
            {
                switch (value)
                {
                    // TODO: handler write request
                }
            }

            public override bool IsSharable => true;
        }
    }
}