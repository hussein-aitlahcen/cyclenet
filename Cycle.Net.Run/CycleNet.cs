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

        private List<IObserver<IRequest>> m_observers;

        private Queue<IRequest> m_buffer;

        private bool m_start;

        public CycleNet(TSource source)
        {
            m_source = source;
            m_start = true;
            m_buffer = new Queue<IRequest>();
            m_observers = new List<IObserver<IRequest>>();
        }

        public void Run(Func<TSource, IObservable<IRequest>> mainFunction)
        {
            foreach (var driver in m_source.GetDrivers())
            {
                Subscribe(driver);
            }
            mainFunction(m_source).Subscribe(this);
            m_start = false;
            while (m_buffer.Count > 0)
            {
                OnNext(m_buffer.Dequeue());
            }
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(IRequest value)
        {
            if (m_start)
            {
                m_buffer.Enqueue(value);
            }
            else
            {
                Dispatch(value);
            }
        }
    }
}