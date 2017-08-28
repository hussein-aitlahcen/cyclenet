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
    using Drivers = Dictionary<string, Func<IObservable<IRequest>, IObservable<IResponse>>>;

    public class CycleNet : AbstractObservable<IRequest>, IObserver<IRequest>
    {
        public void Run<TSource>(Func<TSource, IObservable<IRequest>> main, IScheduler scheduler, Drivers drivers)
            where TSource : ISource, new()
        {
            var source = new TSource();
            var ignore = Observable.Empty<IRequest>();
            foreach (var driver in drivers)
            {
                var stream = driver.Value(this);
                ignore = ignore.Merge(stream.OfType<EmptyResponse>().SelectMany(response => Observable.Empty<IRequest>()));
                source.AddDriver(driver.Key, stream.ObserveOn(scheduler));
            }
            main(source).Merge(ignore).Subscribe(this);
        }

        public void OnCompleted() =>
            NotifyCompleted();

        public void OnError(Exception error) =>
            NotifyError(error);

        public void OnNext(IRequest value) =>
            NotifyNext(value);
    }
}