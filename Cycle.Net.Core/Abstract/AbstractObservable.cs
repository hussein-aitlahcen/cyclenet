using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Cycle.Net.Core.Abstract
{
    public abstract class AbstractObservable<T> : IObservable<T>
    {
        protected List<IObserver<T>> m_observers;

        public AbstractObservable()
        {
            m_observers = new List<IObserver<T>>();
        }

        public void NotifyCompleted() =>
            Notify(observer => observer.OnCompleted());

        public void NotifyError(Exception error) =>
            Notify(observer => observer.OnError(error));

        public void NotifyNext(T value) =>
            Notify(observer => observer.OnNext(value));

        public void Notify(Action<IObserver<T>> fun) =>
            m_observers.ForEach(fun);

        public virtual IDisposable Subscribe(IObserver<T> observer)
        {
            m_observers.Add(observer);
            return Disposable.Create(() => m_observers.Remove(observer));
        }
    }
}