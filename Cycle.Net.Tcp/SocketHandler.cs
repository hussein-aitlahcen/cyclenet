
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Cycle.Net.Core.Abstract;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;

namespace Cycle.Net.Tcp
{
    public sealed class SocketHandler : ChannelHandlerAdapter, IObservable<ITcpResponse>, IObserver<ITcpRequest>
    {
        private class ChannelMatcher : IChannelMatcher
        {
            private readonly Predicate<IChannel> m_predicate;

            public ChannelMatcher(Predicate<IChannel> predicate)
            {
                m_predicate = predicate;
            }

            public bool Matches(IChannel channel) => m_predicate(channel);
        }

        private readonly List<IObserver<ITcpResponse>> m_observers;

        private volatile IChannelGroup m_group;

        public SocketHandler() =>
            m_observers = new List<IObserver<ITcpResponse>>();

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
            Notify(new ClientConnected(context.Channel.Id.AsShortText()));
        }

        public override void ChannelInactive(IChannelHandlerContext context) =>
            Notify(new ClientDisconnected(context.Channel.Id.AsShortText()));

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var buffer = message as IByteBuffer;
            if (buffer != null)
            {
                Notify(new ClientDataReceived(context.Channel.Id.AsShortText(), buffer.Copy()));
            }
        }

        private void Notify(ITcpResponse response) =>
            m_observers.ForEach
            (
                observer => observer.OnNext(response)
            );

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) =>
            context.CloseAsync();


        public IDisposable Subscribe(IObserver<ITcpResponse> observer)
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

        public void OnNext(ITcpRequest value)
        {
            switch (value)
            {
                case ClientDataSend send:
                    m_group.WriteAndFlushAsync(
                        Unpooled.WrappedBuffer(send.Buffer),
                        new ChannelMatcher(channel => channel.Id.AsShortText() == send.ClientId));
                    break;
            }
        }

        public override bool IsSharable => true;
    }
}