using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Cycle.Net.Run.Abstract
{
    public abstract class AbstractObservable<T> : IObservable<T>
    {
        private List<IObserver<T>> m_observers;

        public AbstractObservable()
        {
            m_observers = new List<IObserver<T>>();
        }

        public void Dispatch(T value) =>
            m_observers.ForEach(observer => observer.OnNext(value));

        public IDisposable Subscribe(IObserver<T> observer)
        {
            m_observers.Add(observer);
            return Disposable.Create(() => m_observers.Remove(observer));
        }
    }
}