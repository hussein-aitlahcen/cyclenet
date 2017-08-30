using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Cycle.Net.Core;
using Cycle.Net.Core.Abstract;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using DotNetty.Transport.Channels.Sockets;

namespace Cycle.Net.Tcp
{
    public sealed class TcpDriver
    {
        public const string ID = "tcp-driver";


        public static void EmptyPipelineConfigurator(IChannelPipeline pipeline) { }

        public static void FramingPipelineConfigurator(IChannelPipeline pipeline)
        {
            pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
            pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));
        }
        public static async Task<Func<IObservable<IRequest>, IObservable<IResponse>>> Create(int port) =>
            await Create(EmptyPipelineConfigurator, port);

        public static async Task<Func<IObservable<IRequest>, IObservable<IResponse>>> Create(
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
                requests
                    .OfType<ITcpRequest>()
                    .Subscribe(handler);
                return handler;
            };
        }
    }
}