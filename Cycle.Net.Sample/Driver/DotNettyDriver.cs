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
using DotNetty.Transport.Channels.Groups;
using DotNetty.Transport.Channels.Sockets;

namespace Cycle.Net.Sample.Driver
{
    public interface IDotNettyResponse : IResponse
    {
        IChannelId ClientId { get; }
    }

    public interface IDotNettyRequest : IRequest
    {
        IChannelId ClientId { get; }
    }

    public sealed class ClientConnected : IDotNettyResponse
    {
        public IChannelId ClientId { get; }
        public ClientConnected(IChannelId clientId) => ClientId = clientId;
    }

    public sealed class ClientDisconnected : IDotNettyResponse
    {
        public IChannelId ClientId { get; }
        public ClientDisconnected(IChannelId clientId) => ClientId = clientId;
    }

    public sealed class ClientDataReceived : IDotNettyResponse
    {
        public IChannelId ClientId { get; }
        public IByteBuffer Buffer { get; }
        public ClientDataReceived(IChannelId clientId, IByteBuffer buffer)
        {
            ClientId = clientId;
            Buffer = buffer;
        }
    }

    public sealed class ClientDataSend : IDotNettyRequest
    {

        public IChannelId ClientId { get; }
        public IByteBuffer Buffer { get; }
        public ClientDataSend(IChannelId clientId, IByteBuffer buffer)
        {
            ClientId = clientId;
            Buffer = buffer;
        }
    }

    public sealed class ClientDisconnect : IDotNettyRequest
    {
        public IChannelId ClientId { get; }
        public ClientDisconnect(IChannelId clientId) => ClientId = clientId;
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

            private volatile IChannelGroup m_group;

            public SocketHandler() =>
                m_observers = new List<IObserver<IResponse>>();

            public override void ChannelActive(IChannelHandlerContext context)
            {
                IChannelGroup g = m_group;
                if (g == null)
                {
                    lock (this)
                    {
                        if (m_group == null)
                        {
                            g = m_group = new DefaultChannelGroup(context.Executor);
                        }
                    }
                }
                m_group.Add(context.Channel);
                Notify(new ClientConnected(context.Channel.Id));
            }

            public override void ChannelInactive(IChannelHandlerContext context) =>
                Notify(new ClientDisconnected(context.Channel.Id));

            public override void ChannelRead(IChannelHandlerContext context, object message)
            {
                var buffer = message as IByteBuffer;
                if (buffer != null)
                {
                    Notify(new ClientDataReceived(context.Channel.Id, buffer.Copy()));
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
                    case ClientDataSend send:
                        m_group.WriteAndFlushAsync(
                            send.Buffer,
                            new ChannelMatcher(channel => channel.Id == send.ClientId));
                        break;
                }
            }

            public override bool IsSharable => true;
        }
        private class ChannelMatcher : IChannelMatcher
        {
            private readonly Predicate<IChannel> m_predicate;

            public ChannelMatcher(Predicate<IChannel> predicate)
            {
                m_predicate = predicate;
            }

            public bool Matches(IChannel channel) => m_predicate(channel);
        }
    }
}