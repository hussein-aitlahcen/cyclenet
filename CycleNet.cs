using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Collections.Generic;
using Cycle.Net.Driver;

namespace Cycle.Net
{
    public class CycleNet<TSource, TSink> : IObserver<IEnvelope>, IObservable<IEnvelope>
        where TSource : ISource
        where TSink : ISink
    {
        private TSource m_source;

        private List<IObserver<IEnvelope>> m_observers;

        public CycleNet(TSource source)
        {
            m_source = source;
            m_observers = new List<IObserver<IEnvelope>>();
        }

        public void Run(Func<TSource, TSink> mainFunction)
        {
            foreach (var driver in m_source.GetDrivers())
                this.Where(envelope => envelope.DriverId == driver.Id)
                    .Select(envelope => envelope.Request)
                    .Subscribe(driver);
            mainFunction(m_source).Subscribe(this);
        }

        public void OnCompleted() =>
            m_observers.ForEach(observer => observer.OnCompleted());

        public void OnError(Exception error) =>
            m_observers.ForEach(observer => observer.OnError(error));

        public void OnNext(IEnvelope value) =>
            m_observers.ForEach(observer => observer.OnNext(value));

        public IDisposable Subscribe(IObserver<IEnvelope> observer)
        {
            m_observers.Add(observer);
            return Disposable.Create(() => m_observers.Remove(observer));
        }
    }
}