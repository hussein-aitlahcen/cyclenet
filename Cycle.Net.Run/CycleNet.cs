using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Collections.Generic;
using Cycle.Net.Run.Abstract;

namespace Cycle.Net.Run
{
    public class CycleNet<TSource> : AbstractObservable<IRequest>, IObserver<IRequest>
        where TSource : ISource
    {
        private TSource m_source;

        public CycleNet(TSource source)
        {
            m_source = source;
        }

        public void Run(Func<TSource, IObservable<IRequest>> main)
        {
            foreach (var driver in m_source.GetDrivers())
            {
                Subscribe(driver);
            }
            var sink = main(m_source);
            var connectableSink = sink.Publish();
            connectableSink.Subscribe(this);
            connectableSink.Connect();
        }

        public void OnCompleted() =>
            NotifyCompleted();

        public void OnError(Exception error) =>
            NotifyError(error);

        public void OnNext(IRequest value) =>
            NotifyNext(value);
    }
}