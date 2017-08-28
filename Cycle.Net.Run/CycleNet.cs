using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Collections.Generic;
using Cycle.Net.Core.Abstract;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using Cycle.Net.Core;

namespace Cycle.Net.Run
{
    using Driver = IObservable<IResponse>;
    using DriverMaker = Func<IObservable<IRequest>, IObservable<IResponse>>;
    using Drivers = IEnumerable<Func<IObservable<IRequest>, IObservable<IResponse>>>;

    public class CycleNet : AbstractObservable<IRequest>, IObserver<IRequest>, IObservable<IResponse>, IObserver<IResponse>
    {
        public static IScheduler Scheduler = new EventLoopScheduler();

        private readonly List<IObserver<IResponse>> m_responseObservers;

        public CycleNet()
        {
            m_responseObservers = new List<IObserver<IResponse>>();
        }

        public void Run(Func<IObservable<IResponse>, IObservable<IRequest>> main, Drivers drivers)
        {
            var sink = ((IObservable<IRequest>)this)
                .SubscribeOn(Scheduler)
                .ObserveOn(Scheduler);
            var source = ((IObservable<IResponse>)this)
                .SubscribeOn(Scheduler)
                .ObserveOn(Scheduler);
            foreach (var driver in drivers)
            {
                driver(sink)
                    .SubscribeOn(Scheduler)
                    .ObserveOn(Scheduler)
                    .Where(response => !(response is EmptyResponse))
                    .Subscribe(this);
            }
            main(source).Subscribe(this);
        }

        public void OnCompleted() =>
            NotifyCompleted();

        public void OnError(Exception error) =>
            NotifyError(error);

        public void OnNext(IRequest value)
        {
#if DEBUG
            Console.WriteLine($"[{value.GetType().Name}]");
#endif
            NotifyNext(value);
        }

        public void OnNext(IResponse value)
        {
#if DEBUG
            Console.WriteLine($"[{value.GetType().Name}]");
#endif
            Notify(observer => observer.OnNext(value));
        }

        private void Notify(Action<IObserver<IResponse>> fun) =>
            m_responseObservers.ForEach(fun);

        public IDisposable Subscribe(IObserver<IResponse> observer)
        {
            m_responseObservers.Add(observer);
            return Disposable.Create(() => m_responseObservers.Remove(observer));
        }
    }
}